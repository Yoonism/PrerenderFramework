Shader "HTrace/Voxelization"
{   
    SubShader
    {
        Pass
        {   
            Name "HTrace Voxelization"
            Cull Off
            ZClip Off
            ZWrite Off
            Conservative False

            HLSLPROGRAM

            #pragma require geometry
            #pragma require randomwrite
            
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma multi_compile CONSTANT_VOXELIZATION PARTIAL_VOXELIZATION DYNAMIC_VOXELIZATION
            
            #include "../Includes/VoxelizationStages.hlsl"

            #pragma vertex VoxelizationVert
            #pragma geometry VoxelizationGeom
            #pragma fragment VoxelizationFrag
            
            ENDHLSL
        }
    }

    Fallback Off
}