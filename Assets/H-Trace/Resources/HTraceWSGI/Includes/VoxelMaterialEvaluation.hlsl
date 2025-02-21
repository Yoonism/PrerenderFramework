#pragma once
#include "../Includes/Config.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassRenderers.hlsl"


// ------------------------ STRUCTS ------------------------
struct VoxelSurfaceData
{
	float3 Color;
	float Alpha;
	float IsEmissive;
};

// ------------------------ FUNCTIONS ------------------------
float3 ClampDiffuseColor(float3 DiffuseColor)
{
	DiffuseColor *= SURFACE_DIFFUSE_INTENSITY;
	
	DiffuseColor = FastLinearToSRGB(DiffuseColor);
	DiffuseColor = RgbToHsv(DiffuseColor);
	DiffuseColor.z = min(DiffuseColor.z, 0.9f);
	DiffuseColor = HsvToRgb(DiffuseColor);
	DiffuseColor = FastSRGBToLinear(DiffuseColor);

	return DiffuseColor;
}


float3 TerrainSplatColors[4];

#define FillSplatColors(i, Mask, TextureCoordBase)                                 		\
UNITY_BRANCH																			\
if (Mask > 0)																			\
{																						\
	float2 TextureCoord = TextureCoordBase * _Splat##i##_ST.xy + _Splat##i##_ST.zw;		\
	TerrainSplatColors[i] = H_SAMPLE_2D(_Splat##i, sampler_Splat0, TextureCoord).xyz;	\
}																						\
else																					\
{																						\
	TerrainSplatColors[i] = float3(0, 0, 0);											\
}


// ------------------------ MATERIAL PROPERTIES ------------------------

// Emissive properties
float _AlbedoAffectEmissive;

// Lit Shader properties
float _UVBase;
float4 _BaseColor;
float4 _BaseColorMap_ST;
TEXTURE2D(_BaseColorMap);
H_SAMPLER(sampler_BaseColorMap);

// Layered Lit Shader properties
float _UVBase0;
float _UVBase1;
float _UVBlendMask;
float4 _BaseColor0;
float4 _BaseColor1;
float4 _BaseColorMap0_ST;
float4 _BaseColorMap1_ST;
float4 _LayerMaskMap_ST;
TEXTURE2D(_BaseColorMap0);
TEXTURE2D(_BaseColorMap1);
TEXTURE2D(_LayerMaskMap);
H_SAMPLER(sampler_BaseColorMap0);
H_SAMPLER(sampler_BaseColorMap1);
H_SAMPLER(sampler_LayerMaskMap);

// Terrain Shader properties
float4 _Splat0_ST;
float4 _Splat1_ST;
float4 _Splat2_ST;
float4 _Splat3_ST;
float4 _Control0_TexelSize;
TEXTURE2D(_Splat0);
TEXTURE2D(_Splat1);
TEXTURE2D(_Splat2);
TEXTURE2D(_Splat3);
TEXTURE2D(_Control0);
TEXTURE2D(_TerrainHolesTexture);
H_SAMPLER(sampler_Control0);
H_SAMPLER(sampler_Splat0);
H_SAMPLER(sampler_TerrainHolesTexture);

// SpeedTree Shader properties
float4 _Color;
float _AlphaClipThreshold;
TEXTURE2D(_MainTex);


// ------------------------ MATERIAL EVALUATION ------------------------
bool EvaluateSurfaceColor(float2 TexCoord0, float2 TexCoord1, inout VoxelSurfaceData SurfaceData)
{
	float3 DiffuseColor = 0;

	SurfaceData.Color = 0;
	SurfaceData.Alpha = 0;
	SurfaceData.IsEmissive = 0;

	if (EVALUATE_EMISSION)
	{
		float2 TextureCoord = TexCoord0 * _EmissiveColorMap_ST.xy + _EmissiveColorMap_ST.zw;
		float3 EmissiveMap = H_SAMPLE_2D(_EmissiveColorMap, sampler_EmissiveColorMap, TextureCoord).xyz; 
		
		float3 Emission = _EmissiveColor * lerp(float3(1.0, 1.0, 1.0), DiffuseColor, _AlbedoAffectEmissive);
		Emission = lerp(Emission, Emission * GetCurrentExposureMultiplier(), _EmissiveExposureWeight);
		Emission *= EmissiveMap;
		
		if (Luminance(Emission) > 0)
		{
			SurfaceData.Color = Emission;
			SurfaceData.IsEmissive = 1;

			// This is needed to match SS part which uses _GBufferTexture3 for Emissive which is R111G11B10 format
			uint Packed = PackToR11G11B10f(SurfaceData.Color);
			SurfaceData.Color = UnpackFromR11G11B10f(Packed);

			return true;
		}
	}
	
	if (EVALUATE_LIT)
	{
		float2 TextureCoordBase = _UVBase == 1 ? TexCoord1 : TexCoord0;
		float2 TextureCoord = TextureCoordBase * _BaseColorMap_ST.xy + _BaseColorMap_ST.zw;
		float4 LitColor = H_SAMPLE_2D(_BaseColorMap, sampler_BaseColorMap, TextureCoord) * _BaseColor;
		SurfaceData.Alpha = LitColor.w < _AlphaCutoff ? 1 : 0;

		DiffuseColor += LitColor.xyz;
		SurfaceData.Color = LitColor.xyz;
	}
	
	if (EVALUATE_LAYERED_LIT)
	{
		UNITY_BRANCH
		if (length(DiffuseColor) > 0)
			return true;
		
		float2 TextureCoordBase0 = _UVBase0 == 1 ? TexCoord1 : TexCoord0;
		float2 TextureCoordBase1 = _UVBase1 == 1 ? TexCoord1 : TexCoord0;
		float2 TextureCoordBaseMask = _UVBlendMask == 1 ? TexCoord1 : TexCoord0;
		
		float2 TextureCoord0 = TextureCoordBase0 * _BaseColorMap0_ST.xy + _BaseColorMap0_ST.zw;
		float2 TextureCoord1 = TextureCoordBase1 * _BaseColorMap1_ST.xy + _BaseColorMap1_ST.zw;
		float2 TextureCoordMask = TextureCoordBaseMask * _LayerMaskMap_ST.xy + _LayerMaskMap_ST.zw;
		
		float Mask = H_SAMPLE_2D(_LayerMaskMap, sampler_LayerMaskMap, TextureCoordMask).x;
		float4 LayerColor0 = H_SAMPLE_2D(_BaseColorMap0, sampler_BaseColorMap0, TextureCoord0) * _BaseColor0;
		float4 LayerColor1 = H_SAMPLE_2D(_BaseColorMap1, sampler_BaseColorMap1, TextureCoord1) * _BaseColor1;
		
		float4 LayeredLitColor = lerp(LayerColor0, LayerColor1, Mask.r);
		
		DiffuseColor += LayeredLitColor.xyz;
		SurfaceData.Color = LayeredLitColor.xyz;
		SurfaceData.Alpha = LayeredLitColor.w < _AlphaCutoff ? 1 : 0;
	}
	
	if (EVALUATE_UNLIT)
	{
		UNITY_BRANCH
		if (length(DiffuseColor) > 0)
			return true;
		
		float2 TextureCoord = TexCoord0 * _UnlitColorMap_ST.xy + _UnlitColorMap_ST.zw;
		float4 UnlitColor = H_SAMPLE_2D(_UnlitColorMap, sampler_UnlitColorMap, TextureCoord) * _UnlitColor;
		
		DiffuseColor += UnlitColor.xyz;
		SurfaceData.Color = UnlitColor.xyz;
		SurfaceData.Alpha = UnlitColor.w < _AlphaCutoff ? 1 : 0;
	}
	
	if (EVALUATE_SPEEDTREE)
	{
		UNITY_BRANCH
		if (length(DiffuseColor) > 0)
			return true;
		
		float4 SpeedTreeColor = H_SAMPLE_2D(_MainTex, sampler_BaseColorMap, TexCoord0) * _Color;
		SurfaceData.Alpha = SpeedTreeColor.w < _AlphaClipThreshold ? 1 : 0;

		float Holes = H_SAMPLE_2D(_TerrainHolesTexture, sampler_TerrainHolesTexture, TexCoord0).r;
		if (abs(Holes.x - 0.5) == 0.5)
			SurfaceData.Alpha = Holes < 0.5f;

		DiffuseColor += SpeedTreeColor.xyz;
		SurfaceData.Color = SpeedTreeColor.xyz;
	}

	if (EVALUATE_TERRAIN)
	{
		UNITY_BRANCH
		if (length(DiffuseColor) > 0)
			return true;
	
		float2 TextureCoordBase = TexCoord0;
		float2 ControlMapCoord = (TexCoord0.xy * (_Control0_TexelSize.zw - 1.0f) + 0.5f) * _Control0_TexelSize.xy;
		float4 ControlMap = H_SAMPLE_2D(_Control0, sampler_Control0, ControlMapCoord);
		float Holes = H_SAMPLE_2D(_TerrainHolesTexture, sampler_TerrainHolesTexture, ControlMapCoord).r;
	
		FillSplatColors(0, ControlMap.x, TextureCoordBase);
		FillSplatColors(1, ControlMap.y, TextureCoordBase);
		FillSplatColors(2, ControlMap.z, TextureCoordBase);
		FillSplatColors(3, ControlMap.w, TextureCoordBase);
	
		float3 TerrainColor = 0;
		TerrainColor += TerrainSplatColors[0] * ControlMap.x;
		TerrainColor += TerrainSplatColors[1] * ControlMap.y;
		TerrainColor += TerrainSplatColors[2] * ControlMap.z;
		TerrainColor += TerrainSplatColors[3] * ControlMap.w;
	
		DiffuseColor += TerrainColor;
		SurfaceData.Color = TerrainColor;
		SurfaceData.Alpha = Holes < 0.5f;
	}
	
	return true;
}