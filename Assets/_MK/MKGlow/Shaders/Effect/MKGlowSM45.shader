//////////////////////////////////////////////////////
// MK Glow Shader SM40 HDRP							//
//					                                //
// Created by Michael Kremmel                       //
// www.michaelkremmel.de                            //
// Copyright © 2020 All rights reserved.            //
//////////////////////////////////////////////////////
Shader "Hidden/MK/Glow/MKGlowSM45"
{
	HLSLINCLUDE
		#ifndef MK_RENDER_PIPELINE_HIGH_DEFINITION
			#define MK_RENDER_PIPELINE_HIGH_DEFINITION
		#endif
	ENDHLSL
	SubShader
	{
		Tags {"LightMode" = "Always" "RenderType"="Opaque" "PerformanceChecks"="False" "RenderPipeline" = "HDRenderPipeline"}
		Cull Off ZWrite Off ZTest Always

		/////////////////////////////////////////////////////////////////////////////////////////////
        // Presample - 0
        /////////////////////////////////////////////////////////////////////////////////////////////
		Pass
		{
			HLSLPROGRAM
			#pragma target 4.5
			#pragma vertex vertSimple
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest

			#pragma require mrt8 samplelod
			#pragma exclude_renderers nomrt

			#pragma multi_compile __ _MK_BLOOM
			#pragma multi_compile __ _MK_LENS_FLARE
			#pragma multi_compile __ _MK_GLARE_1 _MK_GLARE_2 _MK_GLARE_3 _MK_GLARE_4
			#pragma multi_compile __ _MK_RENDER_PRIORITY_BALANCED _MK_RENDER_PRIORITY_QUALITY
			#pragma multi_compile __ _MK_NATURAL
			#pragma multi_compile __ _MK_HQ_ANTI_FLICKER

			#define _HDRP

			#include_with_pragmas "../../Shaders/Inc/Presample.hlsl"
			ENDHLSL
		}

		/////////////////////////////////////////////////////////////////////////////////////////////
        // Downsample - 1
        /////////////////////////////////////////////////////////////////////////////////////////////
		Pass
		{
			HLSLPROGRAM
			#pragma target 4.5
			#pragma vertex vertSimple
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest

			#pragma require mrt8 samplelod
			#pragma exclude_renderers nomrt

			#pragma multi_compile __ _MK_BLOOM
			#pragma multi_compile __ _MK_LENS_FLARE
			#pragma multi_compile __ _MK_GLARE_1 _MK_GLARE_2 _MK_GLARE_3 _MK_GLARE_4
			#pragma multi_compile __ _MK_RENDER_PRIORITY_BALANCED _MK_RENDER_PRIORITY_QUALITY

			#define _HDRP

			#include_with_pragmas "../../Shaders/Inc/Downsample.hlsl"
			ENDHLSL
		}

		/////////////////////////////////////////////////////////////////////////////////////////////
        // Upsample - 2
        /////////////////////////////////////////////////////////////////////////////////////////////
		Pass
		{
			HLSLPROGRAM
			#pragma target 4.5
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest

			#pragma require mrt8 samplelod
			#pragma exclude_renderers nomrt

			#pragma multi_compile __ _MK_BLOOM
			#pragma multi_compile __ _MK_LENS_FLARE
			#pragma multi_compile __ _MK_GLARE_1 _MK_GLARE_2 _MK_GLARE_3 _MK_GLARE_4
			#pragma multi_compile __ _MK_RENDER_PRIORITY_BALANCED _MK_RENDER_PRIORITY_QUALITY

			#define _HDRP

			#include_with_pragmas "../../Shaders/Inc/Upsample.hlsl"
			ENDHLSL
		}

		/////////////////////////////////////////////////////////////////////////////////////////////
        // Composite - 3
        /////////////////////////////////////////////////////////////////////////////////////////////
		Pass
		{
			HLSLPROGRAM
			#pragma target 4.5
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest

			#pragma multi_compile __ _MK_LEGACY_BLIT

			#pragma require mrt8 samplelod
			#pragma exclude_renderers nomrt

			#pragma multi_compile __ _MK_LENS_SURFACE
			#pragma multi_compile __ _MK_LENS_FLARE
			#pragma multi_compile __ _MK_GLARE_1 _MK_GLARE_2 _MK_GLARE_3 _MK_GLARE_4
			#pragma multi_compile __ _MK_RENDER_PRIORITY_BALANCED _MK_RENDER_PRIORITY_QUALITY
			#pragma multi_compile __ _MK_NATURAL

			#define _HDRP

			#include_with_pragmas "../../Shaders/Inc/Composite.hlsl"
			ENDHLSL
		}

		/////////////////////////////////////////////////////////////////////////////////////////////
        // Debug - 4
        /////////////////////////////////////////////////////////////////////////////////////////////
		Pass
		{
			HLSLPROGRAM
			#pragma target 4.5
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest

			#pragma multi_compile __ _MK_LEGACY_BLIT
			
			#pragma require mrt8 samplelod
			#pragma exclude_renderers nomrt
			
			#pragma multi_compile __ _MK_DEBUG_RAW_BLOOM _MK_DEBUG_RAW_LENS_FLARE _MK_DEBUG_RAW_GLARE _MK_DEBUG_LENS_FLARE _MK_DEBUG_GLARE _MK_DEBUG_COMPOSITE
			#pragma multi_compile __ _MK_LENS_SURFACE
			#pragma multi_compile __ _MK_LENS_FLARE
			#pragma multi_compile __ _MK_GLARE_1 _MK_GLARE_2 _MK_GLARE_3 _MK_GLARE_4
			#pragma multi_compile __ _MK_RENDER_PRIORITY_BALANCED _MK_RENDER_PRIORITY_QUALITY
			#pragma multi_compile __ _MK_NATURAL
			#pragma multi_compile __ _MK_HQ_ANTI_FLICKER

			#define _HDRP
			
			#include_with_pragmas "../../Shaders/Inc/Debug.hlsl"
			ENDHLSL
		}
	}
	//HDRP Requires at least SM5
	FallBack Off
}
