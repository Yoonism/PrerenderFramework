Shader "HTrace/VoxelVisualization"
{
    HLSLINCLUDE

    #pragma vertex FullScreenVert
    #pragma fragment FullScreenFrag

    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"
    // #include "VoxelTraversal.hlsl"
 
    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "Custom Pass 0"

            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            

            HLSLPROGRAM

            float4x4 _DebugCameraFrustum;
            float4 _DebugCameraFrustumArray[8];

            struct FullScreenAttributes
            {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                uint vertexID : SV_VertexID;
            };

            struct FullScreenVaryings
            {
                UNITY_VERTEX_OUTPUT_STEREO
                float4 positionCS : SV_POSITION;
                float3 ray : TEXCOORD0;
            };

            // From here: https://hventura.com/unity-post-process-v2-raymarching.html
            FullScreenVaryings FullScreenVert(Attributes input)
            {
                FullScreenVaryings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                float4 PositionCS = GetFullScreenTriangleVertexPosition(input.vertexID, UNITY_RAW_FAR_CLIP_VALUE);
                output.positionCS = PositionCS;
                
                PositionCS = PositionCS * 0.5 + 0.5;
                int index = (PositionCS.x / 2.0f) + PositionCS.y;
                
                
                output.ray = (_DebugCameraFrustum[index].xyz);
              // output.ray = ( _DebugCameraFrustumArray[0 * 4 + vertexID].xyz);
                return output;
            }
            

            float4 FullScreenFrag(FullScreenVaryings varyings) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
                return float4(varyings.ray, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
