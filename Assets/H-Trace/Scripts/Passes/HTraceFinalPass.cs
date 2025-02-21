using H_Trace.Scripts.Globals;
using H_Trace.Scripts.Structs;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace H_Trace.Scripts.Passes
{
	internal class HTraceFinalPass : CustomPass
	{
		private static readonly int _Debug_Output_Name = Shader.PropertyToID("_Debug_Output");
		private static readonly int _DebugModeEnumWs_Name = Shader.PropertyToID("_DebugModeEnumWS");
		
		private GeneralData GeneralData;
		private VoxelizationRuntimeData VoxelizationRuntimeData;

		RTHandle OutputTarget;
		
		ComputeShader HDebug;
		ComputeShader HReflectionProbeCompose;
		
		private bool _initialized;

		protected internal void Initialize(GeneralData generalData, VoxelizationRuntimeData voxelizationRuntimeData)
		{
			enabled = true;

			GeneralData = generalData;
			VoxelizationRuntimeData = voxelizationRuntimeData;

			_initialized = true;
		}

		protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
		{
			name = HTraceNames.HTRACE_FINAL_PASS_NAME_FRAME_DEBUG;
			
			HDebug = HExtensions.LoadComputeShader("HDebug");
			HReflectionProbeCompose = HExtensions.LoadComputeShader("HReflectionProbeCompose");
			
			AllocateDebugBuffer();
		}

		private void AllocateDebugBuffer()
		{
			HExtensions.HRelease(OutputTarget);

			if (Application.isPlaying == false)
				TextureXR.maxViews = 1;

			var colorBufferFormat = HExtensions.HdrpAsset?.currentPlatformRenderPipelineSettings.colorBufferFormat == RenderPipelineSettings.ColorBufferFormat.R11G11B10 ? GraphicsFormat.B10G11R11_UFloatPack32 : GraphicsFormat.R16G16B16A16_SFloat;

			OutputTarget = RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension,
				colorFormat: colorBufferFormat, name: "_OutputTarget", enableRandomWrite: true); 
		}

		protected override void Execute(CustomPassContext ctx)
		{
			if (_initialized == false)
				return;

			Texture ShadowmapData = Shader.GetGlobalTexture("_HTraceShadowmap");
			if (ShadowmapData == null || ShadowmapData.width != 2048)
				return;
			
			var cmdList = ctx.cmd;
			var hdCamera = ctx.hdCamera.camera;
			
			if (hdCamera.cameraType == CameraType.Reflection)
				return;
			
			int DebugDispatchX = (ctx.hdCamera.actualWidth + 8 - 1) / 8;
			int DebugDispatchY = (ctx.hdCamera.actualHeight + 8 - 1) / 8;
			
			// if (hdCamera.cameraType == CameraType.Reflection)
			// {	
			// 	// Render to real-time reflection probe
			// 	int reflection_probe_compose_kernel = HReflectionProbeCompose.FindKernel("RenderVoxelsForReflectionProbes");
			// 	ctx.cmd.SetComputeTextureParam(HReflectionProbeCompose, reflection_probe_compose_kernel, "_Output", OutputTarget);
			// 	ctx.cmd.DispatchCompute(HReflectionProbeCompose, reflection_probe_compose_kernel, DebugDispatchX, DebugDispatchY, 1);
			//
			// 	// Copy to camera color buffer
			// 	ctx.cmd.CopyTexture(OutputTarget, ctx.cameraColorBuffer);
			// 	return;	
			// }

			VoxelizationRuntimeData.VoxelizationModeChanged = false;

			if (GeneralData.DebugModeWS == DebugModeWS.None) 
				return;
			
			using (new ProfilingScope(cmdList, new ProfilingSampler("Debug")))
			{
				// Render debug
				int DebugKernel = HDebug.FindKernel("Debug");
				cmdList.SetComputeTextureParam(HDebug, DebugKernel, _Debug_Output_Name, OutputTarget, 0);
				cmdList.SetComputeIntParam(HDebug, _DebugModeEnumWs_Name, (int)GeneralData.DebugModeWS); 
				cmdList.DispatchCompute(HDebug, DebugKernel, DebugDispatchX, DebugDispatchY, TextureXR.slices);
				
				// Copy to camera color buffer
				ctx.cmd.CopyTexture(OutputTarget, ctx.cameraColorBuffer);
			}
		}

		internal void Release()
		{	
			HExtensions.HRelease(OutputTarget);
			_initialized = false;
		}
	}
}
