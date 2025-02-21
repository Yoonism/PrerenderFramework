#pragma once
#include "../Headers/HMain.hlsl"
#include "../Headers/HMath.hlsl"
#include "../Headers/HPacking.hlsl"


// ------------------------ COMMON VARIABLES & BUFFFERS -----------------------
H_TEXTURE_UINT2(_HTraceStencilBuffer);

float _CellSize;

int _ProbeSize;
int _OctahedralSize;
int _PersistentHistorySamples;


// ------------------------ TEMPORAL REPROJECTION STRUCTS -----------------------
struct CurrentFrameData
{
    float3  Normal;
    float3  WorldPos;
    float   DepthRaw;
    float   AligmentZ;
    float   DepthLinear;
    bool    MovingPixel;
};

struct PrevFrameData
{
    float3  Normal;
    float3  WorldPos;
    float   DepthLinear;
};


// ------------------------ TEMPORAL REPROJECTION FUNCTIONS -----------------------
int GetHistoryIndex(int Index)
{
    Index += 1;
    
    int HistoryIndex = uint(_FrameCount) % _PersistentHistorySamples - Index;
    
    if (HistoryIndex < 0)
        HistoryIndex = _PersistentHistorySamples - abs(HistoryIndex);

    return HistoryIndex;
}


float3 DirectClipToAABB(float3 History, float3 Min, float3 Max)
{
    float3 Center  = 0.5 * (Max + Min);
    float3 Extents = 0.5 * (Max - Min);
    
    float3 Offset = History - Center;
    float3 Vunit = Offset.xyz / Extents.xyz;
    float3 AbsUnit = abs(Vunit);
    float MaxUnit = max(max(AbsUnit.x, AbsUnit.y), AbsUnit.z);

    if (MaxUnit > 1.0) return Center + (Offset / MaxUnit);
    else  return History;
}


float DisocclusionDetection(CurrentFrameData CurrentData, PrevFrameData PrevData, bool MovingIntersection, out float RelaxedWeight, out float DisocclusionWeight)
{
    RelaxedWeight = 1;
    DisocclusionWeight = 1;
    
    float PlaneMultiplier = CurrentData.MovingPixel ? 100.0f : 100000.0f; //TODO: make it 5000 for the editor window
    float DepthMultiplier = CurrentData.MovingPixel ? 20.0f : 1.0f;
    
    // Depth-based rejection with an adaptive threshold
    float DepthThreshold = lerp(1e-2f, 1e-1f, CurrentData.AligmentZ);
    if (abs((PrevData.DepthLinear - CurrentData.DepthLinear) / CurrentData.DepthLinear) >= DepthThreshold * DepthMultiplier)
    {
        if (CurrentData.DepthLinear > PrevData.DepthLinear)
            DisocclusionWeight = 0;
    
        RelaxedWeight = 0;
        return 0.0f;
    }
    
    // Plane-based rejection
    float PlaneDistance = abs(dot(PrevData.WorldPos - CurrentData.WorldPos, CurrentData.Normal));
    float RelativeDepthDifference = PlaneDistance / CurrentData.DepthLinear;
    if (exp2(-PlaneMultiplier * (RelativeDepthDifference * RelativeDepthDifference )) < 0.1f)
    {
        RelaxedWeight = 0;
        return 0.0f;
    }
    
    // Normal-based rejection
    if (CurrentData.DepthLinear > PrevData.DepthLinear)
    {
        if (saturate(dot(CurrentData.Normal, PrevData.Normal)) < 0.75)
        {
            RelaxedWeight = 0;
            return  0.0f;
        }
    }
    else 
    {
        if (saturate(dot(CurrentData.Normal, PrevData.Normal)) < 0.75)
            return  0.0f;
    }

    return 1.0f;
}


bool GetReprojectionCoord(int2 pixCoord, float2 MotionVectors, out float4 BilinearWeights, out int2 ReprojectionCoord)
{
    float2 ReprojectionCoordNDC = (pixCoord.xy + 0.5f) - MotionVectors * floor(_ScreenSize.xy / _ProbeSize);
    ReprojectionCoord = ReprojectionCoordNDC - 0.5f;

    float UVx = frac(ReprojectionCoordNDC.x + 0.5f);
    float UVy = frac(ReprojectionCoordNDC.y + 0.5f);

    BilinearWeights.x = (1.0f - UVx) * (1.0f - UVy);
    BilinearWeights.y = (UVx) * (1.0f - UVy);
    BilinearWeights.z = (1.0f - UVx) * (UVy);
    BilinearWeights.w = (UVx) * (UVy);

    if (any(ReprojectionCoord * _ProbeSize >= _ScreenSize.xy) || any(ReprojectionCoordNDC < 0))
    {
        BilinearWeights = float4(0,0,0,0);
        return false;
    }

    return true;
}


bool GetReprojectionWeights(H_TEXTURE(_HistoryBuffer), CurrentFrameData CurrentData, int2 ReprojectionCoord, uint ArrayIndex, inout float4 Weights, inout float4 RelaxedWeights)
{
    PrevFrameData PrevData00, PrevData01, PrevData10, PrevData11;
    
    UnpackWorldPosNormal(asuint(H_LOAD_ARRAY(_HistoryBuffer, ReprojectionCoord + int2(0, 0), ArrayIndex)), PrevData00.WorldPos, PrevData00.Normal); 
    UnpackWorldPosNormal(asuint(H_LOAD_ARRAY(_HistoryBuffer, ReprojectionCoord + int2(1, 0), ArrayIndex)), PrevData10.WorldPos, PrevData10.Normal); 
    UnpackWorldPosNormal(asuint(H_LOAD_ARRAY(_HistoryBuffer, ReprojectionCoord + int2(0, 1), ArrayIndex)), PrevData01.WorldPos, PrevData01.Normal); 
    UnpackWorldPosNormal(asuint(H_LOAD_ARRAY(_HistoryBuffer, ReprojectionCoord + int2(1, 1), ArrayIndex)), PrevData11.WorldPos, PrevData11.Normal); 

    PrevData00.DepthLinear = LinearEyeDepth(GetCameraRelativePositionWS(PrevData00.WorldPos), UNITY_MATRIX_V);
    PrevData10.DepthLinear = LinearEyeDepth(GetCameraRelativePositionWS(PrevData10.WorldPos), UNITY_MATRIX_V);
    PrevData01.DepthLinear = LinearEyeDepth(GetCameraRelativePositionWS(PrevData01.WorldPos), UNITY_MATRIX_V);
    PrevData11.DepthLinear = LinearEyeDepth(GetCameraRelativePositionWS(PrevData11.WorldPos), UNITY_MATRIX_V);
    
    float4 BilinearWeights = Weights;
    
    float4 DisocclusionWeights;
    Weights.x *= DisocclusionDetection(CurrentData, PrevData00, false, RelaxedWeights.x, DisocclusionWeights.x);
    Weights.y *= DisocclusionDetection(CurrentData, PrevData10, false, RelaxedWeights.y, DisocclusionWeights.y);
    Weights.z *= DisocclusionDetection(CurrentData, PrevData01, false, RelaxedWeights.z, DisocclusionWeights.z);
    Weights.w *= DisocclusionDetection(CurrentData, PrevData11, false, RelaxedWeights.w, DisocclusionWeights.w);
    
    return any(DisocclusionWeights <= 0) ? true : false;
}