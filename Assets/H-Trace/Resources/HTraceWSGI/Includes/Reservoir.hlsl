#pragma once
#include "../Headers/HMain.hlsl"
#include "../Headers/HMath.hlsl"
#include "../Headers/HPacking.hlsl"

H_TEXTURE(_ProbeDiffuse);

// Only Ray data
H_TEXTURE(_ReservoirAtlasRayData);
H_TEXTURE(_ReservoirAtlasRayData_Disocclusion);
H_RW_TEXTURE(uint, _ReservoirAtlasRayData_Output);

// Only Radiance data
H_TEXTURE(_ReservoirAtlasRadianceData);
H_RW_TEXTURE(uint2, _ReservoirAtlasRadianceData_Inout);
H_RW_TEXTURE(uint2, _ReservoirAtlasRadianceData_Output);

// Full reservoir with Radiance & Ray datas
H_TEXTURE(_ReservoirAtlas);
H_TEXTURE(_ReservoirAtlas_History);
H_RW_TEXTURE(uint4, _ReservoirAtlas_Output);

uint _UseDiffuseWeight;

// ------------------------ RESERVOIR STRUCTS -----------------------
struct RadianceData
{
    float3 Color;
    float Wsum;
    float M;
    float W;
};

struct OriginData
{
    // Empty for now, but we will need it for validation
};

struct RayData
{
    float3 OriginNormal;
    float3 Direction;
    float Distance;
};

struct Reservoir
{
    uint2 MergedCoord;
    
    RadianceData Radiance;
    RayData Ray;
};



// ------------------------ RESERVOIR PACKING FUNCTIONS-----------------------
uint2 PackRadianceData(RadianceData Radiance)
{
    uint W = f32tof16(Radiance.W);		
    uint M = f32tof16(Radiance.M);		
    uint PackedMW = (W << 16) | (M << 0);
    uint PackedColor = PackTonemappedColor24bit(Radiance.Color);
    
    return uint2(PackedColor, PackedMW);
}

uint2 PackRayData(RayData Ray)
{
    uint DirectionPacked = PackDirection24bit(Ray.Direction);
    uint DistancePacked = (f32tof16(Ray.Distance) >> 8) & 0xFF;

    uint DistanceDirectionPacked = (DistancePacked << 24) | DirectionPacked;
    uint OriginNormalPacked = PackDirection24bit(Ray.OriginNormal);

    return uint2(DistanceDirectionPacked, OriginNormalPacked);
}

void UnpackRadianceData(uint2 RadianceDataPacked, float3 Diffuse, inout RadianceData Radiance)
{
    Radiance.Color = UnpackTonemappedColor24bit(RadianceDataPacked.x);
    Radiance.W = f16tof32(RadianceDataPacked.y >> 16);
    Radiance.M = f16tof32(RadianceDataPacked.y >> 0);
    Radiance.Wsum = Radiance.W * Radiance.M * Luminance(Radiance.Color * Diffuse);
}

void UnpackRayData(uint2 RayDataPacked, inout RayData Ray)
{
    Ray.Direction = UnpackDirection24bit(RayDataPacked.x); 
    Ray.Distance = f16tof32(((RayDataPacked.x >> 24) & 0xFF) << 8);
    Ray.OriginNormal = UnpackDirection24bit(RayDataPacked.y);
}

uint PackOcclusion(float Occlusion, bool IsDisocclusion)
{
    uint OcclusionPacked = uint(Occlusion * 127.0f + 0.5f) & 0x7F;
    return (OcclusionPacked << 24) | (IsDisocclusion << 31);
}

float UnpackOcclusion(uint OcclusionPacked, out bool IsDisocclusion)
{
    IsDisocclusion = OcclusionPacked >> 31;
    return ((OcclusionPacked >> 24) & 0x7F) / 127.0f; 
}


// ------------------------ RESERVOIR FUNCTIONS -----------------------
float3 GetReservoirDiffuse(uint2 pixCoord)
{
    float3 DiffuseBuffer = _UseDiffuseWeight ? H_LOAD(_ProbeDiffuse, pixCoord).xyz : 1.0f;

    if (DiffuseBuffer.x + DiffuseBuffer.y + DiffuseBuffer.z == 0)
        DiffuseBuffer = float3(0.05, 0.05, 0.05);

    return DiffuseBuffer;
}

// Reservoir update
bool ReservoirUpdate(uint2 SampleCoord, float3 SampleColor, float SampleW, float SampleM, inout Reservoir Reservoir, inout uint Random)
{
    float RandomValue = UintToFloat01(Hash1Mutate(Random));
    
    Reservoir.Radiance.Wsum += SampleW;
    Reservoir.Radiance.M += SampleM;
    
    if (RandomValue < SampleW / Reservoir.Radiance.Wsum)
    {
        Reservoir.Radiance.Color = SampleColor;
        Reservoir.MergedCoord = SampleCoord;
        
        return true;
    }
    
    return false;
}

// Reservoir update with RayData
bool ReservoirUpdate(uint2 SampleCoord, float3 SampleColor, float SampleW, float SampleM, RayData SampleRay, inout Reservoir Reservoir, inout uint Random)
{
    float RandomValue = UintToFloat01(Hash1Mutate(Random));
    
    Reservoir.Radiance.Wsum += SampleW;
    Reservoir.Radiance.M += SampleM;
    
    if (RandomValue < SampleW / Reservoir.Radiance.Wsum)
    {   
        Reservoir.Radiance.Color = SampleColor;
        Reservoir.MergedCoord = SampleCoord;
        
        Reservoir.Ray.OriginNormal = SampleRay.OriginNormal;
        Reservoir.Ray.Direction = SampleRay.Direction;
        Reservoir.Ray.Distance = SampleRay.Distance;
        
        return true;
    }
    
    return false;
}

// Merges central reservoir with a temporal neighbour (Radiance & Ray datas are exhanged) loaded externally
bool ReservoirMergeTemporal(uint2 SampleCoord, uint4 SampleReservoirPacked, uint ArrayIndex, float SampleWeight, float3 Diffuse, inout uint Random, inout Reservoir Reservoir)
{
    RadianceData SampleRadiance;
    OriginData SampleOrigin;
    RayData SampleRay;
   
    UnpackRadianceData(SampleReservoirPacked.xy, Diffuse, SampleRadiance);
    UnpackRayData(SampleReservoirPacked.zw, SampleRay);

    SampleRadiance.Wsum *= SampleWeight;
    SampleRadiance.M *= SampleWeight;
    
    return ReservoirUpdate(SampleCoord, SampleRadiance.Color, SampleRadiance.Wsum, SampleRadiance.M, SampleRay, Reservoir, Random);
}

// Merges central reservoir with a spatial neighbour (only RadianceData is exchanged) loaded externally
bool ReservoirMergeSpatial(uint2 SampleCoord, uint2 SampleReservoirPacked, float SampleWeight, float3 Diffuse, inout Reservoir Reservoir, inout uint Random)
{
    RadianceData SampleRadiance;
    UnpackRadianceData(SampleReservoirPacked, Diffuse, SampleRadiance);

    SampleRadiance.Wsum *= SampleWeight;
    SampleRadiance.M *= SampleWeight;
    
    return ReservoirUpdate(SampleCoord, SampleRadiance.Color, SampleRadiance.Wsum, SampleRadiance.M, Reservoir, Random);
}


// Empty RadianceData initialization
void RadianceDataInitialize(out RadianceData Radiance)
{
    Radiance.Color = 0;
    Radiance.Wsum = 0;
    Radiance.M = 0;
    Radiance.W = 0;
}

// Empty RayData initialization
void RayDataInitialize(out RayData Ray)
{
    Ray.OriginNormal = 0;
    Ray.Direction = 0;
    Ray.Distance = 0;
}

// Empty reservoir initialization
void ReservoirInitialize(uint2 Coord, out Reservoir Reservoir)
{
    Reservoir.MergedCoord = Coord;
    
    RadianceDataInitialize(Reservoir.Radiance);
    RayDataInitialize(Reservoir.Ray);
}