#include "config.hlsl"
#include "VoxelizationCommon.hlsl"
#include "../Headers/HPacking.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/HDShadow.hlsl"

#pragma warning (disable : 3206)

H_TEXTURE_DX(float, _HTraceShadowmap);

float4x4 _DirLightMatrix;

float EvaluateDirectionalShadowOcclusion(float3 WorldPos)
{
  if (!EVALUATE_SKY_OCCLUSION)
    return 1;
  
  // Calculate shadowmap coordinates
  float4 PosCS = mul(_DirLightMatrix, float4(WorldPos, 1.0));
  float3 PosTC = float3(saturate(PosCS.xy * 0.5f + 0.5f), PosCS.z);

  if (any(PosTC < 0.0f) || any(PosTC > 1.0f))
    return 1.0f;

  float SkyOcclusion = SAMPLE_TEXTURE2D_SHADOW(_HTraceShadowmap, s_linear_clamp_compare_sampler, PosTC).x;
  return lerp(SkyOcclusion, 1.0f,  MINIMAL_SKY_LIGHTING);
}


float EvaluateDirectionalShadow(DirectionalLightData DirLight, float3 WorldPos, float3 Normal)
{
  WorldPos = GetCameraRelativePositionWS(WorldPos);
  
  // Initialize shadow to 1
  float DirectionalShadow = 1.0f;
  ApplyCameraRelativeXR(WorldPos); //TODO: do we need this for voxel hits?

  // Normal *= FastSign(dot(Normal, -DirLight.forward)); //TODO: do we need this?

  // Detect if we can early out on zero dot product
  float NdotL = saturate(dot(Normal, -DirLight.forward));
  if (NdotL == 0)
    return 0;
	
  // Calculate normal bias
  float WorldTexelSize = 0.025f; // ~for 2048 shadowmap;
  float NormalBias = 1.5f; //_VoxelSize * 10.0f * 3.5f; // 1.5f;
  WorldPos += Normal * NormalBias * WorldTexelSize * lerp(0.35, 1, NdotL);

  // Calculate shadowmap coordinates
  float4 PosCS = mul(_DirLightMatrix, float4(GetAbsolutePositionWS(WorldPos), 1.0));
  float3 PosTC = float3(saturate(PosCS.xy * 0.5f + 0.5f), PosCS.z);
  PosTC.z += FIXED_UNIFORM_BIAS;

  // Apply shadow
  DirectionalShadow *= NdotL;
  DirectionalShadow *= SAMPLE_TEXTURE2D_SHADOW(_HTraceShadowmap, s_linear_clamp_compare_sampler, PosTC).x;
	
  return DirectionalShadow; 
}


bool EvaluateHitLighting(inout VoxelPayload Payload)
{
  bool IsEmissive = false;
  Payload.HitDiffuse = UnpackVoxelColor(asuint(H_LOAD3D(_VoxelData, Payload.HitCoord)), IsEmissive);
  
  Payload.HitColor = 0;
  float DirectionalLightShadow = 1;

  if (_DirectionalLightCount > 0)
  {
    DirectionalLightData DirectionalLightData = _DirectionalLightDatas[0];
    DirectionalLightShadow = EvaluateDirectionalShadow(DirectionalLightData, Payload.HitPosition, Payload.HitNormal);
    
    Payload.HitColor += Payload.HitDiffuse;
    Payload.HitColor *= DirectionalLightData.color * DIRECTIONAL_LIGHT_INTENSITY;
    Payload.HitColor *= DirectionalLightShadow;
    Payload.HitColor /= H_PI;
  }
  
  if (IsEmissive)
  {
    Payload.HitColor = Payload.HitDiffuse * GetInverseCurrentExposureMultiplier();
  }
  
  return DirectionalLightShadow == 0 ? false : true;
}

  