using System;
using H_Trace.Scripts.Globals;
using H_Trace.Scripts.Infrastructure;
using H_Trace.Scripts.Passes;
using H_Trace.Scripts.Structs;
using H_Trace.Scripts.VoxelCameras;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace H_Trace.Scripts.PassHandlers
{
	[ExecuteInEditMode]
	internal class VoxelsHandler : PassHandler
	{
		//Debug fields
		public   bool   ShowVoxelBounds = false;
		internal Bounds BoundsGizmo;
		internal Bounds BoundsGizmoFromUI = new Bounds();

		private VoxelizationData        _voxelizationData;
		private DebugData               _debugData;
		private VoxelizationRuntimeData _voxelizationRuntimeData;

		private VoxelizationPassStaggered _voxelizationPassStaggered;
		private VoxelizationPassConstant  _voxelizationPassConstant;
		private VoxelizationPassPartial   _voxelizationPassPartial;

		private Transform              _prevAttachTo;
		private VoxelizationUpdateMode _prevVoxelizationUpdateMode;
		private int _prevLodMax;

		public bool NeedToReallocForUI
		{
			get { return _needToReallocForUI; }
		}

		//UI fields for Apply Params button
		private bool  _needToReallocForUI = false;
		private float _prevDensityUI;
		private int   _prevVoxelBoundsUI;
		private int   _prevOverrideBoundsHeightUI;

		private void OnEnable()
		{
		}

		public void Initialize(VoxelizationData voxelizationData, VoxelizationRuntimeData voxelizationRuntimeData, CustomPass[] passes, DebugData debugData)
		{
			_debugData               = debugData;
			_voxelizationData        = voxelizationData;
			_voxelizationRuntimeData = voxelizationRuntimeData;

			for (int index = 0; index < passes.Length; index++)
			{
				if (passes[index] is VoxelizationPassStaggered voxelizationPassStaggered)
					_voxelizationPassStaggered = voxelizationPassStaggered;
				if (passes[index] is VoxelizationPassConstant voxelizationPassConstant)
					_voxelizationPassConstant = voxelizationPassConstant;
				if (passes[index] is VoxelizationPassPartial voxelizationPassPassPartial)
					_voxelizationPassPartial = voxelizationPassPassPartial;
			}

			CreateVoxelCamera();
			CreateVoxelCullingCamera();
			CreateVoxelOctantCamera();
			CreateHTraceCameraDirectional();

			SwitchPass();

			_voxelizationRuntimeData.FullVoxelization  =  false;
			_voxelizationRuntimeData.OnReallocTextures += ReallocTextures;

			if (_voxelizationData.AttachTo == null && Camera.main != null)
				_voxelizationData.AttachTo = Camera.main.transform;
			_prevAttachTo = _voxelizationData.AttachTo;
		}

		private void ReallocTextures()
		{
			_needToReallocForUI = false;
		}

		private void SwitchPass()
		{
			switch (_prevVoxelizationUpdateMode)
			{
				//todo: stuggered uncomment
				// case VoxelizationUpdateMode.Staggered:
				// 	_voxelizationPassStaggered.Release();
				// 	_voxelizationPassStaggered.enabled = false;
				// 	break;
				case VoxelizationUpdateMode.Constant:
					_voxelizationPassConstant.Release();
					_voxelizationPassConstant.enabled = false;
					break;
				case VoxelizationUpdateMode.Partial:
					_voxelizationPassPartial.Release();
					_voxelizationPassPartial.enabled = false;
					break;
			}
			
			switch (_voxelizationData.VoxelizationUpdateMode)
			{
				//todo: stuggered uncomment
				// case VoxelizationUpdateMode.Staggered:
				// 	_voxelizationPassStaggered.Initialize(_voxelizationData, _voxelizationRuntimeData);
				// 	break;
				case VoxelizationUpdateMode.Constant:
					_voxelizationPassConstant.Initialize(_voxelizationData, _voxelizationRuntimeData);
					break;
				case VoxelizationUpdateMode.Partial:
					_voxelizationPassPartial.Initialize(_voxelizationData, _voxelizationRuntimeData);
					break;
			}

			_prevVoxelizationUpdateMode = _voxelizationData.VoxelizationUpdateMode;
			_voxelizationRuntimeData.VoxelizationModeChanged = true;
		}

		protected override void Update()
		{
			base.Update();

			if (_voxelizationRuntimeData == null || _voxelizationData == null || _voxelizationRuntimeData.FakeDirectionalCamera == null)
				return;

			if (_prevVoxelizationUpdateMode != _voxelizationData.VoxelizationUpdateMode)
				SwitchPass();
			
			//gizmo update for camera bounds
#if UNITY_EDITOR
			if (Application.isEditor)
			{
				BoundsGizmo = GetVoxelCameraBounds();
				SetVoxelCameraBounds(BoundsGizmo);
			}
#endif

			CheckBounds();
			_voxelizationData.UpdateData();
			_voxelizationRuntimeData.FakeDirectionalCamera.UpdateData(_voxelizationData);
			CheckPrevValues();
		}

		public void OnSceneGUI()
		{
			//gizmo update
			BoundsGizmo = GetVoxelCameraBounds();

			if (BoundsGizmoFromUI.size != BoundsGizmo.size /* && _boundsGizmoFromUIEdited*/)
			{
				SetVoxelCameraBounds(BoundsGizmoFromUI);
				BoundsGizmo = GetVoxelCameraBounds();
				// _boundsGizmoFromUIEdited = false;
			}
		}

		public Bounds GetVoxelCameraBounds()
		{
			Vector3 boundCenter = _voxelizationRuntimeData.VoxelCamera.transform.position;

			float height = _voxelizationData.OverrideBoundsHeightEnable == false ? _voxelizationData.VoxelBounds : _voxelizationData.OverrideBoundsHeight;
			if (_voxelizationData.GroundLevelEnable == true && (_voxelizationRuntimeData.VoxelCamera.transform.position.y - height / 2) < _voxelizationData.GroundLevel)
			{
				boundCenter = new Vector3(_voxelizationRuntimeData.VoxelCamera.transform.position.x, _voxelizationData.GroundLevel + height / 2, _voxelizationRuntimeData.VoxelCamera.transform.position.z);
			}

			BoundsGizmo.center = boundCenter;

			BoundsGizmo.size = new Vector3(
				_voxelizationData.VoxelBounds,
				height,
				_voxelizationData.VoxelBounds);

			return BoundsGizmo;
		}

		public void SetVoxelCameraBounds(Bounds newBounds)
		{
			if (newBounds.size.x < 1 || newBounds.size.y < 1 || newBounds.size.z < 1)
				return;
			
			int newWidthDepth = 0;
			if ((int)BoundsGizmo.size.x != (int)newBounds.size.x)
				newWidthDepth = (int)newBounds.size.x;
			if ((int)BoundsGizmo.size.z != (int)newBounds.size.z)
				newWidthDepth = (int)newBounds.size.z;
			if (newWidthDepth == 0)
			{
				if (_voxelizationData.OverrideBoundsHeightEnable == false)
					newWidthDepth = (int)newBounds.size.y;
				else
					newWidthDepth = (int)newBounds.size.x;
			}

			_voxelizationData.VoxelBounds = newWidthDepth;
			_voxelizationData.OverrideBoundsHeight = _voxelizationData.OverrideBoundsHeightEnable == true ? (int)newBounds.size.y : _voxelizationData.OverrideBoundsHeight;

			if (_voxelizationData.OverrideBoundsHeightEnable == true)
				_voxelizationData.OverrideBoundsHeight = Mathf.Clamp(_voxelizationData.OverrideBoundsHeight, 1, _voxelizationData.VoxelBounds);
			
			// float height = _voxelizationData.OverrideBoundsHeightEnable == false ? _voxelizationData.VoxelBounds : _voxelizationData.OverrideBoundsHeight;
			// if (_voxelizationData.GroundLevelEnable == true && (_voxelizationRuntimeData.VoxelCamera.transform.position.y - height / 2) <= _voxelizationData.GroundLevel)
			// {
			// 	_boundsGizmo.center = newBounds.center;
			// }

			//_boundsGizmo.size = newBounds.size;
			BoundsGizmo = GetVoxelCameraBounds();
		}

		private void CheckBounds()
		{
			if (_voxelizationRuntimeData.CheckBounds(_voxelizationData.VoxelDensity, _voxelizationData.VoxelBounds, _voxelizationData.OverrideBoundsHeight))
			{
				if (Time.frameCount > 3) // hack for enter and exit in Play mode
					_needToReallocForUI = true;

				_voxelizationRuntimeData.SetParamsForApplyButton(_voxelizationData.VoxelDensity, _voxelizationData.VoxelBounds, _voxelizationData.OverrideBoundsHeight);
			}
		}

		private void CheckPrevValues()
		{
			if (_voxelizationData.AttachTo != _prevAttachTo)
			{
				_prevAttachTo = _voxelizationData.AttachTo;
				_voxelizationRuntimeData.OnReallocTextures?.Invoke();
			}
			if (_voxelizationData.LODMax != _prevLodMax)
			{
				_prevLodMax = _voxelizationData.LODMax;
				_voxelizationRuntimeData.FullVoxelization = true;
			}
		}

		private void CreateVoxelCamera()
		{
			if (_voxelizationRuntimeData.VoxelCamera != null)
			{
				_voxelizationRuntimeData.VoxelCamera.Initialize(this, _voxelizationData, _voxelizationRuntimeData, _debugData);
				return;
			}

			GameObject cameraGO = new GameObject(HTraceNames.HTRACE_VOXEL_CAMERA_NAME);
			//cameraGO.hideFlags = HideFlags.HideAndDontSave;//TODO: release uncomment
			// cameraGO.transform.parent = Camera.main.transform;
			// cameraGO.transform.localPosition = Vector3.zero;
			_voxelizationRuntimeData.VoxelCamera = cameraGO.AddComponent<VoxelCamera>();
			_voxelizationRuntimeData.VoxelCamera.Initialize(this, _voxelizationData, _voxelizationRuntimeData, _debugData);
		}

		private void CreateVoxelCullingCamera()
		{
			if (_voxelizationRuntimeData.CullingCamera != null)
			{
				_voxelizationRuntimeData.CullingCamera.Initialize(_voxelizationData, _voxelizationRuntimeData, _debugData);
				return;
			}

			GameObject cameraGO = new GameObject(HTraceNames.HTRACE_VOXEL_CULLING_CAMERA_NAME);
			cameraGO.transform.parent = _voxelizationRuntimeData.VoxelCamera.gameObject.transform;
			cameraGO.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
			//cameraGO.hideFlags = HideFlags.HideAndDontSave;//TODO: release uncomment
			// cameraGO.transform.parent = Camera.main.transform;
			// cameraGO.transform.localPosition = Vector3.zero;
			_voxelizationRuntimeData.CullingCamera = cameraGO.AddComponent<VoxelCullingCamera>();
			_voxelizationRuntimeData.CullingCamera.Initialize(_voxelizationData, _voxelizationRuntimeData, _debugData);
		}

		private void CreateVoxelOctantCamera()
		{
			if (_voxelizationRuntimeData.VoxelOctantCamera != null)
			{
				_voxelizationRuntimeData.VoxelOctantCamera.Initialize( _voxelizationData, _voxelizationRuntimeData, _debugData);
				return;
			}

			GameObject cameraGO = new GameObject(HTraceNames.HTRACE_VOXEL_OCTANT_CAMERA_NAME);
			cameraGO.transform.parent = _voxelizationRuntimeData.VoxelCamera.gameObject.transform;
			cameraGO.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
			//cameraGO.hideFlags = HideFlags.HideAndDontSave;//TODO: release uncomment
			// cameraGO.transform.parent = Camera.main.transform;
			// cameraGO.transform.localPosition = Vector3.zero;
			_voxelizationRuntimeData.VoxelOctantCamera = cameraGO.AddComponent<VoxelOctantCamera>();
			_voxelizationRuntimeData.VoxelOctantCamera.Initialize(_voxelizationData, _voxelizationRuntimeData, _debugData);
		}

		private void CreateHTraceCameraDirectional()
		{
			if (_voxelizationRuntimeData.FakeDirectionalCamera != null)
			{
				_voxelizationRuntimeData.FakeDirectionalCamera.Initialize(_voxelizationRuntimeData.VoxelCamera.Camera, this, _voxelizationData, _voxelizationRuntimeData, _debugData);
				return;
			}

			GameObject cameraFromFakeDirLightGo = new GameObject("HTraceDirectionalCameraHandler");
			//cameraFromFakeDirLightGo.hideFlags = HideFlags.HideAndDontSave; //TODO: release uncomment
			_voxelizationRuntimeData.FakeDirectionalCamera = cameraFromFakeDirLightGo.AddComponent<HTraceDirectionalCamera>();

			_voxelizationRuntimeData.FakeDirectionalCamera.Initialize(_voxelizationRuntimeData.VoxelCamera.Camera, this, _voxelizationData, _voxelizationRuntimeData, _debugData);
		}

		public bool PingVoxelsHandler(VoxelCamera voxelCamera)
		{
			return _voxelizationRuntimeData.VoxelCamera != voxelCamera;
		}

		public bool PingFakeDirLight(HTraceDirectionalCamera camera)
		{
			return _voxelizationRuntimeData.FakeDirectionalCamera != camera;
		}

		private void OnDestroy()
		{
			if (_voxelizationRuntimeData != null && _voxelizationRuntimeData.FakeDirectionalCamera != null)
				_voxelizationRuntimeData.FakeDirectionalCamera = null;
			
			Release();
		}

		internal void Release()
		{
			_voxelizationPassStaggered?.Release();
			_voxelizationPassConstant?.Release();
			_voxelizationPassPartial?.Release();
			
			if (_voxelizationRuntimeData != null)
				_voxelizationRuntimeData.OnReallocTextures -= ReallocTextures;
		}
	}
}
