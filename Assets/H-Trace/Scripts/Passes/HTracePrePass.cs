using H_Trace.Scripts.Globals;
using H_Trace.Scripts.Structs;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace H_Trace.Scripts.Passes
{
	internal class HTracePrePass : CustomPass
	{
		private static readonly int g_HTraceStencilBuffer = Shader.PropertyToID("_HTraceStencilBuffer");
		private static readonly int g_OnlyForDebugDemoBuffer = Shader.PropertyToID("_OnlyForDebugDemoBuffer"); //TODO: release delete
		
		RTHandle         HTraceStencilBuffer;
		RTHandle OnlyForDebugDemoBuffer; //TODO: release delete
		private Material _testMaterial; //TODO: release delete
		private bool     _initialized = false;

		private VoxelizationRuntimeData _voxelizationRuntimeData;

		public void Initialize(VoxelizationRuntimeData voxelizationRuntimeData)
		{
			enabled = true;

			_voxelizationRuntimeData = voxelizationRuntimeData;
			_initialized             = true;
		}

		private void AllocateBuffers(bool onlyRelease = false)
		{
			void ReleaseTextures()
			{
				HExtensions.HRelease(HTraceStencilBuffer);
				HExtensions.HRelease(OnlyForDebugDemoBuffer); //TODO: release delete
			}
            
			if (onlyRelease)
			{
				ReleaseTextures();
				return;
			}

			ReleaseTextures();
			
			if (Application.isPlaying == false)
				TextureXR.maxViews = 1;
			
			// HDRenderPipelineAsset currentAsset = HExtensions.currentAsset;
			// var format = currentAsset.currentPlatformRenderPipelineSettings.colorBufferFormat == RenderPipelineSettings.ColorBufferFormat.R11G11B10 ? 
			// 	GraphicsFormat.B10G11R11_UFloatPack32 : GraphicsFormat.R16G16B16A16_SFloat;
			
			HTraceStencilBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, DepthBits.Depth32, dimension: TextureXR.dimension,
				colorFormat: GraphicsFormat.R32_SFloat, name: "_HTraceStencilBuffer", useDynamicScale: true);

			OnlyForDebugDemoBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension, //TODO: release delete
				colorFormat: GraphicsFormat.B10G11R11_UFloatPack32, name: "_OnlyForDebugDemoBuffer", useDynamicScale: true, enableRandomWrite: true); //TODO: release delete
		}

		protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
		{
			name = HTraceNames.HTRACE_PRE_PASS_NAME_FRAME_DEBUG;

			AllocateBuffers();
			//_testMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Custom/TestShader")); //TODO: release delete

			if (_voxelizationRuntimeData != null)
				_voxelizationRuntimeData.FullVoxelization = true;
		}

		protected override void Execute(CustomPassContext ctx)
		{
			if (_initialized == false)
				return;
			
			_voxelizationRuntimeData.FrameCount += 1;
			// Copying stencil moving object bit before it's overwritten by Unity. Needed for denoising (for both patched and unpatched versions).
			using (new ProfilingScope(ctx.cmd, new ProfilingSampler("Copying stencil moving object")))
			{
				if (ctx.cameraDepthBuffer.rt.volumeDepth == TextureXR.slices)
					ctx.cmd.CopyTexture(ctx.cameraDepthBuffer, HTraceStencilBuffer);
				ctx.cmd.SetGlobalTexture(g_HTraceStencilBuffer, HTraceStencilBuffer, RenderTextureSubElement.Stencil);
				ctx.cmd.SetGlobalTexture(g_OnlyForDebugDemoBuffer, OnlyForDebugDemoBuffer); //TODO: release delete
				//CoreUtils.DrawFullScreen(ctx.cmd, _testMaterial, shaderPassId: 2); //TODO: release delete
			}
		}

		internal void Release()
		{
			AllocateBuffers(true);
		}
	}
}
