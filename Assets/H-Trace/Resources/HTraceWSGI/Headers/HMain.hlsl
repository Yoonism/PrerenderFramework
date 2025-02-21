#pragma once
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Builtin/BuiltinData.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/TextureXR.hlsl"
//#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightDefinition.cs.hlsl"
//#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariablesGlobal.cs.hlsl"
//#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"


//------------------------ TEXTURE HELPERS

#define H_COORD(pixelCoord)                                             COORD_TEXTURE2D_X(pixelCoord)
#define H_INDEX_ARRAY(slot)                                             INDEX_TEXTURE2D_ARRAY_X(slot)
#define H_TILE_SIZE                                                     TILE_SIZE_FPTL

//------------------------ TEXTURE PROPERTY DEFINES

#define H_SAMPLER                                                           SAMPLER
#define H_TEXTURE                                                           TEXTURE2D_X
#define H_TEXTURE_2D(textureName)                                           TEXTURE2D(textureName)
#define H_TEXTURE_ARRAY(textureName)                                        TEXTURE2D_ARRAY(textureName)
#define H_TEXTURE_DX(type, textureName)                                     Texture2D<type> textureName
#define H_RW_TEXTURE(type, textureName)                                     RW_TEXTURE2D_X(type, textureName)
#define H_RW_TEXTURE_ARRAY(type, textureName)                               RW_TEXTURE2D_ARRAY(type, textureName)
#define H_TEXTURE_UINT2(textureName)                                        TEXTURE2D_X_UINT2(textureName)
#define H_RW_TEXTURE_UINT2(textureName)                                     RW_TEXTURE2D_X_UINT2(textureName)

#define H_TEXTURE3D(type, textureName)                                      Texture3D<type> textureName
#define H_RW_TEXTURE3D(type, textureName)                                   RWTexture3D<type> textureName

//------------------------ TEXTURE LOAD / SAMPLE DEFINES

#define H_LOAD(textureName, unCoord2)                                           LOAD_TEXTURE2D_X(textureName, unCoord2)
#define H_LOAD_LOD(textureName, unCoord2, lod)                                  LOAD_TEXTURE2D_X_LOD(textureName, unCoord2, lod)
#define H_LOAD_ARRAY(textureName, unCoord2, index)                              LOAD_TEXTURE2D_ARRAY(textureName, unCoord2, H_INDEX_ARRAY(index))
#define H_LOAD_ARRAY_LOD(textureName, unCoord2, index, lod)                     LOAD_TEXTURE2D_ARRAY_LOD(textureName, unCoord2, H_INDEX_ARRAY(index), lod)

#define H_SAMPLE_2D(textureName, samplerName, coord2)                           SAMPLE_TEXTURE2D(textureName, samplerName, coord2)

#define H_SAMPLE(textureName, samplerName, coord2)                              SAMPLE_TEXTURE2D_X(textureName, samplerName, coord2)
#define H_SAMPLE_LOD(textureName, samplerName, coord2, lod)                     SAMPLE_TEXTURE2D_X_LOD(textureName, samplerName, coord2, lod)
#define H_SAMPLE_ARRAY(textureName, samplerName, coord2, index)                 SAMPLE_TEXTURE2D_ARRAY(textureName, samplerName, coord2, index)
#define H_SAMPLE_ARRAY_LOD(textureName, samplerName, coord2, index, lod)        SAMPLE_TEXTURE2D_ARRAY_LOD(textureName, samplerName, coord2, index, lod)


#define H_GATHER_RED(textureName, samplerName, coord2, offset)                  textureName.GatherRed(samplerName, float3(coord2, H_INDEX_ARRAY(0)), offset)
#define H_GATHER_BLUE(textureName, samplerName, coord2, offset)                 textureName.GatherBlue(samplerName, float3(coord2, H_INDEX_ARRAY(0)), offset)
#define H_GATHER_GREEN(textureName, samplerName, coord2, offset)                textureName.GatherGreen(samplerName, float3(coord2, H_INDEX_ARRAY(0)), offset)
#define H_GATHER_ALPHA(textureName, samplerName, coord2, offset)                textureName.GatherAlpha(samplerName, float3(coord2, H_INDEX_ARRAY(0)), offset)

#define H_LOAD3D(textureName, unCoord3)                                         LOAD_TEXTURE3D(textureName, unCoord3)
#define H_LOAD3D_LOD(textureName, unCoord3, lod)                                LOAD_TEXTURE3D_LOD(textureName, unCoord3, lod)

//------------------------ TEXTURE WRITES


//------------------------ BUFFER DEFINES

#define HBUFFER_NORMAL_WS(pixCoordWS)                               GetNormalWS(pixCoordWS)
#define HBUFFER_ROUGHNESS(pixCoord)                                 GetRoughness(pixCoord)
#define HBUFFER_DEPTH(pixCoord)                                     GetDepth(pixCoord)
#define HBUFFER_COLOR(pixCoord)                                     GetColor(pixCoord)
#define HBUFFER_DIFFUSE(pixCoord)                                   GetDiffuse(pixCoord)
#define HBUFFER_MOTION_VECTOR(pixCoord)                             GetMotionVector(pixCoord)
#define HBUFFER_GEOMETRICAL_NORMAL_FROM_DEPTH(pixCoord)             GeometricalNormalFromDepth(pixCoord)

//------------------------ SAMPLERS DEFINES

#define H_SAMPLER_POINT_CLAMP                     s_point_clamp_sampler
#define H_SAMPLER_LINEAR_CLAMP                    s_linear_clamp_sampler
#define H_SAMPLER_LINEAR_REPEAT                   s_linear_repeat_sampler
#define H_SAMPLER_TRILINEAR_CLAMP                 s_trilinear_clamp_sampler
#define H_SAMPLER_TRILINEAR_REPEAT                s_trilinear_repeat_sampler
#define H_SAMPLER_LINEAR_CLAMP_COMPARE            s_linear_clamp_compare_sampler

//------------------------ OTHER DEFINES

#define UNITY_MATRIX_PREV_I_VP_H                  UNITY_MATRIX_PREV_I_VP

//------------------------ METHODS

H_TEXTURE(_GBufferTexture0);
H_TEXTURE(_CustomMotionVectors);

float3 GetNormalWS(uint2 pixCoordWS)
{
    NormalData normalData;
    DecodeFromNormalBuffer(pixCoordWS, normalData);
    return normalData.normalWS;
}

float GetRoughness(uint2 pixCoord)
{   
    NormalData normalData;
    DecodeFromNormalBuffer(pixCoord, normalData);
    return normalData.perceptualRoughness;
}

float GetDepth(uint2 pixCoord)
{
    return LoadCameraDepth(pixCoord);
}

float4 GetColor(uint2 pixCoord)
{
    return LOAD_TEXTURE2D_X(_ColorPyramidTexture, pixCoord);
}

float4 GetDiffuse(uint2 pixCoord)
{
    return LOAD_TEXTURE2D_X(_GBufferTexture0, pixCoord);
}

float2 GetMotionVector(uint2 pixCoord)
{
    float2 MotionVectors;
    DecodeMotionVector(LOAD_TEXTURE2D_X(_CustomMotionVectors, pixCoord), MotionVectors);
   
    return MotionVectors;
}

float3 GeometricalNormalFromDepth(float2 pixCoord)
{
	// Option 1: Protect borders by dilation
	// if (pixCoord.x == 0) pixCoord.x += 2;
	// if (pixCoord.y == 0) pixCoord.y += 2;

	// Option 2: Protect borders by culling out-of-frame samples
	float CullX = 1;
	float CullY = 1;
	if (pixCoord.x == 0) CullX = 0;
	if (pixCoord.y == 0) CullY = 0; 

	// TODO: find out why do we need to protect borders < 0 (Left, Bottom) while going above _ScreenSize is okay (Top, Right)
	
    float DepthC = HBUFFER_DEPTH(pixCoord);

    // Early-out on the sky
    if (DepthC <= 1e-7)
        return 0;
	
    float DepthL = HBUFFER_DEPTH(pixCoord + int2(-1,  0)) * CullX;
    float DepthR = HBUFFER_DEPTH(pixCoord + int2( 1,  0));
    float DepthD = HBUFFER_DEPTH(pixCoord + int2( 0, -1)) * CullY;
    float DepthU = HBUFFER_DEPTH(pixCoord + int2( 0,  1));
    
    float3 WorldPosC = ComputeWorldSpacePosition((pixCoord + 0.5 + float2( 0.0,  0.0)) * _ScreenSize.zw, DepthC, UNITY_MATRIX_I_VP);
    float3 WorldPosL = ComputeWorldSpacePosition((pixCoord + 0.5 + float2(-1.0,  0.0)) * _ScreenSize.zw, DepthL, UNITY_MATRIX_I_VP) * CullX;
    float3 WorldPosR = ComputeWorldSpacePosition((pixCoord + 0.5 + float2( 1.0,  0.0)) * _ScreenSize.zw, DepthR, UNITY_MATRIX_I_VP);
    float3 WorldPosD = ComputeWorldSpacePosition((pixCoord + 0.5 + float2( 0.0, -1.0)) * _ScreenSize.zw, DepthD, UNITY_MATRIX_I_VP) * CullY;
    float3 WorldPosU = ComputeWorldSpacePosition((pixCoord + 0.5 + float2( 0.0,  1.0)) * _ScreenSize.zw, DepthU, UNITY_MATRIX_I_VP);

    float3 L = WorldPosC - WorldPosL;
    float3 R = WorldPosR - WorldPosC;
    float3 D = WorldPosC - WorldPosD;
    float3 U = WorldPosU - WorldPosC;
    
    float4 H = float4(HBUFFER_DEPTH(pixCoord + int2(-1, 0)) * CullX,
                      HBUFFER_DEPTH(pixCoord + int2( 1, 0)),
                      HBUFFER_DEPTH(pixCoord + int2(-2, 0)) * CullX,
                      HBUFFER_DEPTH(pixCoord + int2( 2, 0)));

    float4 V = float4(HBUFFER_DEPTH(pixCoord + int2(0, -1)) * CullY,
                      HBUFFER_DEPTH(pixCoord + int2(0,  1)),
                      HBUFFER_DEPTH(pixCoord + int2(0, -2)) * CullY,
                      HBUFFER_DEPTH(pixCoord + int2(0,  2)));
    
    float2 HE = abs((2 * H.xy - H.zw) - DepthC);
    float2 VE = abs((2 * V.xy - V.zw) - DepthC);
    
    half3 DerivH = HE.x < HE.y ? L : R;
    half3 DerivV = VE.x < VE.y ? D : U;

    return -normalize(cross(DerivH, DerivV));
}