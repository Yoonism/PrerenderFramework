#pragma once
#include "../Headers/HMain.hlsl"
#include "../Headers/HMath.hlsl"
#include "VoxelizationCommon.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingSampling.hlsl"

#define _VoxelResInverse (1.0f / _VoxelResolution.xyz)
#define _VoxelResInverseHalf ((1.0f / _VoxelResolution.xyz) * 0.5f)

float _SkyOcclusionCone;

// ------------------------ FUNCTIONS ------------------------

// Offsets tracing origin from a GBuffer surface to avoid voxel self-intersection
void VoxelBias(int2 pixCoord, H_TEXTURE(_DepthPyramid), StructuredBuffer<float2> _PointDistribution, float Depth, float HitDistanceWS, float3 GeometryNormalWS, float3 RayDirectionWS, inout float3 RayOriginWS)
{   
    float JitterNoise = GetBNDSequenceSample(pixCoord.xy, _FrameCount, 0);
    float DotWeight = saturate(dot(GeometryNormalWS, RayDirectionWS));

    // bool IsRayOriginMoving = (GetStencilValue(H_LOAD(_HTraceStencilBuffer, (pixCoord.xy) ) ) & 32) != 0;
    // if (IsRayOriginMoving)
    // {
    //     float MotionBias = min(0.5f / LinearEyeDepth(Depth, _ZBufferParams), 0.5f);
    //     RayOriginWS += RayDirectionWS * MotionBias; 
    // }

    // Apply positive direction bias
    if (HitDistanceWS <= _VoxelSize * 0.5f) 
    {
        float3 DirectionBias = normalize(RayDirectionWS) * _VoxelSize;
        RayOriginWS += DirectionBias * lerp(0.0f, 0.5f, DotWeight); 
    }

    // Jitter ray to hide voxel artifacts
    if (true) 
    {
        float3x3 OrthoBasis = GetLocalFrame(GeometryNormalWS.xyz);
        
        for (int i = 0; i < 4; i++)
        {
            float2 Sample = _PointDistribution[JitterNoise.x * 64 - i] * 0.1f;
            float3 SamplePosWS = RayOriginWS + OrthoBasis[0] * Sample.x + OrthoBasis[1] * Sample.y;
            float4 SamplePosHC = TransformWorldToHClip(SamplePosWS);
            SamplePosHC.xyz /= SamplePosHC.w;
            float2 SamplePosSS = SamplePosHC.xy * 0.5f + 0.5f;
            SamplePosSS.y = 1.0f - SamplePosSS.y;
            
            if (SamplePosHC.x > 1.0f || SamplePosHC.x < -1.0f || SamplePosHC.y > 1.0f || SamplePosHC.y < -1.0f)
                continue;
            
            float BiasedSurface = H_LOAD(_DepthPyramid, SamplePosSS * _ScreenSize.xy).x;
            
            if (SamplePosHC.z > BiasedSurface)
            {
                RayOriginWS = SamplePosWS; 
                break;
            }
        }   
    }

    // Apply normal bias
    if (HitDistanceWS <= _VoxelSize * 3)
    {
        RayOriginWS += GeometryNormalWS * _VoxelSize * (1.0f - DotWeight);  
    }
    
    // Apply negative direction bias
    if (HitDistanceWS >= _VoxelSize * 2.0f)
    {
        float MaxNegativeDistance = HitDistanceWS > _VoxelSize * 2.0f ? 1.0f : 0.5f;
        RayOriginWS = RayOriginWS - RayDirectionWS * _VoxelSize * lerp(0.25f, 1.0f, DotWeight);
    }
}

// Calculates the distance of Ray-VoxelBound intersection in a given direction
float MaxVoxelRayDistance(float3 RayOrigin, float3 RayDirection)
{
	float3 BoundMin = _VoxelCameraPos - _VoxelBounds.xzy / 2;
	float3 BoundMax = _VoxelCameraPos + _VoxelBounds.xzy / 2;
	return RayBoxIntersect(RayOrigin, RayDirection, BoundMin, BoundMax);	
}



// ------------------------ STRUCTS ------------------------
struct Step
{
	float3 Direction;
	float3 Position;
	float Distance;
};


struct Ray
{
	float3 Origin;
	float3 DirInv;
	float3 Dir;
};

struct Traversal
{
	float3 DirSignZ;
	float3 DirSign;
	float3 Normal;
	float3 Coord;
	float3 Delta;
	float3 Step;
	float3 Pos;
	float3 Max;
	float Dist; 
};


// ------------------------ TRAVERSAL UPDATES ------------------------
void UpdateStepParameters(inout Traversal Traversal, inout Ray Ray, float Scale)
{
	float TexelSize = _VoxelSize * Scale;
	float3 ResolutionInv = _VoxelResInverse * Scale;
	Traversal.Delta = Ray.DirInv * TexelSize;
	Traversal.Pos = (Ray.Origin + Traversal.Dist * Ray.Dir) / TexelSize;
	Traversal.Coord = floor(Traversal.Pos);
	Traversal.Max = (Ray.DirInv * (Traversal.DirSignZ + Traversal.DirSign * (Traversal.Coord - Traversal.Pos))) * TexelSize + Traversal.Dist;
	Traversal.Coord = (Traversal.Coord + 0.5) * ResolutionInv;
	Traversal.Step = Traversal.DirSign * ResolutionInv;
}

void IterateStep(inout Traversal Traversal, inout Ray Ray)
{
	Traversal.Normal = step(Traversal.Max.xyz, Traversal.Max.zxy) * step(Traversal.Max.xyz, Traversal.Max.yzx);
	Traversal.Dist = dot(Traversal.Max, Traversal.Normal);
	Traversal.Max += Traversal.Delta * Traversal.Normal;
	Traversal.Coord += Traversal.Step * Traversal.Normal;
}


// ------------------------ SUPERCOVER TRAVERSAL + MIPS + OCTANTS ------------------------
bool TraceVoxelsDiffuse(float3 RayOrigin, float3 RayDirection, float TraceDistance, uint MaxIterations, inout VoxelPayload Payload)
{
	float3 OriginWS = RayOrigin;
	float Offset = (_VoxelBounds.y - _VoxelBounds.z) / 2.0f;
	RayOrigin = RayOrigin - float3(0, Offset, -Offset) - _VoxelCameraPos;
	
	Traversal Traversal;
	Ray Ray;

	Ray.Origin = RayOrigin + (_VoxelResolution.xyz / 2.0f) * _VoxelSize;
	Ray.DirInv = 1.0f / (abs(RayDirection) + 0.00001f);
	Ray.Dir = RayDirection;

	Traversal.DirSign = sign(RayDirection);
	Traversal.DirSignZ = step(0.0f, Traversal.DirSign);
	Traversal.Normal = RayDirection;
	Traversal.Coord = 0;
	Traversal.Dist = 0;

	int MipIndex = -1;
	uint Iterations = 0;
	
	while (Traversal.Dist < TraceDistance && Iterations < MaxIterations)
	{
		if (MipIndex == -1)
		{
			UpdateStepParameters(Traversal, Ray, 0.5f);
			Payload.HitDistance = Traversal.Dist;

			int i = 0;
			while (Traversal.Dist < TraceDistance && i++ < 4)
			{
				uint3 VoxelTexCoord = Traversal.Coord * _VoxelResolution.xyz;
				uint VoxelOccupancy = asuint(H_LOAD3D_LOD(_VoxelPositionPyramid, VoxelTexCoord, 0));
				
				if (VoxelOccupancy > 0)
				{	
					uint BitShift = 0u;
					BitShift += fmod(Traversal.Coord.x, _VoxelResInverse.x) > _VoxelResInverseHalf.x ? 1u : 0u;
					BitShift += fmod(Traversal.Coord.y, _VoxelResInverse.y) > _VoxelResInverseHalf.y ? 2u : 0u;
					BitShift += fmod(Traversal.Coord.z, _VoxelResInverse.z) > _VoxelResInverseHalf.z ? 4u : 0u;
					uint BitMask = 1u << BitShift;

					if ((BitMask & VoxelOccupancy) != 0u)
					{
						Payload.HitCoord = VoxelTexCoord;
						Payload.HitDistance = Traversal.Dist;
						Payload.HitNormal = normalize(-Traversal.Normal * Traversal.DirSign);
						Payload.HitPosition = OriginWS + Traversal.Dist * RayDirection;
		
						return true;
					}
				}
				
				IterateStep(Traversal, Ray);
				Iterations++;
			}

			MipIndex = 0;
		}
		else
		{
			float MipIndexScale = float(1 << MipIndex);
			UpdateStepParameters(Traversal, Ray, MipIndexScale);
			Payload.HitDistance = Traversal.Dist;
			
			int i = (MipIndex < 5 ? 8 : 1024);
			while (Traversal.Dist < TraceDistance)
			{
				if (i-- == 0)
				{
					MipIndex++;
					break;
				}

				float3 VoxelTexCoord = Traversal.Coord * _VoxelResolution.xyz / MipIndexScale;
				uint VoxelOccupancy = asuint(H_LOAD3D_LOD(_VoxelPositionPyramid, VoxelTexCoord, MipIndex));

				// if (!IsVoxelCoordInBounds(Traversal.Coord))
				// {
				// 	Payload.HitDistance = Traversal.Dist;
				// 	return false;
				// }
				
				
				if (VoxelOccupancy > 0)
				{
					MipIndex--;
					break;
				}
				
				IterateStep(Traversal, Ray);
				Iterations++;
			}
		}
	}

	return false;
}


// ------------------------ LINEAR TRAVERSAL + MIPS + OCTANTS ------------------------
bool TraceVoxelsOcclusion(float3 RayOrigin, float3 RayDirection, float TraceDistance, uint MaxIterations, inout float Distance)
{
	float Offset = (_VoxelBounds.y - _VoxelBounds.z) / 2.0f;
	RayOrigin = RayOrigin - float3(0, Offset, -Offset) + (_WorldSpaceCameraPos - _VoxelCameraPos);
	RayOrigin = RayOrigin + (_VoxelResolution.xyz / 2.0f) * _VoxelSize;

	Step Step;
	Step.Distance = _VoxelSize;
	Step.Direction = RayDirection * (Step.Distance / _VoxelSize) * _VoxelResInverse;
	Step.Position = (RayOrigin / _VoxelSize) * _VoxelResInverse;

	Step.Direction *= 0.5;
	Step.Distance *= 0.5;

	int MipIndex = -1;
	uint Iterations = 0;
	float3 VoxelResInverseHalf = _VoxelResInverse * 0.5;

	Distance = 0;
	while (Distance < TraceDistance && Iterations < MaxIterations)
	{
		uint MipIndexLocal = max(0, MipIndex);
		float MipIndexScale = float(1 << MipIndexLocal);
		uint3 VoxelTexCoord = Step.Position * _VoxelResolution.xyz;
		uint VoxelOccupancy = asuint(H_LOAD3D_LOD(_VoxelPositionPyramid, VoxelTexCoord / MipIndexScale, MipIndexLocal));
		Iterations++;
		
		if (MipIndex == -1)
		{
			if (VoxelOccupancy != 0u)
			{
				uint BitShift = 0u;
				BitShift += fmod(Step.Position.x, _VoxelResInverse.x) > VoxelResInverseHalf.x ? 1u : 0u;
				BitShift += fmod(Step.Position.y, _VoxelResInverse.y) > VoxelResInverseHalf.y ? 2u : 0u;
				BitShift += fmod(Step.Position.z, _VoxelResInverse.z) > VoxelResInverseHalf.z ? 4u : 0u;
				uint BitMask = 1u << BitShift;
				
				if ((BitMask & VoxelOccupancy) != 0u) return true;
				else
				{
					Step.Position += Step.Direction;
					Distance += Step.Distance;
				}
			}
			else
			{
				MipIndex++;
				Step.Direction *= 2.0;
				Step.Distance *= 2.0;
				Step.Position += Step.Direction;
				Distance += Step.Distance;
			}
		}
		else
		{
			if (VoxelOccupancy != 0u)
			{
				MipIndex--;
				Step.Direction *= 0.5;
				Step.Distance *= 0.5;
				Step.Position -= Step.Direction;
				Distance -= Step.Distance;
			}
			else
			{
				if (MipIndex < 5)
				{
					uint MipIndexLocal = MipIndex + 1;
					float MipIndexScale = float(1 << MipIndexLocal);
					float3 VoxelTexCoord = Step.Position * _VoxelResolution.xyz ;
					uint VoxelOccupancy = asuint(H_LOAD3D_LOD(_VoxelPositionPyramid, VoxelTexCoord / MipIndexScale, MipIndexLocal));
					Iterations++;
					
					if (VoxelOccupancy == 0u)
					{
						MipIndex++;
						Step.Direction *= 2.0;
						Step.Distance *= 2.0;
					}
				}
				
				Step.Position += Step.Direction;
				Distance += Step.Distance;
			}
		}
	}

	Distance = TraceDistance;
	return false;
}