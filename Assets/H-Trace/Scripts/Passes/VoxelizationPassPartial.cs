using H_Trace.Scripts.Structs;
using H_Trace.Scripts.Globals;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace H_Trace.Scripts.Passes
{
	internal class VoxelizationPassPartial : CustomPass
	{
		#region Shaders Properties ID

		//globals
		private static readonly int g_OffsetAxisIndex = Shader.PropertyToID("_OffsetAxisIndex");
		private static readonly int g_AxisOffset = Shader.PropertyToID("_AxisOffset");
		private static readonly int g_CullingTrim = Shader.PropertyToID("_CullingTrim");
		private static readonly int g_OctantOffset = Shader.PropertyToID("_OctantOffset");
		private static readonly int g_CullingTrimAxis = Shader.PropertyToID("_CullingTrimAxis");
		private static readonly int g_VoxelCameraPos = Shader.PropertyToID("_VoxelCameraPos");
		private static readonly int g_VoxelCameraPosActual = Shader.PropertyToID("_VoxelCameraPosActual");
		private static readonly int g_VoxelResolution = Shader.PropertyToID("_VoxelResolution");
		private static readonly int g_VoxelBounds = Shader.PropertyToID("_VoxelBounds");
		private static readonly int g_VoxelPerMeter = Shader.PropertyToID("_VoxelPerMeter");
		private static readonly int g_VoxelSize = Shader.PropertyToID("_VoxelSize");
		private static readonly int g_VoxelizationAABB_Min = Shader.PropertyToID("_VoxelizationAABB_Min");
		private static readonly int g_VoxelizationAABB_Max = Shader.PropertyToID("_VoxelizationAABB_Max");
		private static readonly int g_DirLightMatrix = Shader.PropertyToID("_DirLightMatrix");
		private static readonly int g_HTraceShadowmap = Shader.PropertyToID("_HTraceShadowmap");
		private static readonly int g_VoxelData = Shader.PropertyToID("_VoxelData");
		private static readonly int g_VoxelPositionPyramid = Shader.PropertyToID("_VoxelPositionPyramid");
		
		//locals
		private static readonly int _OctantCopyOffset = Shader.PropertyToID("_OctantCopyOffset");
		private static readonly int _VoxelOffset = Shader.PropertyToID("_VoxelOffset");
		private static readonly int _VoxelData_A = Shader.PropertyToID("_VoxelData_A");
		private static readonly int _VoxelData_B = Shader.PropertyToID("_VoxelData_B");
		private static readonly int _DirectionalShadowmapStatic = Shader.PropertyToID("_DirectionalShadowmapStatic");
		private static readonly int _Shadowmap = Shader.PropertyToID("_Shadowmap");
		private static readonly int _Shadowmap_Output = Shader.PropertyToID("_Shadowmap_Output");
		private static readonly int _OctantShadowOffset = Shader.PropertyToID("_OctantShadowOffset");
		private static readonly int _VoxelPositionPyramid_Mip0 = Shader.PropertyToID("_VoxelPositionPyramid_MIP0");
		private static readonly int _VoxelPositionPyramid_Mip1 = Shader.PropertyToID("_VoxelPositionPyramid_MIP1");
		private static readonly int _VoxelPositionPyramid_Mip2 = Shader.PropertyToID("_VoxelPositionPyramid_MIP2");
		private static readonly int _VoxelPositionIntermediate_Output = Shader.PropertyToID("_VoxelPositionIntermediate_Output");
		private static readonly int _VoxelPositionIntermediate = Shader.PropertyToID("_VoxelPositionIntermediate");
		private static readonly int _VoxelPositionPyramid_Mip3 = Shader.PropertyToID("_VoxelPositionPyramid_MIP3");
		private static readonly int _VoxelPositionPyramid_Mip4 = Shader.PropertyToID("_VoxelPositionPyramid_MIP4");
		private static readonly int _VoxelPositionPyramid_Mip5 = Shader.PropertyToID("_VoxelPositionPyramid_MIP5");

		#endregion Shaders Properties ID
		
		private VoxelizationData VoxelizationData;
		private VoxelizationRuntimeData VoxelizationRuntimeData;

		// Shaders & Materials
		private Shader VoxelizationShader;
		private Material ShadowmapMaterial;
		private ComputeShader HVoxelization;
		private ComputeShader HShadowmap;

		// Buffers & Textures
		ComputeBuffer DummyVoxelBuffer;

		RTHandle DummyVoxelizationStaticTarget;
		RTHandle DummyVoxelizationDynamicTarget;
		RTHandle DirectionalDepthTargetCombined;
		RTHandle DirectionalDepthTargetStatic;
		RTHandle DirectionalShadowmapStatic;
		RTHandle VoxelPositionPyramid;
		RTHandle VoxelPositionIntermediate;
		RTHandle VoxelData_A;
		RTHandle VoxelData_B;

		// Constants & Variables
		private readonly Vector2 _shadowmapResolution = new Vector2(2048, 2048);
		private Vector3 _prevCameraPos = Vector3.zero;
		private int _textureOutputCounter;
		private int _textureSwapCounter;
		private int _localFrameCounter;
		private int _octantSyncCounter;
		private int _waitCounter;
		private bool _initialized;

		private void AllocateMainBuffers(bool onlyRelease = false)
		{
			void ReleaseBuffersAndTextures()
			{
				HExtensions.HRelease(DummyVoxelizationStaticTarget);
				HExtensions.HRelease(DummyVoxelizationDynamicTarget);
				HExtensions.HRelease(DirectionalDepthTargetCombined);
				HExtensions.HRelease(DirectionalDepthTargetStatic);
				HExtensions.HRelease(DirectionalShadowmapStatic);
				HExtensions.HRelease(VoxelPositionPyramid);
				HExtensions.HRelease(VoxelPositionIntermediate);
				HExtensions.HRelease(VoxelData_A);
				HExtensions.HRelease(VoxelData_B);

				HExtensions.HRelease(DummyVoxelBuffer);
			}

			if (onlyRelease)
			{
				ReleaseBuffersAndTextures();
				return;
			}

			ReleaseBuffersAndTextures();

			DummyVoxelBuffer = new ComputeBuffer(1, sizeof(int));

			int voxelResX = VoxelizationData.ExactData.Resolution.x;
			int voxelResY = VoxelizationData.ExactData.Resolution.z;
			int voxelResZ = VoxelizationData.ExactData.Resolution.y;

			DummyVoxelizationStaticTarget = RTHandles.Alloc(voxelResX, voxelResZ, dimension: TextureDimension.Tex2D,
				colorFormat: GraphicsFormat.R8_UNorm, name: "_DummyVoxelizationStaticTarget");

			DummyVoxelizationDynamicTarget = RTHandles.Alloc(voxelResX * 2, voxelResZ * 2, dimension: TextureDimension.Tex2D,
				colorFormat: GraphicsFormat.R8_UNorm, name: "_DummyVoxelizationDynamicTarget");

			DirectionalDepthTargetStatic = RTHandles.Alloc((int)_shadowmapResolution.x / 2, (int)_shadowmapResolution.y / 2, dimension: TextureDimension.Tex2D,
				colorFormat: GraphicsFormat.R8_SNorm, name: "_DirectionalDepthTargetStatic", depthBufferBits: DepthBits.Depth32);

			DirectionalDepthTargetCombined = RTHandles.Alloc((int)_shadowmapResolution.x, (int)_shadowmapResolution.y, dimension: TextureDimension.Tex2D,
				colorFormat: GraphicsFormat.R8_SNorm, name: "_DirectionalDepthTargetCombined", depthBufferBits: DepthBits.Depth32);

			DirectionalShadowmapStatic = RTHandles.Alloc((int)_shadowmapResolution.x, (int)_shadowmapResolution.y, dimension: TextureDimension.Tex2D,
				colorFormat: GraphicsFormat.R32_SFloat, name: "_DirectionalShadowmapStatic", enableRandomWrite: true);

			VoxelPositionPyramid = RTHandles.Alloc(voxelResX, voxelResY, voxelResZ, dimension: TextureDimension.Tex3D, useMipMap: true, autoGenerateMips: false,
				colorFormat: GraphicsFormat.R8_UInt, name: "_VoxelPositionPyramid", enableRandomWrite: true);

			VoxelPositionIntermediate = RTHandles.Alloc(voxelResX / 4, voxelResY / 4, voxelResZ / 4, dimension: TextureDimension.Tex3D, useMipMap: false, autoGenerateMips: false,
				colorFormat: GraphicsFormat.R8_UInt, name: "_VoxelPositionIntermediate", enableRandomWrite: true);

			VoxelData_A = RTHandles.Alloc(voxelResX, voxelResY, voxelResZ, dimension: TextureDimension.Tex3D, useMipMap: false, autoGenerateMips: false,
				colorFormat: GraphicsFormat.R32_UInt, name: "_VoxelData_A", enableRandomWrite: true);

			VoxelData_B = RTHandles.Alloc(voxelResX, voxelResY, voxelResZ, dimension: TextureDimension.Tex3D, useMipMap: false, autoGenerateMips: false,
				colorFormat: GraphicsFormat.R32_UInt, name: "_VoxelData_B", enableRandomWrite: true);
		}

		protected internal void Initialize(VoxelizationData voxelizationData, VoxelizationRuntimeData voxelizationRuntimeData)
		{
			enabled = true;

			VoxelizationData =  voxelizationData;
			VoxelizationRuntimeData  =  voxelizationRuntimeData;
			VoxelizationRuntimeData.OnReallocTextures += ReAllocateMainBuffers;

			ReAllocateMainBuffers();

			_initialized = true;
		}

		protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
		{
			name = HTraceNames.HTRACE_VOXEL_PARTIAL_PASS_NAME_FRAME_DEBUG;

			VoxelizationShader = Shader.Find("HTrace/Voxelization");
			ShadowmapMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("HTrace/Shadowmap"));
			ShadowmapMaterial.enableInstancing = true;
			HVoxelization = HExtensions.LoadComputeShader("Voxelization");
			HShadowmap = HExtensions.LoadComputeShader("Shadowmap");
		}

		private void ReAllocateMainBuffers()
		{
			VoxelizationRuntimeData.FullVoxelization = true;
			AllocateMainBuffers();
		}

		// Calculates offset data for voxelization
		private void CalculateOctantOffsets(CustomPassContext ctx)
		{
			Vector2 cullingTrim     = Vector2.zero;
			Vector2 cullingTrimAxis = Vector2.zero;
			Vector3 octantOffset    = Vector3.zero;
			Vector3 axisOffset      = Vector3.zero;

			Vector3 voxelResolutionSwizzled = new Vector3(VoxelizationData.ExactData.Resolution.x, VoxelizationData.ExactData.Resolution.z, VoxelizationData.ExactData.Resolution.y);
			Vector3 axisOffsetSign  = VoxelizationRuntimeData.VoxelOctantCamera.transform.position - VoxelizationRuntimeData.VoxelCamera.transform.position;
			int offsetAxisIndex = (int)VoxelizationRuntimeData.OffsetAxisIndex > 2 ? (int)VoxelizationRuntimeData.OffsetAxisIndex - 3 : (int)VoxelizationRuntimeData.OffsetAxisIndex;
			int octantIndex = (int)VoxelizationRuntimeData.OctantIndex;

			if (offsetAxisIndex == 0) // X axis
			{
				axisOffset = axisOffsetSign.x > 0 ? new Vector3(voxelResolutionSwizzled.x / 2, 0, 0) : new Vector3(0, 0, 0);

				int cullingFarPlane = axisOffsetSign.x > 0
					? Mathf.RoundToInt(VoxelizationRuntimeData.CullingCamera.Camera.farClipPlane * VoxelizationData.ExactData.VoxelsPerMeter)
					: Mathf.FloorToInt(VoxelizationRuntimeData.CullingCamera.Camera.farClipPlane * VoxelizationData.ExactData.VoxelsPerMeter);

				cullingTrim = axisOffsetSign.x > 0 ? new Vector2((int)voxelResolutionSwizzled.x - cullingFarPlane, (int)voxelResolutionSwizzled.x) : new Vector2(0, cullingFarPlane);
				cullingTrimAxis = axisOffsetSign.x > 0 ? new Vector2(1,                                                0) : new Vector2(0,                              1);

				if (octantIndex == 1) octantOffset = new Vector3(0, voxelResolutionSwizzled.y / 2, 0);
				if (octantIndex == 2) octantOffset = new Vector3(0, voxelResolutionSwizzled.y / 2, voxelResolutionSwizzled.z / 2);
				if (octantIndex == 3) octantOffset = new Vector3(0, 0, 0);
				if (octantIndex == 4) octantOffset = new Vector3(0, 0, voxelResolutionSwizzled.z / 2);
			}

			if (offsetAxisIndex == 1) // Y axis
			{
				axisOffset = axisOffsetSign.y > 0 ? new Vector3(0, voxelResolutionSwizzled.y / 2, 0) : new Vector3(0, 0, 0);

				int cullingFarPlane = axisOffsetSign.y > 0
					? Mathf.RoundToInt(VoxelizationRuntimeData.CullingCamera.Camera.farClipPlane * VoxelizationData.ExactData.VoxelsPerMeter)
					: Mathf.FloorToInt(VoxelizationRuntimeData.CullingCamera.Camera.farClipPlane * VoxelizationData.ExactData.VoxelsPerMeter);

				cullingTrim = axisOffsetSign.y > 0 ? new Vector2((int)voxelResolutionSwizzled.y - cullingFarPlane, (int)voxelResolutionSwizzled.y) : new Vector2(0, cullingFarPlane);
				cullingTrimAxis = axisOffsetSign.y > 0 ? new Vector2(1,                                                0) : new Vector2(0,                              1);

				if (octantIndex == 1) octantOffset = new Vector3(0, 0, 0);
				if (octantIndex == 2) octantOffset = new Vector3(0, 0, voxelResolutionSwizzled.z / 2);
				if (octantIndex == 3) octantOffset = new Vector3(voxelResolutionSwizzled.x / 2, 0, 0);
				if (octantIndex == 4) octantOffset = new Vector3(voxelResolutionSwizzled.x / 2, 0, voxelResolutionSwizzled.z / 2);
			}

			if (offsetAxisIndex == 2) // Z axis
			{
				axisOffset = axisOffsetSign.z > 0 ? new Vector3(0, 0, voxelResolutionSwizzled.z / 2) : new Vector3(0, 0, 0);

				int cullingFarPlane = axisOffsetSign.z > 0
					? Mathf.RoundToInt(VoxelizationRuntimeData.CullingCamera.Camera.farClipPlane * VoxelizationData.ExactData.VoxelsPerMeter)
					: Mathf.FloorToInt(VoxelizationRuntimeData.CullingCamera.Camera.farClipPlane * VoxelizationData.ExactData.VoxelsPerMeter);

				cullingTrim = axisOffsetSign.z > 0 ? new Vector2((int)voxelResolutionSwizzled.z - cullingFarPlane, (int)voxelResolutionSwizzled.z) : new Vector2(0, cullingFarPlane);
				cullingTrimAxis = axisOffsetSign.z > 0 ? new Vector2(1,                                                0) : new Vector2(0,                              1);

				if (octantIndex == 1) octantOffset = new Vector3(voxelResolutionSwizzled.x / 2, voxelResolutionSwizzled.y / 2, 0);
				if (octantIndex == 2) octantOffset = new Vector3(0, voxelResolutionSwizzled.y / 2, 0);
				if (octantIndex == 3) octantOffset = new Vector3(voxelResolutionSwizzled.x / 2, 0, 0);
				if (octantIndex == 4) octantOffset = new Vector3(0, 0, 0);
			}

			ctx.cmd.SetGlobalInt(g_OffsetAxisIndex, offsetAxisIndex);
			ctx.cmd.SetGlobalVector(g_AxisOffset, axisOffset);
			ctx.cmd.SetGlobalVector(g_CullingTrim, cullingTrim);
			ctx.cmd.SetGlobalVector(g_OctantOffset, octantOffset);
			ctx.cmd.SetGlobalVector(g_CullingTrimAxis, cullingTrimAxis);
		}

		// Calculates offset data for copying compute shader
		Vector3 CalculateOctantOffsetsForCopyShader()
		{
			Vector3 octantOffset = Vector3.zero;

			Vector3 voxelResolutionSwizzled = new Vector3(VoxelizationData.ExactData.Resolution.x, VoxelizationData.ExactData.Resolution.z, VoxelizationData.ExactData.Resolution.y);
			Vector3 axisOffsetSign = VoxelizationRuntimeData.VoxelOctantCamera.transform.position - VoxelizationRuntimeData.VoxelCamera.transform.position;
			int offsetAxisIndex = (int)VoxelizationRuntimeData.OffsetAxisIndex > 2 ? (int)VoxelizationRuntimeData.OffsetAxisIndex - 3 : (int)VoxelizationRuntimeData.OffsetAxisIndex;
			int octantIndex = (int)VoxelizationRuntimeData.OctantIndex;

			if (offsetAxisIndex == 0) // X axis
			{
				if (axisOffsetSign.x < 0)
				{
					if (octantIndex == 1) octantOffset = new Vector3(0, 0, 0);
					if (octantIndex == 2) octantOffset = new Vector3(0, 0, voxelResolutionSwizzled.z / 2);
					if (octantIndex == 3) octantOffset = new Vector3(voxelResolutionSwizzled.x / 2, 0, voxelResolutionSwizzled.z / 2);
					if (octantIndex == 4) octantOffset = new Vector3(voxelResolutionSwizzled.x / 2, 0, 0);
				}
				else
				{
					if (octantIndex == 1) octantOffset = new Vector3(voxelResolutionSwizzled.x / 2, 0, 0);
					if (octantIndex == 2) octantOffset = new Vector3(voxelResolutionSwizzled.x / 2, 0, voxelResolutionSwizzled.z / 2);
					if (octantIndex == 3) octantOffset = new Vector3(0, 0, 0);
					if (octantIndex == 4) octantOffset = new Vector3(0, 0, voxelResolutionSwizzled.z / 2);
				}
			}

			if (offsetAxisIndex == 1) // Y axis
			{
				if (octantIndex == 1) octantOffset = new Vector3(0, 0, 0);
				if (octantIndex == 2) octantOffset = new Vector3(0, 0, voxelResolutionSwizzled.z / 2);
				if (octantIndex == 3) octantOffset = new Vector3(voxelResolutionSwizzled.x / 2, 0, 0);
				if (octantIndex == 4) octantOffset = new Vector3(voxelResolutionSwizzled.x / 2, 0, voxelResolutionSwizzled.z / 2);
			}

			if (offsetAxisIndex == 2) // Z axis
			{
				if (axisOffsetSign.z < 0)
				{
					if (octantIndex == 1) octantOffset = new Vector3(voxelResolutionSwizzled.x / 2, 0, 0);
					if (octantIndex == 2) octantOffset = new Vector3(0, 0, 0);
					if (octantIndex == 3) octantOffset = new Vector3(0, 0, voxelResolutionSwizzled.z / 2);
					if (octantIndex == 4) octantOffset = new Vector3(voxelResolutionSwizzled.x / 2, 0, voxelResolutionSwizzled.z / 2);
				}
				else
				{
					if (octantIndex == 1) octantOffset = new Vector3(voxelResolutionSwizzled.x / 2, 0, voxelResolutionSwizzled.z / 2);
					if (octantIndex == 2) octantOffset = new Vector3(0, 0, voxelResolutionSwizzled.z / 2);
					if (octantIndex == 3) octantOffset = new Vector3(0, 0, 0);
					if (octantIndex == 4) octantOffset = new Vector3(voxelResolutionSwizzled.x / 2, 0, 0);
				}
			}

			return octantOffset;
		}


		protected override void Execute(CustomPassContext ctx)
		{	
			if (ctx.hdCamera.camera.cameraType == CameraType.Reflection)
				return;
			
#if UNITY_EDITOR
			if (UnityEditor.EditorApplication.isPaused && VoxelizationRuntimeData.OctantIndex != OctantIndex.DynamicObjects)
				return;
			
			if (HExtensions.PipelineSupportsSSGI == false)
				return;
#endif
			if (_initialized == false)
				return;
			
			if (VoxelizationRuntimeData == null || VoxelizationRuntimeData.VoxelCamera == null)
				return;

			VoxelizationRuntimeData.VoxelCamera.ExecuteUpdate(ctx.hdCamera.camera);

			// Wait for the start of the cycle to do full voxelization
			if (VoxelizationRuntimeData.FullVoxelization == true && (int)VoxelizationRuntimeData.OctantIndex > 1)
				return;
			

			if (VoxelizationRuntimeData.FullVoxelization != true) // TODO: once framecounter is fixed revisit it and remove if possible
			{
				// Reset sync counter at the end of every cycle
				if (_octantSyncCounter == 5)
					_octantSyncCounter = 0;

				// Increment wait counter after each index = 1
				if ((int)VoxelizationRuntimeData.OctantIndex == 1)
					_waitCounter++;

				// Cycle is still broken, skip and wait
				if (_waitCounter < 1)
					return;

				// Cycle went out of sync, skip and wait
				if (_localFrameCounter != 0 && (int)VoxelizationRuntimeData.OctantIndex != _octantSyncCounter + 1)
					return;

				// Set sync counter to the current octant index
				_octantSyncCounter = (int)VoxelizationRuntimeData.OctantIndex;
			}
			
			bool isFullVoxelization = VoxelizationRuntimeData.FullVoxelization;
			bool isDynamicOctant = (int)VoxelizationRuntimeData.OctantIndex == 5 ? true : false;
			bool isFirstOctant = (int)VoxelizationRuntimeData.OctantIndex == 1 ? true : false;

			// Swap counter, flips at the start of every cycle
			if (isFirstOctant && !isFullVoxelization)
				_textureSwapCounter++;

			// Output counter, flips at the end of every cycle
			if (isDynamicOctant && !isFullVoxelization)
				_textureOutputCounter++;

			// Clear 3D textures
			using (new ProfilingScope(ctx.cmd, new ProfilingSampler("Clear Voxel Textures")))
			{
				if (isDynamicOctant || isFullVoxelization)
				{
					CoreUtils.SetRenderTarget(ctx.cmd, VoxelPositionPyramid, ClearFlag.Color, Color.clear, 0, CubemapFace.Unknown, -1);
					CoreUtils.SetRenderTarget(ctx.cmd, VoxelPositionPyramid, ClearFlag.Color, Color.clear, 1, CubemapFace.Unknown, -1);
					CoreUtils.SetRenderTarget(ctx.cmd, VoxelPositionPyramid, ClearFlag.Color, Color.clear, 2, CubemapFace.Unknown, -1);
					CoreUtils.SetRenderTarget(ctx.cmd, VoxelPositionPyramid, ClearFlag.Color, Color.clear, 3, CubemapFace.Unknown, -1);
					CoreUtils.SetRenderTarget(ctx.cmd, VoxelPositionPyramid, ClearFlag.Color, Color.clear, 4, CubemapFace.Unknown, -1);
					CoreUtils.SetRenderTarget(ctx.cmd, VoxelPositionPyramid, ClearFlag.Color, Color.clear, 5, CubemapFace.Unknown, -1);
					CoreUtils.SetRenderTarget(ctx.cmd, VoxelPositionIntermediate, ClearFlag.Color, Color.clear, 0, CubemapFace.Unknown, -1);
				}

				if (isFirstOctant || isFullVoxelization)
					CoreUtils.SetRenderTarget(ctx.cmd, _textureSwapCounter % 2 == 0 ? VoxelData_B : VoxelData_A, ClearFlag.Color, Color.clear, 0, CubemapFace.Unknown, -1);
			}

			// Pass voxel camera pos to shaders at the end of the cycle
			if (isDynamicOctant || isFullVoxelization)
				ctx.cmd.SetGlobalVector(g_VoxelCameraPos, VoxelizationRuntimeData.VoxelCamera.transform.position);

			// Pass actual voxel camera pos to voxelization shader for geometry trimming
			if (isFirstOctant || isFullVoxelization)
				ctx.cmd.SetGlobalVector(g_VoxelCameraPosActual, VoxelizationRuntimeData.VoxelCamera.transform.position);

			// Scroll voxels by copying them
			using (new ProfilingScope(ctx.cmd, new ProfilingSampler("Copy Voxels")))
			{
				if (!isDynamicOctant && !isFullVoxelization)
				{
					Vector3 octantCopyOffset = CalculateOctantOffsetsForCopyShader();

					int voxelCopyKernel = HVoxelization.FindKernel("CopyData");
					ctx.cmd.SetComputeVectorParam(HVoxelization, _OctantCopyOffset, octantCopyOffset);
					ctx.cmd.SetComputeVectorParam(HVoxelization, _VoxelOffset, VoxelizationRuntimeData.VoxelCamera.transform.position - _prevCameraPos);
					ctx.cmd.SetComputeTextureParam(HVoxelization, voxelCopyKernel, _VoxelData_A, _textureSwapCounter % 2 == 0 ? VoxelData_B : VoxelData_A);
					ctx.cmd.SetComputeTextureParam(HVoxelization, voxelCopyKernel, _VoxelData_B, _textureSwapCounter % 2 == 0 ? VoxelData_A : VoxelData_B);
					ctx.cmd.DispatchCompute(HVoxelization, voxelCopyKernel,
						Mathf.CeilToInt(VoxelizationData.ExactData.Resolution.x / 8f / 2),
						Mathf.CeilToInt(VoxelizationData.ExactData.Resolution.z / 4f / 2),
						Mathf.CeilToInt(VoxelizationData.ExactData.Resolution.y / 8f / 2));
				}
			}

			// Cache main camera matrices
			var viewMatrixCached = ctx.hdCamera.camera.worldToCameraMatrix;
			var projectionMatrixCached = ctx.hdCamera.camera.projectionMatrix;

			// Render voxels
			if (true)
			{
				// Pass main voxelization parameters to shaders
				ctx.cmd.SetGlobalVector(g_VoxelResolution, (Vector3)VoxelizationData.ExactData.Resolution);
				ctx.cmd.SetGlobalVector(g_VoxelBounds, VoxelizationData.ExactData.Bounds);
				ctx.cmd.SetGlobalFloat(g_VoxelPerMeter, VoxelizationData.ExactData.VoxelsPerMeter);
				ctx.cmd.SetGlobalFloat(g_VoxelSize, VoxelizationData.ExactData.VoxelSize);

				Bounds voxelizationAABB = new UnityEngine.Bounds(VoxelizationRuntimeData.VoxelCamera.transform.position, VoxelizationData.ExactData.Bounds);
				ctx.cmd.SetGlobalVector(g_VoxelizationAABB_Min, voxelizationAABB.min);
				ctx.cmd.SetGlobalVector(g_VoxelizationAABB_Max, voxelizationAABB.max);

				// Load our culling & voxelization camera
				Camera cullingCamera = isFullVoxelization ? VoxelizationRuntimeData.VoxelCamera.Camera : VoxelizationRuntimeData.CullingCamera.Camera;
				Camera voxelizationCamera = isFullVoxelization ? VoxelizationRuntimeData.VoxelCamera.Camera : VoxelizationRuntimeData.VoxelOctantCamera.Camera;

				if (cullingCamera.farClipPlane > 0)
				{
					// Set voxelization camera matrices
					ctx.cmd.SetViewProjectionMatrices(voxelizationCamera.worldToCameraMatrix, voxelizationCamera.projectionMatrix);

					// Calculate octant offsets
					if ((int)VoxelizationRuntimeData.OctantIndex != 5)
						CalculateOctantOffsets(ctx);

					// Set rendering targets
					ctx.cmd.ClearRandomWriteTargets();
					ctx.cmd.SetRandomWriteTarget(1, _textureSwapCounter % 2 == 0 ? VoxelData_B : VoxelData_A);
					ctx.cmd.SetRandomWriteTarget(2, DummyVoxelBuffer, false);

#if UNITY_6000_0
					ScriptableRenderContext.PopDisableApiRenderers();
#endif
					// Prepare culling params and select LOD level
					cullingCamera.TryGetCullingParameters(out ScriptableCullingParameters voxelizationCullingParams);
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
				
					LayerMask voxelizationLayer  = VoxelizationData.VoxelizationMask & ~VoxelizationData.DynamicObjectsMask;
					var voxelizationRenderTarget = DummyVoxelizationStaticTarget;
					int voxelizationShaderPass = 0;
					Shader.EnableKeyword("PARTIAL_VOXELIZATION");
					Shader.DisableKeyword("DYNAMIC_VOXELIZATION");
					Shader.DisableKeyword("CONSTANT_VOXELIZATION");

					if (isFullVoxelization)
					{
						voxelizationRenderTarget = DummyVoxelizationDynamicTarget;
						voxelizationShaderPass = 0;
						Shader.EnableKeyword("CONSTANT_VOXELIZATION");
						Shader.DisableKeyword("PARTIAL_VOXELIZATION");
						Shader.DisableKeyword("DYNAMIC_VOXELIZATION");
					}
					else if ((int)VoxelizationRuntimeData.OctantIndex == 5)
					{
						voxelizationLayer = VoxelizationData.DynamicObjectsMask & VoxelizationData.VoxelizationMask;
						voxelizationRenderTarget = DummyVoxelizationDynamicTarget;
						voxelizationShaderPass = 0;
						Shader.EnableKeyword("DYNAMIC_VOXELIZATION");
						Shader.DisableKeyword("PARTIAL_VOXELIZATION");
						Shader.DisableKeyword("CONSTANT_VOXELIZATION");
					}

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
				lodParameters.cameraPosition = directionalLightCamera.transform.position;
				lodParameters.isOrthographic = true;
				lodParameters.orthoSize = 0;
				shadowCullingParams.lodParameters = lodParameters;

				// Cull meshes
				ctx.cullingResults = ctx.renderContext.Cull(ref shadowCullingParams);
#if UNITY_6000_0
					ScriptableRenderContext.PushDisableApiRenderers();
#endif

				LayerMask shadowmapLayer = VoxelizationData.VoxelizationMask & ~VoxelizationData.DynamicObjectsMask;
				RTHandle shadowmapRenderTarget = DirectionalDepthTargetStatic;
				ClearFlag clearDepthFlag = ClearFlag.Depth;

				if ((int)VoxelizationRuntimeData.OctantIndex == 5)
				{
					// Pass directional light matrices to shaders for shadowmap sampling at hit points
					var viewMatrix = directionalLightCamera.worldToCameraMatrix;
					var projectionMatrix = directionalLightCamera.projectionMatrix;
					projectionMatrix = GL.GetGPUProjectionMatrix(projectionMatrix, false);

					ctx.cmd.SetGlobalMatrix(g_DirLightMatrix, projectionMatrix * viewMatrix);

					// Copy merged static depth to the final depth render target where dynamic objects will be added
					ShadowmapMaterial.SetTexture(_DirectionalShadowmapStatic, DirectionalShadowmapStatic);
					CoreUtils.SetRenderTarget(ctx.cmd, DirectionalDepthTargetCombined, ClearFlag.Depth, 0, CubemapFace.Unknown, -1);
					CoreUtils.DrawFullScreen(ctx.cmd, ShadowmapMaterial, DirectionalDepthTargetCombined, DirectionalDepthTargetCombined, shaderPassId: 1, properties: ctx.propertyBlock);

					shadowmapLayer = VoxelizationData.DynamicObjectsMask;
					shadowmapRenderTarget = DirectionalDepthTargetCombined;
					clearDepthFlag = ClearFlag.None;
				}

				// Render shadowmap
				RenderingExtensions.RenderShadowmap(ctx, directionalLightCamera, shadowmapRenderTarget, shadowmapRenderTarget, shadowmapLayer,
													OverriderShader: null, OverrideMaterial: ShadowmapMaterial, ClearFlag: clearDepthFlag, UseShadowCasterPass: true);
			}

			// Merge shadowmap octants with static objects into a single texture
			using (new ProfilingScope(ctx.cmd, new ProfilingSampler("Merge Shadowmap Static")))
			{
				if ((int)VoxelizationRuntimeData.OctantIndex != 5)
				{
					Vector2 octantShadowOffset = Vector2.zero;
					int octantIndex = (int)VoxelizationRuntimeData.OctantIndex;

					if (octantIndex == 1) octantShadowOffset = new Vector2(0, _shadowmapResolution.y / 2);
					if (octantIndex == 2) octantShadowOffset = new Vector2(_shadowmapResolution.x / 2, _shadowmapResolution.y / 2);
					if (octantIndex == 3) octantShadowOffset = new Vector2(0, 0);
					if (octantIndex == 4) octantShadowOffset = new Vector2(_shadowmapResolution.x / 2, 0);

					int shadowmapMerge_Kernel = HShadowmap.FindKernel("ShadowmapMerge");
					ctx.cmd.SetComputeTextureParam(HShadowmap, shadowmapMerge_Kernel, _Shadowmap, DirectionalDepthTargetStatic);
					ctx.cmd.SetComputeTextureParam(HShadowmap, shadowmapMerge_Kernel, _Shadowmap_Output, DirectionalShadowmapStatic);
					ctx.cmd.SetComputeVectorParam(HShadowmap, _OctantShadowOffset, octantShadowOffset);
					ctx.cmd.DispatchCompute(HShadowmap, shadowmapMerge_Kernel, (int)_shadowmapResolution.x / 2 / 8, (int)_shadowmapResolution.y / 2 / 8, 1);
				}
			}

			// Pass rendered shadowmap to shaders
			ctx.cmd.SetGlobalTexture(g_HTraceShadowmap, DirectionalDepthTargetCombined);

			// Restore matrices and culling of the main camera
			ctx.cmd.SetViewProjectionMatrices(viewMatrixCached, projectionMatrixCached);
			ctx.cullingResults = ctx.cameraCullingResults;

			// Generate mip pyramid for 3D position texture
			using (new ProfilingScope(ctx.cmd, new ProfilingSampler("Generate Position Pyramid")))
			{
				if (isDynamicOctant || isFullVoxelization)
				{
					// Generate 0-2 mip levels
					int positionPyramid1_Kernel = HVoxelization.FindKernel("GeneratePositionPyramid1");
					ctx.cmd.SetComputeTextureParam(HVoxelization, positionPyramid1_Kernel, g_VoxelData,                        _textureSwapCounter % 2 == 0 ? VoxelData_B : VoxelData_A);
					ctx.cmd.SetComputeTextureParam(HVoxelization, positionPyramid1_Kernel, _VoxelPositionPyramid_Mip0,        VoxelPositionPyramid, 0);
					ctx.cmd.SetComputeTextureParam(HVoxelization, positionPyramid1_Kernel, _VoxelPositionPyramid_Mip1,        VoxelPositionPyramid, 1);
					ctx.cmd.SetComputeTextureParam(HVoxelization, positionPyramid1_Kernel, _VoxelPositionPyramid_Mip2,        VoxelPositionPyramid, 2);
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
			}

			if (isDynamicOctant || isFullVoxelization)
				_prevCameraPos = VoxelizationRuntimeData.VoxelCamera.transform.position;

			// Pass voxelized textures to shaders in the Main Pass
			ctx.cmd.SetGlobalTexture(g_VoxelPositionPyramid, VoxelPositionPyramid);
			ctx.cmd.SetGlobalTexture(g_VoxelData, _textureOutputCounter % 2 == 0 ? VoxelData_B : VoxelData_A);

			// Copy to the opposite data buffer to make sure we have full voxelization in both of them
			if (isFullVoxelization)
				ctx.cmd.CopyTexture(_textureSwapCounter % 2 == 0 ? VoxelData_B : VoxelData_A, _textureSwapCounter % 2 == 0 ? VoxelData_A : VoxelData_B);

			VoxelizationRuntimeData.FullVoxelization = false;

			_localFrameCounter++;
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
