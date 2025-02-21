#pragma once

#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1

#define EVALUATE_LIT 1
#define EVALUATE_UNLIT 1
#define EVALUATE_TERRAIN 1
#define EVALUATE_EMISSION 1
#define EVALUATE_SPEEDTREE 1
#define EVALUATE_LAYERED_LIT 0

#define AXIS_X 0
#define AXIS_Y 1
#define AXIS_Z 2

#include "../Headers/HPacking.hlsl"
#include "VoxelizationCommon.hlsl"
#include "VoxelMaterialEvaluation.hlsl" 

H_RW_TEXTURE3D(uint, _VoxelColor);

// Terrain instancing properties
float4 _TerrainHeightmapRecipSize;
float4 _TerrainHeightmapScale;  
UNITY_INSTANCING_BUFFER_START(Terrain)
UNITY_DEFINE_INSTANCED_PROP(float4, _TerrainPatchInstanceData)  
UNITY_INSTANCING_BUFFER_END(Terrain)
TEXTURE2D(_TerrainHeightmapTexture);

int _OffsetAxisIndex;
int2 _CullingTrim;
int2 _CullingTrimAxis;
float3 _AxisOffset;
float3 _OctantOffset;
float3 _VoxelCameraPosActual;

// ------------------------ SHARED STRUCTS ------------------------
struct GeometryToFragment
{
    float4 Position        : POSITION;
    float2 TextureCoord0   : TEXCOORD0;

    #if EVALUATE_LAYERED_LIT
    float2 TextureCoord1   : TEXCOORD1;
    #endif
    
    float Axis             : TEXCOORD2;
};


struct VertexToGeometry
{
    float4 Position        : POSITION;
    float2 TextureCoord0   : TEXCOORD0;

    #if EVALUATE_LAYERED_LIT
    float2 TextureCoord1   : TEXCOORD1;
    #endif

    int CullingTest        : TEXCOORD2;
    #ifdef PARTIAL_VOXELIZATION
    #endif
};


// ------------------------ SHARED FUNCTIONS ------------------------
float3 SwizzleAxis(float3 Position, uint Axis)
{
    uint a = Axis + 1;
    float3 p = Position;
    Position.x = p[(0 + a) % 3];
    Position.y = p[(1 + a) % 3];
    Position.z = p[(2 + a) % 3];

    return Position;
}

float3 RestoreAxis(float3 Position, uint Axis)
{
    uint a = 2 - Axis;
    float3 p = Position;
    Position.x = p[(0 + a) % 3];
    Position.y = p[(1 + a) % 3];
    Position.z = p[(2 + a) % 3]; 
    
    return Position;
}

void ModifyForTerrainInstancing(inout AttributesMesh InputMesh)
{   
    float2 PatchVertex = InputMesh.positionOS.xy;
    float4 InstanceData = UNITY_ACCESS_INSTANCED_PROP(Terrain, _TerrainPatchInstanceData);

    float2 TextureCoord = (PatchVertex.xy + InstanceData.xy) * InstanceData.z;
    float HeightmapTexture = UnpackHeightmap(_TerrainHeightmapTexture.Load(int3(TextureCoord, 0)));
    float HolesTexture = _TerrainHolesTexture.Load(int3(TextureCoord, 0)).x;

    if (abs(HolesTexture.x - 0.5) == 0.5) //HolesTexture.x == 1 || HolesTexture.x == 0)
    {
        InputMesh.positionOS.xz = TextureCoord * _TerrainHeightmapScale.xz;
        InputMesh.positionOS.y = HeightmapTexture * _TerrainHeightmapScale.y;
        
        float4 Scale = InstanceData.z * _TerrainHeightmapRecipSize;
        float4 Offset = InstanceData.xyxy * Scale;
        Offset.xy += 0.5f * _TerrainHeightmapRecipSize.xy;
        InputMesh.uv0 = (PatchVertex.xy * Scale.zw + Offset.zw);

        InputMesh.positionOS.xyz = mul(GetRawUnityObjectToWorld(), float4(InputMesh.positionOS.xyz - _WorldSpaceCameraPos, 1.0)).xyz;
    }
    else
    {
       InputMesh.positionOS.xyz = mul(GetObjectToWorldMatrix(), float4(InputMesh.positionOS, 1.0)).xyz;
    }
}


// --- Vertex Stage ---
VertexToGeometry VoxelizationVert(AttributesMesh inputMesh)
{
    VertexToGeometry Output;

    float3 PositionWS;
    
    // Process instancing
    #ifdef UNITY_INSTANCING_ENABLED
    UNITY_SETUP_INSTANCE_ID(inputMesh);
    ModifyForTerrainInstancing(inputMesh);
    Output.Position = mul(GetWorldToHClipMatrix(), float4(inputMesh.positionOS, 1.0));
    PositionWS = inputMesh.positionOS;
    #else
    Output.Position = TransformObjectToHClip(inputMesh.positionOS);
    PositionWS = TransformObjectToWorld(inputMesh.positionOS);
    #endif

    // Output uv channels
    Output.TextureCoord0 = inputMesh.uv0;
    #if EVALUATE_LAYERED_LIT
    Output.TextureCoord1 = inputMesh.uv1;
    #endif
   
    #ifdef PARTIAL_VOXELIZATION
    // Transform world position to voxel coordinate
    float3 WorldPosition = PositionWS + (_WorldSpaceCameraPos - _VoxelCameraPosActual);
    int3 VoxelCoord = AbsoluteWorldPositionToVoxelCoord(WorldPosition);
    
    // Check if the vertex is behind culling camera
    Output.CullingTest = 0; 
    if ((VoxelCoord[_OffsetAxisIndex] < _CullingTrim.x) * _CullingTrimAxis.x || (VoxelCoord[_OffsetAxisIndex] > _CullingTrim.y + 1) * _CullingTrimAxis.y)
    Output.CullingTest = 1;
    #endif

    Output.CullingTest = 0; 
    
    #ifdef UNITY_REVERSED_Z
    Output.Position.z = mad(Output.Position.z, -2.0, 1.0);
    #endif

    return Output;
}

// --- Geometry Stage ---
[maxvertexcount(3)]
void VoxelizationGeom(triangle VertexToGeometry i[3], inout TriangleStream<GeometryToFragment> Stream)
{
    // If all 3 vertices are behind culling camera - early out
    #ifdef PARTIAL_VOXELIZATION
    if (i[0].CullingTest + i[1].CullingTest + i[2].CullingTest == 3)
        return;
    #endif
    
    float3 Normal = normalize(abs(cross(i[1].Position.xyz - i[0].Position.xyz, i[2].Position.xyz - i[0].Position.xyz)));
    
    uint Axis = AXIS_Z;
    if  (Normal.x > Normal.y && Normal.x > Normal.z)
         Axis = AXIS_X;
    else if (Normal.y > Normal.x && Normal.y > Normal.z)
         Axis = AXIS_Y;
    
    [unroll]
    for (int j = 0; j < 3; j++)
    {
        GeometryToFragment Output;

        Output.Position = float4(SwizzleAxis(i[j].Position.xyz, Axis), 1); 

        #ifdef UNITY_UV_STARTS_AT_TOP
        Output.Position.y = -Output.Position.y;
        #endif
        
        #ifdef UNITY_REVERSED_Z
        Output.Position.z = mad(Output.Position.z, 0.5, 0.5);
        #endif
        
        Output.TextureCoord0 = i[j].TextureCoord0;
        #if EVALUATE_LAYERED_LIT
        Output.TextureCoord1 = i[j].TextureCoord1;
        #endif
        
        Output.Axis = Axis;
        
        Stream.Append(Output);
    }
}

// --- Fragment Stage ---
float VoxelizationFrag(GeometryToFragment Input) : SV_TARGET
{
    float VoxelRes = _VoxelResolution.x;

    #ifndef PARTIAL_VOXELIZATION
    VoxelRes = _VoxelResolution.x * 2;
    #endif
    
    float3 VoxelPos = float3(Input.Position.x, Input.Position.y, Input.Position.z * VoxelRes);
    VoxelPos = RestoreAxis(VoxelPos, Input.Axis);
    
    // Modify Axes for non-cubic bounds
    VoxelPos.xyz = VoxelPos.xzy;
    VoxelPos.y *= (_VoxelBounds.z / _VoxelBounds.y);
    VoxelPos.xz = VoxelRes - VoxelPos.xz;

    // Calculate octants for the first 8 bits
    uint3 VoxelPosInt = floor(VoxelPos);
    uint BitShift = (1 * (VoxelPosInt.x % 2)) + (2 * (VoxelPosInt.y % 2)) + (4 * (VoxelPosInt.z % 2));
    uint OctantBits = (1 << BitShift) << 24;
    int StaticBitFlag = 1 << 23;

    int3 VoxelPosRounded = floor(VoxelPos / 2);
    
    #ifdef PARTIAL_VOXELIZATION
    //Offset by axis
    VoxelPosRounded.xyz += _AxisOffset.xyz; 

    // Culling trim
    if (VoxelPosRounded[_OffsetAxisIndex] < _CullingTrim.x || VoxelPosRounded[_OffsetAxisIndex] > _CullingTrim.y)
    return 0.0f;
    
    //Offset by octant
    VoxelPosRounded.xyz += _OctantOffset.xyz;
    #endif

    float2 TextureCoord0 = Input.TextureCoord0;
    float2 TextureCoord1 = Input.TextureCoord0;
    #if EVALUATE_LAYERED_LIT
    TextureCoord1 = Input.TextureCoord1;
    #endif
    
    // Evaluate all material attributes
    VoxelSurfaceData SurfaceData;
    EvaluateSurfaceColor(TextureCoord0, TextureCoord1, SurfaceData);

    if (!SurfaceData.IsEmissive)
        SurfaceData.Color = ClampDiffuseColor(SurfaceData.Color);
            
    if (SurfaceData.Alpha == 1)
        OctantBits = 0;
    
    // Pack color for the last 24 bits
    uint PackedColor = PackVoxelColor(SurfaceData.Color, SurfaceData.IsEmissive);

    #ifdef DYNAMIC_VOXELIZATION
    uint OriginalValue;
    InterlockedCompareExchange(_VoxelColor[VoxelPosRounded], 0, 0, OriginalValue);
    
    if (((OriginalValue >> 23) & 0x1) != 1)
    {
        InterlockedOr(_VoxelColor[VoxelPosRounded], OctantBits, OriginalValue);  
        InterlockedMax(_VoxelColor[VoxelPosRounded], PackedColor | (OriginalValue & 0xFF000000) | OctantBits );
    }
    #else
    uint OriginalValue;
    InterlockedOr(_VoxelColor[VoxelPosRounded],  StaticBitFlag | OctantBits, OriginalValue);
    InterlockedMax(_VoxelColor[VoxelPosRounded], StaticBitFlag | PackedColor | (OriginalValue & 0xFF000000) | OctantBits);
    #endif
    
    return 0.0f;
}
