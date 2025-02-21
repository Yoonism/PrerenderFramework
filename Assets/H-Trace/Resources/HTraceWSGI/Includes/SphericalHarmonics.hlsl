#pragma once


struct SH_L2_Vector
{
	float4 V0;
	float4 V1;
	float  V2;
};

struct SH_L2_Color
{
	SH_L2_Vector R;
	SH_L2_Vector G;
	SH_L2_Vector B;
};

SH_L2_Vector BasisSH(float3 Direction)
{
	SH_L2_Vector Result;

	Result.V0.x =  0.282095f; 
	Result.V0.y = -0.488603f * Direction.y;
	Result.V0.z =  0.488603f * Direction.z;
	Result.V0.w = -0.488603f * Direction.x;

	float3 DirectionSquared = Direction * Direction;
	Result.V1.x =  1.092548f * Direction.x * Direction.y;
	Result.V1.y = -1.092548f * Direction.y * Direction.z;
	Result.V1.z =  0.315392f * (3.0f * DirectionSquared.z - 1.0f);
	Result.V1.w = -1.092548f * Direction.x * Direction.z;
	Result.V2 =    0.546274f * (DirectionSquared.x - DirectionSquared.y);

	return Result;
}

void InitializeSH(inout SH_L2_Color CoefficientsSH)
{
	CoefficientsSH.R.V0 =  0;
	CoefficientsSH.R.V1 =  0;
	CoefficientsSH.R.V2 =  0;
	CoefficientsSH.G.V0 =  0;
	CoefficientsSH.G.V1 =  0;
	CoefficientsSH.G.V2 =  0;
	CoefficientsSH.B.V0 =  0;
	CoefficientsSH.B.V1 =  0;
	CoefficientsSH.B.V2 =  0;
}

void PackInterpolationSH(inout uint4 PackedA, inout uint4 PackedB, SH_L2_Color LightingSH)
{
	float4 AmbientSH = float4(LightingSH.R.V0.x , LightingSH.G.V0.x, LightingSH.B.V0.x, 1);
	
	float4 DirectionalSH0_0 = float4(LightingSH.R.V0.yzw, LightingSH.R.V1.x);
	float4 DirectionalSH0_1 = float4(LightingSH.G.V0.yzw, LightingSH.G.V1.x);
	float4 DirectionalSH0_2 = float4(LightingSH.B.V0.yzw, LightingSH.B.V1.x);
 
	float4 DirectionalSH1_0 = float4(LightingSH.R.V1.yzw, LightingSH.R.V2);
	float4 DirectionalSH1_1 = float4(LightingSH.G.V1.yzw, LightingSH.G.V2);
	float4 DirectionalSH1_2 = float4(LightingSH.B.V1.yzw, LightingSH.B.V2);

	float4 NormalizationScale0 = float4(1,1,1,1);
	float4 NormalizationScale1 = float4(1,1,1,1);
  
	NormalizationScale0 = float4(0.282095f / 0.488603f, 0.282095f / 0.488603f, 0.282095f / 0.488603f, 0.282095f / 1.092548f);
	NormalizationScale1 = float4(0.282095f / 1.092548f, 0.282095f / (4.0f * 0.315392f), 0.282095f / 1.092548f, 0.282095f / (2.0f * 0.546274f));

	DirectionalSH0_0 = DirectionalSH0_0 * NormalizationScale0 / max(AmbientSH[0], .00001f) * 0.5f + 0.5f;
	DirectionalSH0_1 = DirectionalSH0_1 * NormalizationScale0 / max(AmbientSH[1], .00001f) * 0.5f + 0.5f;
	DirectionalSH0_2 = DirectionalSH0_2 * NormalizationScale0 / max(AmbientSH[2], .00001f) * 0.5f + 0.5f;

	DirectionalSH1_0 = DirectionalSH1_0 * NormalizationScale1 / max(AmbientSH[0], .00001f) * 0.5f + 0.5f;
	DirectionalSH1_1 = DirectionalSH1_1 * NormalizationScale1 / max(AmbientSH[1], .00001f) * 0.5f + 0.5f;
	DirectionalSH1_2 = DirectionalSH1_2 * NormalizationScale1 / max(AmbientSH[2], .00001f) * 0.5f + 0.5f;

	PackedA.x =  ((uint(DirectionalSH0_0.x * 255.0f + 0.5f) & 0xFF) << 24) |
				 ((uint(DirectionalSH0_0.y * 255.0f + 0.5f) & 0xFF) << 16) |
				 ((uint(DirectionalSH0_0.z * 255.0f + 0.5f) & 0xFF) <<  8) |
				 ((uint(DirectionalSH0_0.w * 255.0f + 0.5f) & 0xFF) <<  0);

	PackedA.y =  ((uint(DirectionalSH0_1.x * 255.0f + 0.5f) & 0xFF) << 24) |
			 	 ((uint(DirectionalSH0_1.y * 255.0f + 0.5f) & 0xFF) << 16) |
			 	 ((uint(DirectionalSH0_1.z * 255.0f + 0.5f) & 0xFF) <<  8) |
			 	 ((uint(DirectionalSH0_1.w * 255.0f + 0.5f) & 0xFF) <<  0);

	PackedA.z =  ((uint(DirectionalSH0_2.x * 255.0f + 0.5f) & 0xFF) << 24) |
				 ((uint(DirectionalSH0_2.y * 255.0f + 0.5f) & 0xFF) << 16) |
				 ((uint(DirectionalSH0_2.z * 255.0f + 0.5f) & 0xFF) <<  8) |
				 ((uint(DirectionalSH0_2.w * 255.0f + 0.5f) & 0xFF) <<  0);

	PackedA.w =  ((uint(DirectionalSH1_0.x * 255.0f + 0.5f) & 0xFF) << 24) |
				 ((uint(DirectionalSH1_0.y * 255.0f + 0.5f) & 0xFF) << 16) |
				 ((uint(DirectionalSH1_0.z * 255.0f + 0.5f) & 0xFF) <<  8) |
				 ((uint(DirectionalSH1_0.w * 255.0f + 0.5f) & 0xFF) <<  0);

	PackedB.x =  ((uint(DirectionalSH1_1.x * 255.0f + 0.5f) & 0xFF) << 24) |
			 	 ((uint(DirectionalSH1_1.y * 255.0f + 0.5f) & 0xFF) << 16) |
			 	 ((uint(DirectionalSH1_1.z * 255.0f + 0.5f) & 0xFF) <<  8) |
			 	 ((uint(DirectionalSH1_1.w * 255.0f + 0.5f) & 0xFF) <<  0);

	PackedB.y =  ((uint(DirectionalSH1_2.x * 255.0f + 0.5f) & 0xFF) << 24) |
			  	 ((uint(DirectionalSH1_2.y * 255.0f + 0.5f) & 0xFF) << 16) |
			  	 ((uint(DirectionalSH1_2.z * 255.0f + 0.5f) & 0xFF) <<  8) |
			  	 ((uint(DirectionalSH1_2.w * 255.0f + 0.5f) & 0xFF) <<  0);

	PackedB.z =  PackToR11G11B10f(AmbientSH.xyz);

	PackedB.w = 0;
}

void UnpackInterpolationSH(uint4 PackedA, uint4 PackedB, inout SH_L2_Color CoefficientsSH)
{
	float4 DirectionalSH0[3], DirectionalSH1[3], AmbientSH;

	DirectionalSH0[0].x = ((PackedA.x >> 24) & 0xFF) / 255.0f;
	DirectionalSH0[0].y = ((PackedA.x >> 16) & 0xFF) / 255.0f;
	DirectionalSH0[0].z = ((PackedA.x >>  8) & 0xFF) / 255.0f;
	DirectionalSH0[0].w = ((PackedA.x >>  0) & 0xFF) / 255.0f;

	DirectionalSH0[1].x = ((PackedA.y >> 24) & 0xFF) / 255.0f;
	DirectionalSH0[1].y = ((PackedA.y >> 16) & 0xFF) / 255.0f;
	DirectionalSH0[1].z = ((PackedA.y >>  8) & 0xFF) / 255.0f;
	DirectionalSH0[1].w = ((PackedA.y >>  0) & 0xFF) / 255.0f;

	DirectionalSH0[2].x = ((PackedA.z >> 24) & 0xFF) / 255.0f;
	DirectionalSH0[2].y = ((PackedA.z >> 16) & 0xFF) / 255.0f;
	DirectionalSH0[2].z = ((PackedA.z >>  8) & 0xFF) / 255.0f;
	DirectionalSH0[2].w = ((PackedA.z >>  0) & 0xFF) / 255.0f;

	DirectionalSH1[0].x = ((PackedA.w >> 24) & 0xFF) / 255.0f;
	DirectionalSH1[0].y = ((PackedA.w >> 16) & 0xFF) / 255.0f;
	DirectionalSH1[0].z = ((PackedA.w >>  8) & 0xFF) / 255.0f;
	DirectionalSH1[0].w = ((PackedA.w >>  0) & 0xFF) / 255.0f;

	DirectionalSH1[1].x = ((PackedB.x >> 24) & 0xFF) / 255.0f;
	DirectionalSH1[1].y = ((PackedB.x >> 16) & 0xFF) / 255.0f;
	DirectionalSH1[1].z = ((PackedB.x >>  8) & 0xFF) / 255.0f;
	DirectionalSH1[1].w = ((PackedB.x >>  0) & 0xFF) / 255.0f;

	DirectionalSH1[2].x = ((PackedB.y >> 24) & 0xFF) / 255.0f;
	DirectionalSH1[2].y = ((PackedB.y >> 16) & 0xFF) / 255.0f;
	DirectionalSH1[2].z = ((PackedB.y >>  8) & 0xFF) / 255.0f;
	DirectionalSH1[2].w = ((PackedB.y >>  0) & 0xFF) / 255.0f;
	
	AmbientSH = float4(UnpackFromR11G11B10f(PackedB.z), 1);

	float4 DenormalizationScales0 = float4(1,1,1,1);
	float4 DenormalizationScales1 = float4(1,1,1,1);
   
	DenormalizationScales0 = float4(0.488603f / 0.282095f, 0.488603f / 0.282095f, 0.488603f / 0.282095f, 1.092548f / 0.282095f);
	DenormalizationScales1 = float4(1.092548f / 0.282095f, 4.0f * 0.315392f / 0.282095f, 1.092548f / 0.282095f, 2.0f * 0.546274f / 0.282095f);
   
	DirectionalSH0[0] = (DirectionalSH0[0] * 2 - 1) * AmbientSH.x * DenormalizationScales0;
	DirectionalSH0[1] = (DirectionalSH0[1] * 2 - 1) * AmbientSH.y * DenormalizationScales0;
	DirectionalSH0[2] = (DirectionalSH0[2] * 2 - 1) * AmbientSH.z * DenormalizationScales0;
	
	DirectionalSH1[0] = (DirectionalSH1[0] * 2 - 1) * AmbientSH.x * DenormalizationScales1;
	DirectionalSH1[1] = (DirectionalSH1[1] * 2 - 1) * AmbientSH.y * DenormalizationScales1;
	DirectionalSH1[2] = (DirectionalSH1[2] * 2 - 1) * AmbientSH.z * DenormalizationScales1;
	
	CoefficientsSH.R.V0 = float4(AmbientSH.x, DirectionalSH0[0].xyz);
	CoefficientsSH.G.V0 = float4(AmbientSH.y, DirectionalSH0[1].xyz);
	CoefficientsSH.B.V0 = float4(AmbientSH.z, DirectionalSH0[2].xyz);
	
	CoefficientsSH.R.V1 = float4(DirectionalSH0[0].w, DirectionalSH1[0].xyz);
	CoefficientsSH.G.V1 = float4(DirectionalSH0[1].w, DirectionalSH1[1].xyz);
	CoefficientsSH.B.V1 = float4(DirectionalSH0[2].w, DirectionalSH1[2].xyz);
	
	CoefficientsSH.R.V2 = DirectionalSH1[0].w;
	CoefficientsSH.G.V2 = DirectionalSH1[1].w;
	CoefficientsSH.B.V2 = DirectionalSH1[2].w;
}

void AddSampleSH(inout SH_L2_Color Result, float3 Direction, float3 Color)
{
	SH_L2_Vector Basis = BasisSH(Direction);
	
	Result.R.V0 += Basis.V0 * Color.r;
	Result.R.V1 += Basis.V1 * Color.r;
	Result.R.V2 += Basis.V2 * Color.r;
	Result.G.V0 += Basis.V0 * Color.g;
	Result.G.V1 += Basis.V1 * Color.g;
	Result.G.V2 += Basis.V2 * Color.g;
	Result.B.V0 += Basis.V0 * Color.b;
	Result.B.V1 += Basis.V1 * Color.b;
	Result.B.V2 += Basis.V2 * Color.b;
}

float3 EvaluateSHIrradiance(float3 Direction, float CosThetaAO, SH_L2_Color SH)
{
	float t2 = CosThetaAO * CosThetaAO;
	float t3 = t2 * CosThetaAO;
	float t4 = t2 * t2;

	float c0 = 0.5f * sqrt(PI);
	float c1 = sqrt(PI / 3.0f);
	float c2 = sqrt(5.0f * PI) / 16.0f * (3.0f - 2.0f);

	c0 = c0 * (1 - t2);
	c1 = c1 * (1 - t3);
	c2 = c2 / 16.0f * (3.0f * (1.0f - t4) - 2.0f * (1.0f - t2));

	return max(0.0f, c0 * float3(SH.R.V0.x, SH.G.V0.x, SH.B.V0.x)
		+ c1 * (-float3(SH.R.V0.y, SH.G.V0.y, SH.B.V0.y) * Direction.y + float3(SH.R.V0.z, SH.G.V0.z, SH.B.V0.z) * Direction.z - float3(SH.R.V0.w, SH.G.V0.w, SH.B.V0.w) * Direction.x)
		+ c2 * (float3(SH.R.V1.z, SH.G.V1.z, SH.B.V1.z) * (3.0f * Direction.z * Direction.z - 1.0f)
			+ sqrt(3.0f) * (float3(SH.R.V2, SH.G.V2, SH.B.V2) * (Direction.x * Direction.x - Direction.y * Direction.y)
				+ 2.0f * (float3(SH.R.V1.x, SH.G.V1.x, SH.B.V1.x) * Direction.x * Direction.y - float3(SH.R.V1.y, SH.G.V1.y, SH.B.V1.y) * Direction.y * Direction.z - float3(SH.R.V1.w, SH.G.V1.w, SH.B.V1.w) * Direction.z * Direction.x))));
}