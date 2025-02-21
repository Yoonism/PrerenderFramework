Shader "HTrace/Shadowmap"
{
    SubShader
    {
        HLSLINCLUDE

        #pragma target 4.5
        #pragma multi_compile_instancing
        #pragma multi_compile _ DOTS_INSTANCING_ON

        ENDHLSL
        
        Pass
        {
            Name "SHADOWMAP_STATIC"

            Blend Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Frag
            
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassRenderers.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"

            PackedVaryingsType Vert(AttributesMesh inputMesh)
            {
                VaryingsType varyingsType;
                varyingsType.vmesh = VertMesh(inputMesh);
                return PackVaryingsType(varyingsType);
            }

            float Frag(PackedVaryingsType packedInput) : SV_Target
            {    
                return 0;
            }
            
            ENDHLSL
        }

        Pass
        {
            Name "SHADOWMAP_COPY"

            Blend Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex FullScreenVert
            #pragma fragment FullScreenFrag
            
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"
            #include "../Headers/HMain.hlsl"
            #include "../Headers/HMath.hlsl"

            H_TEXTURE_DX(float, _DirectionalShadowmapStatic);
            
            struct FullScreenAttributes
            {
                uint vertexID : SV_VertexID;
            };

            struct FullScreenVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            
            FullScreenVaryings FullScreenVert(Attributes input)
            {
                FullScreenVaryings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID, UNITY_RAW_FAR_CLIP_VALUE);
                
                return output;
            }
            

            float FullScreenFrag(FullScreenVaryings varyings, out float Depth_Output : SV_Depth) : SV_Target
            { 
                Depth_Output = H_LOAD(_DirectionalShadowmapStatic, varyings.positionCS.xy);
                return 0;
            }
            
            ENDHLSL
        }
    }
}