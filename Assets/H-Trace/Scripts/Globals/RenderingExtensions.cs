using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace H_Trace.Scripts.Globals
{
	public static class RenderingExtensions
	{
        public static void RenderVoxels(in CustomPassContext ctx, Camera VoxelizationCamera, RenderTexture RenderTarget, LayerMask LayerMask,
                                        Shader OverriderShader = null, Material OverrideMaterial = null, int ShaderPass = 0)
        {
            CoreUtils.SetRenderTarget(ctx.cmd, RenderTarget.colorBuffer, RenderTarget.depthBuffer, ClearFlag.All);

            float AspectRatio = RenderTarget.width / (float)RenderTarget.height;

            using (new CustomPassUtils.DisableSinglePassRendering(ctx))
            {
                using (new CustomPassUtils.OverrideCameraRendering(ctx, VoxelizationCamera, AspectRatio))
                {
                    using (new ProfilingScope(ctx.cmd, new ProfilingSampler("Render Voxels")))
                    {
                        ShaderTagId[] VoxelizationTags = {new(HDShaderPassNames.s_MetaStr), HDShaderPassNames.s_GBufferName};

                        var RenderList = new UnityEngine.Rendering.RendererUtils.RendererListDesc(VoxelizationTags, ctx.cullingResults, ctx.hdCamera.camera)
                        {   
                            rendererConfiguration = PerObjectData.None,
                            renderQueueRange = CustomPassUtils.GetRenderQueueRangeFromRenderQueueType(CustomPass.RenderQueueType.AllOpaque),
                            sortingCriteria = SortingCriteria.OptimizeStateChanges,
                            layerMask = LayerMask,
                            overrideShader = OverriderShader,
                            overrideMaterial = OverrideMaterial,
                            overrideShaderPassIndex = ShaderPass,
                        };
                        
                        CoreUtils.DrawRendererList(ctx.renderContext, ctx.cmd, ctx.renderContext.CreateRendererList(RenderList));
                        
                        var RenderListProcedural = new UnityEngine.Rendering.RendererUtils.RendererListDesc(new []{new ShaderTagId(HTraceNames.HTRACE_VOXELIZATION_SHADER_TAG_ID)}, ctx.cullingResults, ctx.hdCamera.camera)
                        {
                            rendererConfiguration = PerObjectData.None,
                            renderQueueRange = CustomPassUtils.GetRenderQueueRangeFromRenderQueueType(CustomPass.RenderQueueType.AllOpaque),
                            sortingCriteria = SortingCriteria.OptimizeStateChanges,
                            overrideShader = null,
                            overrideMaterial = null,
                            layerMask = LayerMask,
                        };

                        CoreUtils.DrawRendererList(ctx.renderContext, ctx.cmd, ctx.renderContext.CreateRendererList(RenderListProcedural));
                    }
                }
            }
        }
        


        public static void RenderShadowmap(in CustomPassContext ctx, Camera VoxelizationCamera, RenderTexture ColorTarget, RenderTexture DepthTarget, LayerMask LayerMask,
                                            Shader OverriderShader = null, Material OverrideMaterial = null, int ShaderPass = 0, ClearFlag ClearFlag = ClearFlag.None, bool UseShadowCasterPass = false)
        {   
            if (ClearFlag != ClearFlag.None)
                CoreUtils.SetRenderTarget(ctx.cmd, ColorTarget, DepthTarget, ClearFlag);

            float AspectRatio = ColorTarget.width / (float)ColorTarget.height;

            using (new CustomPassUtils.DisableSinglePassRendering(ctx))
            {
                using (new CustomPassUtils.OverrideCameraRendering(ctx, VoxelizationCamera, AspectRatio))
                {
                    using (new ProfilingScope(ctx.cmd, new ProfilingSampler("Render Shadowmap")))
                    {
                        UseShadowCasterPass = true;
                        
                        ShaderTagId[] ShaderForwardTags = {HDShaderPassNames.s_ForwardOnlyName, HDShaderPassNames.s_ForwardName, HDShaderPassNames.s_SRPDefaultUnlitName};
                        if (UseShadowCasterPass) ShaderForwardTags = new ShaderTagId[] {new ShaderTagId("ShadowCaster")};
                        
                        var RenderList = new UnityEngine.Rendering.RendererUtils.RendererListDesc(ShaderForwardTags, ctx.cullingResults, ctx.hdCamera.camera)
                        {
                            rendererConfiguration = PerObjectData.None,
                            renderQueueRange = CustomPassUtils.GetRenderQueueRangeFromRenderQueueType(CustomPass.RenderQueueType.AllOpaque),
                            sortingCriteria = SortingCriteria.OptimizeStateChanges,
                            overrideShader = null,
                            overrideMaterial = null,
                            layerMask = LayerMask,
                        };

                        if (UseShadowCasterPass) CoreUtils.DrawRendererList(ctx.renderContext, ctx.cmd, ctx.renderContext.CreateRendererList(RenderList));
                        else CustomPassUtils.DrawRenderers(ctx, LayerMask, CustomPass.RenderQueueType.AllOpaque, OverrideMaterial, ShaderPass);
                    }
                }
            }
        }
    }
}
