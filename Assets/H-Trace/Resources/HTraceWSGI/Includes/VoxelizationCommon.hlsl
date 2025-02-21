#pragma once
#include "../Headers/HMain.hlsl"

float3 _VoxelizationAABB_Min;
float3 _VoxelizationAABB_Max;
float3 _VoxelResolution;
float3 _VoxelCameraPos;
float _VoxelPerMeter;
float3 _VoxelBounds;
float _VoxelSize;

H_RW_TEXTURE3D(uint, _VoxelData);
H_TEXTURE3D(uint, _VoxelPositionPyramid);


// ------------------------ STRUCTS ------------------------
struct VoxelPayload
{
    uint3 HitCoord;
    float3 HitColor;
    float3 HitCache;
    float3 HitNormal;
    float3 HitDiffuse;
    float3 HitPosition;
    float HitDistance;
};

void InitializePayload(inout VoxelPayload Payload)
{	
    Payload.HitCoord = 0;
    Payload.HitColor = 0;
    Payload.HitCache = 0;
    Payload.HitNormal = 0;
    Payload.HitDiffuse = 0;
    Payload.HitPosition = 0;
    Payload.HitDistance = 0;
}


// ------------------------ FUNCTIONS ------------------------
bool IsWorldPositionInBounds(float3 WorldPositionAbsolute)
{
    float TestX = step(_VoxelizationAABB_Min.x, WorldPositionAbsolute.x) * step(WorldPositionAbsolute.x, _VoxelizationAABB_Max.x);
    float TestY = step(_VoxelizationAABB_Min.y, WorldPositionAbsolute.y) * step(WorldPositionAbsolute.y, _VoxelizationAABB_Max.y);
    float TestZ = step(_VoxelizationAABB_Min.z, WorldPositionAbsolute.z) * step(WorldPositionAbsolute.z, _VoxelizationAABB_Max.z);
	
    return TestX * TestY * TestZ;
}

bool IsVoxelCoordInBounds(int3 VoxelCoord)
{
    VoxelCoord.xyz = VoxelCoord.xzy;

    float TestX = step(0.0f, VoxelCoord.x) * step(VoxelCoord.x, _VoxelResolution.x);
    float TestY = step(0.0f, VoxelCoord.y) * step(VoxelCoord.y, _VoxelResolution.y);
    float TestZ = step(0.0f, VoxelCoord.z) * step(VoxelCoord.z, _VoxelResolution.z);
	
    return TestX * TestY * TestZ;
}


int3 AbsoluteWorldPositionToVoxelCoord(float3 WorldPos)
{
    // TODO: seems like floor must be used instead of round here
    int3 VoxelBoxCenter = int3(_VoxelResolution.xzy / 2);
    int3 VoxelPosition = round(WorldPos * _VoxelPerMeter) - VoxelBoxCenter;	// Must keep int here to detect going out of bounds  
    return _VoxelResolution.xzy + VoxelPosition;
}

float3 VoxelCoordToAbsoluteWorldPosition(int3 VoxelPos)
{
    int3 VoxelBoxCenter = int3(_VoxelResolution.xzy / 2);
    return float3(VoxelPos - VoxelBoxCenter) / _VoxelPerMeter + _VoxelCameraPos;
}

int3 VoxelCoordToAbsoluteVoxelCoord(int3 HitCoord)
{
    int3 VoxelBoxCenter = int3(_VoxelResolution.xzy / 2);
    return int3((HitCoord - VoxelBoxCenter) + round(_VoxelCameraPos / _VoxelSize));
}

float3 ComputeRadianceCacheCoord(int3 HitCoord)
{
    float3 CacheCoord = HitCoord + _VoxelCameraPos * _VoxelPerMeter; 
    int3 CacheSpans = floor(CacheCoord / int3(_VoxelResolution.xzy));
    return round(CacheCoord - CacheSpans * _VoxelResolution.xzy);
}