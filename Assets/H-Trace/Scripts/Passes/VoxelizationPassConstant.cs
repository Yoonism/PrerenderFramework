using H_Trace.Scripts.Structs;
using H_Trace.Scripts.Globals;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace H_Trace.Scripts.Passes
{
	internal class VoxelizationPassConstant : CustomPass
	{
		#region Shaders Properties ID
		
		//globals
		private static readonly int g_VoxelCameraPos = Shader.PropertyToID("_VoxelCameraPos");
		private static readonly int g_VoxelResolution = Shader.PropertyToID("_VoxelResolution");
		private static readonly int g_VoxelBounds = Shader.PropertyToID("_VoxelBounds");
		private static readonly int g_VoxelPerMeter = Shader.PropertyToID("_VoxelPerMeter");
		private static readonly int g_VoxelSize = Shader.PropertyToID("_VoxelSize");
		private static readonly int g_VoxelizationAABB_Min = Shader.PropertyToID("_VoxelizationAABB_Min");
		private static readonly int g_VoxelizationAABB_Max = Shader.PropertyToID("_VoxelizationAABB_Max");
		private static readonly int g_DirLightMatrix = Shader.PropertyToID("_DirLightMatrix");
		private static readonly int g_DirLightPlanes = Shader.PropertyToID("_DirLightPlanes");
		private static readonly int g_HTraceShadowmap = Shader.PropertyToID("_HTraceShadowmap");
		private static readonly int g_VoxelData = Shader.PropertyToID("_VoxelData");
		private static readonly int g_VoxelPositionPyramid = Shader.PropertyToID("_VoxelPositionPyramid");
		
		//locals
		private static readonly int _VoxelPositionPyramid_Mip0 = Shader.PropertyToID("_VoxelPositionPyramid_MIP0");
		private static readonly int _VoxelPositionPyramid_Mip1 = Shader.PropertyToID("_VoxelPositionPyramid_MIP1");
		private static readonly int _VoxelPositionPyramid_Mip2 = Shader.PropertyToID("_VoxelPositionPyramid_MIP2");
		private static readonly int _VoxelPositionIntermediate_Output = Shader.PropertyToID("_VoxelPositionIntermediate_Output");
		private static readonly int _VoxelPositionIntermediate = Shader.PropertyToID("_VoxelPositionIntermediate");
		private static readonly int _VoxelPositionPyramid_Mip3 = Shader.PropertyToID("_VoxelPositionPyramid_MIP3");
		private static readonly int _VoxelPositionPyramid_Mip4 = Shader.PropertyToID("_VoxelPositionPyramid_MIP4");
		private static readonly int _VoxelPositionPyramid_Mip5 = Shader.PropertyToID("_VoxelPositionPyramid_MIP5");
		
		#endregion Shaders Properties ID
		
		private VoxelizationData        VoxelizationData;
		private VoxelizationRuntimeData VoxelizationRuntimeData;

		// Shaders & Materials
		private Shader        VoxelizationShader;
		private Material      ShadowmapMaterial;
		private ComputeShader HVoxelization;

		// Buffers & Textures
		ComputeBuffer DummyVoxelBuffer;

		RTHandle DummyVoxelizationTarget;
		RTHandle DirectionalDepthTarget;
		RTHandle VoxelPositionPyramid;
		RTHandle VoxelPositionIntermediate;
		RTHandle VoxelData;

		// Constants & Variables
		Vector2 ShadowmapResolution = new Vector2(2048, 2048);
		private bool _initialized;

		private void AllocateMainBuffers(bool onlyRelease = false)
		{
			void ReleaseBuffersAndTextures()
			{
				HExtensions.HRelease(DummyVoxelizationTarget);
				HExtensions.HRelease(DirectionalDepthTarget);
				HExtensions.HRelease(VoxelPositionPyramid);
				HExtensions.HRelease(VoxelPositionIntermediate);
				HExtensions.HRelease(VoxelData);

				HExtensions.HRelease(DummyVoxelBuffer);
			}

			if (onlyRelease)
			{
				ReleaseBuffersAndTextures();
				return;
			}

			ReleaseBuffersAndTextures();

			DummyVoxelBuffer = new ComputeBuffer(1, sizeof(int));

			int VoxelResX = VoxelizationData.ExactData.Resolution.x;
			int VoxelResY = VoxelizationData.ExactData.Resolution.z;
			int VoxelResZ = VoxelizationData.ExactData.Resolution.y;
			
			DummyVoxelizationTarget = RTHandles.Alloc(VoxelResX * 2, VoxelResZ * 2, dimension: TextureDimension.Tex2D,
				colorFormat: GraphicsFormat.R8_UNorm, name: "_DummyVoxelizationDynamicTarget");
			
			DirectionalDepthTarget = RTHandles.Alloc((int)ShadowmapResolution.x, (int)ShadowmapResolution.y, dimension: TextureDimension.Tex2D,
				colorFormat: GraphicsFormat.R8_SNorm, name: "_DirectionalDepthTargetCombined", depthBufferBits: DepthBits.Depth32);

			VoxelPositionPyramid = RTHandles.Alloc(VoxelResX, VoxelResY, VoxelResZ,
				dimension: TextureDimension.Tex3D, useMipMap: true, autoGenerateMips: false,
				colorFormat: GraphicsFormat.R8_UInt, name: "_VoxelPositionPyramid", enableRandomWrite: true);

			VoxelPositionIntermediate = RTHandles.Alloc(VoxelResX / 4, VoxelResY / 4, VoxelResZ / 4,
				dimension: TextureDimension.Tex3D, useMipMap: false, autoGenerateMips: false,
				colorFormat: GraphicsFormat.R8_UInt, name: "_VoxelPositionIntermediate", enableRandomWrite: true);

			VoxelData = RTHandles.Alloc(VoxelResX, VoxelResY, VoxelResZ, dimension: TextureDimension.Tex3D, useMipMap: false, autoGenerateMips: false,
				colorFormat: GraphicsFormat.R32_UInt, name: "_VoxelData", enableRandomWrite: true);
		}

		protected internal void Initialize(VoxelizationData voxelizationData, VoxelizationRuntimeData voxelizationRuntimeData)
		{
			enabled = true;

			VoxelizationData                          =  voxelizationData;
			VoxelizationRuntimeData                   =  voxelizationRuntimeData;
			VoxelizationRuntimeData.OnReallocTextures += ReAllocateMainBuffers;

			ReAllocateMainBuffers();
			_initialized = true;
		}

		protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
		{
			name = HTraceNames.HTRACE_VOXEL_CONSTANT_PASS_NAME_FRAME_DEBUG;
			
			VoxelizationShader = Shader.Find("HTrace/Voxelization");
			ShadowmapMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("HTrace/Shadowmap"));
			ShadowmapMaterial.enableInstancing = true;
			HVoxelization = HExtensions.LoadComputeShader("Voxelization");
		}

		private void ReAllocateMainBuffers()
		{
			VoxelizationRuntimeData.FullVoxelization = true;
			AllocateMainBuffers();
		}
		
		
		protected override void Execute(CustomPassContext ctx)
		{
			if (ctx.hdCamera.camera.cameraType == CameraType.Reflection)
				return;
			
#if UNITY_EDITOR
			if (HExtensions.PipelineSupportsSSGI == false)
				return;
#endif
			if (_initialized == false)
				return;
			
			if (VoxelizationRuntimeData.VoxelCamera == null)
				return;

			VoxelizationRuntimeData.VoxelCamera.ExecuteUpdate(ctx.hdCamera.camera);

			// Clear 3D textures
			using (new ProfilingScope(ctx.cmd, new ProfilingSampler("Clear Voxel Textures")))
			{	
				CoreUtils.SetRenderTarget(ctx.cmd, VoxelData, ClearFlag.Color, Color.clear, 0, CubemapFace.Unknown, -1);
				CoreUtils.SetRenderTarget(ctx.cmd, VoxelPositionPyramid, ClearFlag.Color, Color.clear, 0, CubemapFace.Unknown, -1);
				CoreUtils.SetRenderTarget(ctx.cmd, VoxelPositionPyramid, ClearFlag.Color, Color.clear, 1, CubemapFace.Unknown, -1);
				CoreUtils.SetRenderTarget(ctx.cmd, VoxelPositionPyramid, ClearFlag.Color, Color.clear, 2, CubemapFace.Unknown, -1);
				CoreUtils.SetRenderTarget(ctx.cmd, VoxelPositionPyramid, ClearFlag.Color, Color.clear, 3, CubemapFace.Unknown, -1);
				CoreUtils.SetRenderTarget(ctx.cmd, VoxelPositionPyramid, ClearFlag.Color, Color.clear, 4, CubemapFace.Unknown, -1);
				CoreUtils.SetRenderTarget(ctx.cmd, VoxelPositionPyramid, ClearFlag.Color, Color.clear, 5, CubemapFace.Unknown, -1);
				CoreUtils.SetRenderTarget(ctx.cmd, VoxelPositionIntermediate, ClearFlag.Color, Color.clear, 0, CubemapFace.Unknown, -1);
			}

			// Pass voxel camera pos to shaders
			ctx.cmd.SetGlobalVector(g_VoxelCameraPos, VoxelizationRuntimeData.VoxelCamera.transform.position);
			
			// Cache main camera matrices
			var viewMatrixCached = ctx.hdCamera.camera.worldToCameraMatrix;
			var projectionMatrixCached = ctx.hdCamera.camera.projectionMatrix;
			
			// Load our voxelization camera //
			var voxelizationCamera = VoxelizationRuntimeData.VoxelCamera.Camera;
			
			// Render voxels
			if (true)
			{
				// Pass main voxelization parameters to shaders
				ctx.cmd.SetGlobalVector(g_VoxelResolution, (Vector3)VoxelizationData.ExactData.Resolution);
				ctx.cmd.SetGlobalVector(g_VoxelBounds, VoxelizationData.ExactData.Bounds);
				ctx.cmd.SetGlobalFloat(g_VoxelPerMeter, VoxelizationData.ExactData.VoxelsPerMeter);
				ctx.cmd.SetGlobalFloat(g_VoxelSize, VoxelizationData.ExactData.VoxelSize);

				Bounds voxelizationAABB = new Bounds(VoxelizationRuntimeData.VoxelCamera.transform.position, VoxelizationData.ExactData.Bounds);
				ctx.cmd.SetGlobalVector(g_VoxelizationAABB_Min, voxelizationAABB.min);
				ctx.cmd.SetGlobalVector(g_VoxelizationAABB_Max, voxelizationAABB.max);
				
				if (voxelizationCamera.farClipPlane > 0)
				{
					// Set voxelization camera matrices
					ctx.cmd.SetViewProjectionMatrices(voxelizationCamera.worldToCameraMatrix, voxelizationCamera.projectionMatrix);
					
					// Set rendering targets
					ctx.cmd.ClearRandomWriteTargets();
					ctx.cmd.SetRandomWriteTarget(1, VoxelData);
					ctx.cmd.SetRandomWriteTarget(2, DummyVoxelBuffer, false);
					
#if UNITY_6000_0
					ScriptableRenderContext.PopDisableApiRenderers();
#endif
					// Prepare culling params and select LOD level
					voxelizationCamera.TryGetCullingParameters(out ScriptableCullingParameters voxelizationCullingParams);
					voxelizationCullingParams.cullingOptions = CullingOptions.None;
					voxelizationCullingParams.isOrthographic = true;
					
					LODParameters lodParameters = voxelizationCullingParams.lodParameters;
					lodParameters.cameraPosition = voxelizationCamera.transform.position;
					lodParameters.isOrthographic = true;
					lodParameters.orthoSize = 0;
					voxelizationCullingParams.lodParameters = lodParameters;
					QualitySettings.SetLODSettings(1, VoxelizationData.LODMax, false);
					
					// Cull meshes
					ctx.cullingResults = ctx.renderContext.Cull(ref voxelizationCullingParams);
#if UNITY_6000_0
					ScriptableRenderContext.PushDisableApiRenderers();
#endif
					
					// Set voxelization layer, render target and select shader pass
					LayerMask voxelizationLayer = VoxelizationData.VoxelizationMask;
					RTHandle voxelizationRenderTarget = DummyVoxelizationTarget;
					int voxelizationShaderPass = 0;
					Shader.EnableKeyword("CONSTANT_VOXELIZATION");
					Shader.DisableKeyword("PARTIAL_VOXELIZATION");
					Shader.DisableKeyword("DYNAMIC_VOXELIZATION");
					
					// Render voxels
					RenderingExtensions.RenderVoxels(ctx, voxelizationCamera, voxelizationRenderTarget, voxelizationLayer, OverriderShader: VoxelizationShader, OverrideMaterial: null, voxelizationShaderPass);
					
					// Reset rendering targets
					ctx.cmd.ClearRandomWriteTargets();
				}
			}

			// Render directional light shadowmap
			using (new ProfilingScope(ctx.cmd, new ProfilingSampler("Render Shadowmap")))
			{
				// Load our directional light camera and set its matrices
				var directionalLightCamera = VoxelizationRuntimeData.FakeDirectionalCamera.GetDirectionalCamera;
				ctx.cmd.SetViewProjectionMatrices(directionalLightCamera.worldToCameraMatrix, directionalLightCamera.projectionMatrix);

#if UNITY_6000_0
				ScriptableRenderContext.PopDisableApiRenderers();
#endif
				// Prepare culling params and select LOD level
				directionalLightCamera.TryGetCullingParameters(out ScriptableCullingParameters shadowCullingParams);
				shadowCullingParams.cullingOptions = CullingOptions.None;
				shadowCullingParams.isOrthographic = true;
				
				LODParameters lodParameters = shadowCullingParams.lodParameters;
				lodParameters.cameraPosition = voxelizationCamera.transform.position;
				lodParameters.isOrthographic = true;
				lodParameters.orthoSize = 0;
				shadowCullingParams.lodParameters = lodParameters;
				//QualitySettings.SetLODSettings(1, VoxelizationData.LODMax, false);
				
				// Cull meshes
				ctx.cullingResults = ctx.renderContext.Cull(ref shadowCullingParams);
				
#if UNITY_6000_0
					ScriptableRenderContext.PushDisableApiRenderers();
#endif
				LayerMask shadowmapLayer = VoxelizationData.VoxelizationMask;
				var shadowmapRenderTarget = DirectionalDepthTarget;
				ClearFlag clearDepthFlag = ClearFlag.Depth;
				
				// Pass directional light matrices to shaders for shadowmap sampling at hit points
				var viewMatrix  = directionalLightCamera.worldToCameraMatrix;
				var projectionMatrix = directionalLightCamera.projectionMatrix;
				projectionMatrix = GL.GetGPUProjectionMatrix(projectionMatrix, false);

				ctx.cmd.SetGlobalMatrix(g_DirLightMatrix, projectionMatrix * viewMatrix);
				ctx.cmd.SetGlobalVector(g_DirLightPlanes, new Vector2(directionalLightCamera.nearClipPlane, directionalLightCamera.farClipPlane));
				
				// Render shadowmap
				RenderingExtensions.RenderShadowmap(ctx, directionalLightCamera, shadowmapRenderTarget, shadowmapRenderTarget, shadowmapLayer,
													OverriderShader: null, OverrideMaterial: ShadowmapMaterial, ClearFlag: clearDepthFlag, UseShadowCasterPass: true); 
			}
			

			// Pass rendered shadowmap to shaders
			ctx.cmd.SetGlobalTexture(g_HTraceShadowmap, DirectionalDepthTarget);

			// Restore matrices and culling of the main camera
			ctx.cmd.SetViewProjectionMatrices(viewMatrixCached, projectionMatrixCached);
			ctx.cullingResults = ctx.cameraCullingResults;

			// Generate mip pyramid for 3D position texture
			using (new ProfilingScope(ctx.cmd, new ProfilingSampler("Generate Position Pyramid")))
			{
				// Generate 0-2 mip levels
				int positionPyramid1_Kernel = HVoxelization.FindKernel("GeneratePositionPyramid1");
				ctx.cmd.SetComputeTextureParam(HVoxelization, positionPyramid1_Kernel, g_VoxelData, VoxelData);
				ctx.cmd.SetComputeTextureParam(HVoxelization, positionPyramid1_Kernel, _VoxelPositionPyramid_Mip0, VoxelPositionPyramid, 0);
				ctx.cmd.SetComputeTextureParam(HVoxelization, positionPyramid1_Kernel, _VoxelPositionPyramid_Mip1, VoxelPositionPyramid, 1);
				ctx.cmd.SetComputeTextureParam(HVoxelization, positionPyramid1_Kernel, _VoxelPositionPyramid_Mip2, VoxelPositionPyramid, 2);
				ctx.cmd.SetComputeTextureParam(HVoxelization, positionPyramid1_Kernel, _VoxelPositionIntermediate_Output, VoxelPositionIntermediate);
				ctx.cmd.DispatchCompute(HVoxelization, positionPyramid1_Kernel,
					Mathf.CeilToInt(VoxelizationData.ExactData.Resolution.x / 8f),
					Mathf.CeilToInt(VoxelizationData.ExactData.Resolution.z / 8f),
					Mathf.CeilToInt(VoxelizationData.ExactData.Resolution.y / 8f));

				// Generate 3-5 mip levels
				int positionPyramid2_Kernel = HVoxelization.FindKernel("GeneratePositionPyramid2");
				ctx.cmd.SetComputeTextureParam(HVoxelization, positionPyramid2_Kernel, _VoxelPositionIntermediate, VoxelPositionIntermediate);
				ctx.cmd.SetComputeTextureParam(HVoxelization, positionPyramid2_Kernel, _VoxelPositionPyramid_Mip3, VoxelPositionPyramid, 3);
				ctx.cmd.SetComputeTextureParam(HVoxelization, positionPyramid2_Kernel, _VoxelPositionPyramid_Mip4, VoxelPositionPyramid, 4);
				ctx.cmd.SetComputeTextureParam(HVoxelization, positionPyramid2_Kernel, _VoxelPositionPyramid_Mip5, VoxelPositionPyramid, 5);
				ctx.cmd.DispatchCompute(HVoxelization, positionPyramid2_Kernel,
					Mathf.CeilToInt(VoxelizationData.ExactData.Resolution.x / 32f),
					Mathf.CeilToInt(VoxelizationData.ExactData.Resolution.z / 32f),
					Mathf.CeilToInt(VoxelizationData.ExactData.Resolution.y / 32f));
			}

			// Pass voxelized textures to shaders in the Main Pass
			ctx.cmd.SetGlobalTexture(g_VoxelPositionPyramid, VoxelPositionPyramid);
			ctx.cmd.SetGlobalTexture(g_VoxelData, VoxelData);
			
			VoxelizationRuntimeData.FullVoxelization = false;
		}

		protected internal void Release()
		{
			_initialized = false;
			AllocateMainBuffers(true);
			if (VoxelizationRuntimeData != null)
				VoxelizationRuntimeData.OnReallocTextures -= ReAllocateMainBuffers;
		}
	}
}
