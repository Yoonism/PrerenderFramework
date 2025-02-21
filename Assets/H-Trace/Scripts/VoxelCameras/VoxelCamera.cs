using H_Trace.Scripts.PassHandlers;
using H_Trace.Scripts.Structs;
using H_Trace.Scripts.Globals;
using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace H_Trace.Scripts.VoxelCameras
{
	[ExecuteInEditMode]
	internal class VoxelCamera : MonoBehaviour
	{
		public Camera Camera
		{
			get { return _camera; }
		}

		private Camera                  _camera;
		private VoxelsHandler           _voxelsHandler;
		private VoxelizationRuntimeData _voxelizationRuntimeData;
		private VoxelizationData        _voxelizationData;
		private DebugData _debugData;

		private bool    _dirtyBounds = false;
		private Vector3 _prevVoxelBounds;
		private Vector3 _rememberPos;

		public void Initialize(VoxelsHandler voxelsHandler, VoxelizationData voxelizationData, VoxelizationRuntimeData voxelizationRuntimeData, DebugData debugData)
		{
			_voxelizationRuntimeData = voxelizationRuntimeData;
			_voxelizationData        = voxelizationData;
			_debugData        = debugData;
			_voxelsHandler           = voxelsHandler;
			CreateCamera();
			ExecuteUpdate(null);
			_voxelizationRuntimeData.OnReallocTextures += UpdateCameraFromUI;

			_prevVoxelBounds = voxelizationData.ExactData.Bounds;
		}

		public void ExecuteUpdate(Camera ctxCamera)
		{
			if (_dirtyBounds == true)
			{
				_dirtyBounds     = false;
				_prevVoxelBounds = _voxelizationData.ExactData.Bounds;
			}

			_camera.cullingMask      = ~0; //voxelizationData.VoxelizationMask;
			_camera.orthographic     = true;
			_camera.farClipPlane     = .5f * _voxelizationData.ExactData.Bounds.z;
			_camera.nearClipPlane    = -.5f * _voxelizationData.ExactData.Bounds.z;
			_camera.orthographicSize = .5f * _prevVoxelBounds.x;
			_camera.aspect           = (.5f * _prevVoxelBounds.x) / (.5f * _prevVoxelBounds.x);
			
			if (true)
			{
				_voxelizationData.ExactData.PreviousVoxelCameraPosition = new Vector3(_camera.transform.position.x, _camera.transform.position.y, _camera.transform.position.z);
			}

			if (_voxelizationData.AttachTo != null)
			{
				AttachedCameraTranslate(ctxCamera);
			}
			else
			{
				GroundLevelTranslate();
				_camera.transform.position = _camera.transform.position.OptimizeForVoxelization(_voxelizationData.ExactData);
			}
		}

		private void AttachedCameraTranslate(Camera ctxCamera)
		{
			Transform attachToTransform = _voxelizationData.AttachTo;
#if UNITY_EDITOR
			if (ctxCamera != null && ctxCamera.cameraType == CameraType.SceneView && _debugData.AttachToSceneCamera == true)
			{
				attachToTransform = SceneView.lastActiveSceneView.camera.transform;
			}
#endif
			_camera.transform.parent      =  attachToTransform;
			_camera.transform.rotation    =  Quaternion.identity;
			_camera.transform.eulerAngles += new Vector3(-90f, 0, 180f);

			switch (_voxelizationData.VoxelizationUpdateMode)
			{
				//todo: stuggered uncomment
				// case VoxelizationUpdateMode.Staggered:
				// 	if (_voxelizationRuntimeData.FrameCount % 8 == 0)
				// 	{
				// 		_camera.transform.localPosition = Vector3.zero;
				// 		CenterShiftTranslate();
				// 		GroundLevelTranslate();
				// 		_camera.transform.position = _camera.transform.position.OptimizeForVoxelization(_voxelizationData.ExactData);
				// 		
				// 		_rememberPos = _camera.transform.position;
				// 	}
				// 	else
				// 	{
				// 		_camera.transform.position = _rememberPos;
				// 	}
				// 	break;
				case VoxelizationUpdateMode.Constant:
					_camera.transform.localPosition = Vector3.zero;

					CenterShiftTranslate(attachToTransform);
					GroundLevelTranslate();
					_camera.transform.position = _camera.transform.position.OptimizeForVoxelization(_voxelizationData.ExactData);
					break;
				case VoxelizationUpdateMode.Partial:
					
					_voxelizationRuntimeData.OctantIndex = _voxelizationRuntimeData.OctantIndex.Next();
					
					if (_voxelizationRuntimeData.FrameCount % HConstants.OCTANTS_FRAMES_LENGTH == 0) // || _voxelizationRuntimeData.FullVoxelization)
					{
						_voxelizationRuntimeData.OctantIndex = OctantIndex.OctantA;
						_camera.transform.localPosition      = Vector3.zero;

						CenterShiftTranslate(attachToTransform);
						GroundLevelTranslate();
						_camera.transform.position = _camera.transform.position.OptimizeForVoxelization(_voxelizationData.ExactData);

						_voxelizationRuntimeData.OffsetAxisIndex = CalculateOffsetPositionAndTargetAxis();

						if (_voxelizationRuntimeData.CullingCamera != null && _voxelizationRuntimeData.VoxelOctantCamera != null && _voxelizationRuntimeData.FakeDirectionalCamera != null) //first frame exceprion
						{
							_voxelizationRuntimeData.VoxelOctantCamera.UpdateCamera();
							_voxelizationRuntimeData.CullingCamera.UpdateCamera();
							//_voxelizationRuntimeData.FakeDirectionalCamera.UpdateCamera(); we do it every frame in ExecuteUpdate()
						}
					}
					else
					{
						_camera.transform.position = _rememberPos;
					}
					break;
			}
			
			if (_voxelizationRuntimeData.CullingCamera != null && _voxelizationRuntimeData.VoxelOctantCamera != null && _voxelizationRuntimeData.FakeDirectionalCamera != null) //first frame exceprion
			{
				// Debug.Log(_voxelizationRuntimeData.OctantIndex);
				_voxelizationRuntimeData.VoxelOctantCamera.ExecuteUpdate();
				_voxelizationRuntimeData.CullingCamera.ExecuteUpdate();
				_voxelizationRuntimeData.FakeDirectionalCamera.ExecuteUpdate();
			} 
		}

		private OffsetAxisIndex CalculateOffsetPositionAndTargetAxis()
		{
			Vector3 offsetWorldPosition = _rememberPos;
			_rememberPos = _camera.transform.position;

			offsetWorldPosition = _rememberPos - offsetWorldPosition;

			_voxelizationRuntimeData.OffsetWorldPosition = new OffsetWorldPosition(
				_voxelizationRuntimeData.OffsetAxisIndex == OffsetAxisIndex.AxisXPos ? 0.0f : _voxelizationRuntimeData.OffsetWorldPosition.AxisXPos,
				_voxelizationRuntimeData.OffsetAxisIndex == OffsetAxisIndex.AxisYPos ? 0.0f : _voxelizationRuntimeData.OffsetWorldPosition.AxisYPos,
				_voxelizationRuntimeData.OffsetAxisIndex == OffsetAxisIndex.AxisZPos ? 0.0f : _voxelizationRuntimeData.OffsetWorldPosition.AxisZPos,
				_voxelizationRuntimeData.OffsetAxisIndex == OffsetAxisIndex.AxisXNeg ? 0.0f : _voxelizationRuntimeData.OffsetWorldPosition.AxisXNeg,
				_voxelizationRuntimeData.OffsetAxisIndex == OffsetAxisIndex.AxisYNeg ? 0.0f : _voxelizationRuntimeData.OffsetWorldPosition.AxisYNeg,
				_voxelizationRuntimeData.OffsetAxisIndex == OffsetAxisIndex.AxisZNeg ? 0.0f : _voxelizationRuntimeData.OffsetWorldPosition.AxisZNeg
			);

			_voxelizationRuntimeData.OffsetWorldPosition += new OffsetWorldPosition(
				offsetWorldPosition.x > Mathf.Epsilon ? offsetWorldPosition.x : 0f,
				offsetWorldPosition.y > Mathf.Epsilon ? offsetWorldPosition.y : 0f,
				offsetWorldPosition.z > Mathf.Epsilon ? offsetWorldPosition.z : 0f,
				offsetWorldPosition.x < Mathf.Epsilon ? -offsetWorldPosition.x : 0f,
				offsetWorldPosition.y < Mathf.Epsilon ? -offsetWorldPosition.y : 0f,
				offsetWorldPosition.z < Mathf.Epsilon ? -offsetWorldPosition.z : 0f
			);

			return _voxelizationRuntimeData.OffsetWorldPosition.MaxAxisOffset();
		}

		private void CenterShiftTranslate(Transform attachToTransform)
		{
			if (attachToTransform.GetComponent<Camera>() && Mathf.Abs(_voxelizationData.CenterShift) > 0.01f)
			{
				var forward = attachToTransform.forward;
				_camera.transform.position += new Vector3(forward.x, 0f, forward.z) * _voxelizationData.CenterShift;
			}
		}

		private void GroundLevelTranslate()
		{
			float height = _voxelizationData.ExactData.Bounds.z;
			if (_voxelizationData.GroundLevelEnable == true && (_camera.transform.position.y - height / 2) < _voxelizationData.GroundLevel) 
			{
				_camera.transform.position = new Vector3(_camera.transform.position.x, _voxelizationData.GroundLevel + height / 2,
					_camera.transform.position.z);
			}
		}

		private void UpdateCameraFromUI()
		{
			_dirtyBounds = true;
		}

		private void CreateCamera()
		{
			if (_camera == null)
			{
				_camera = gameObject.AddComponent<Camera>();
				_camera.aspect       = 1f;
				_camera.orthographic = true;
				_camera.enabled      = false;
				//_camera.hideFlags = HideFlags.HideInHierarchy;//TODO: release uncomment

				var HDCameraData = gameObject.AddComponent<HDAdditionalCameraData>();
			}
		}

		private void Update()
		{	
			if (_voxelsHandler == null || _voxelsHandler.PingVoxelsHandler(this))
			{
				DestroyImmediate(this.gameObject);
			}
		}

		private void OnDestroy()
		{
			if (_voxelizationRuntimeData != null)
				_voxelizationRuntimeData.OnReallocTextures -= UpdateCameraFromUI;
		}
	}
}
