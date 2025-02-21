using System.Collections.Generic;
using System.Linq;
using H_Trace.Scripts.Globals;
using H_Trace.Scripts.PassHandlers;
using H_Trace.Scripts.Structs;
using UnityEngine;
using Object = UnityEngine.Object;

namespace H_Trace.Scripts.VoxelCameras
{
	[ExecuteInEditMode]
	internal class HTraceDirectionalCamera : MonoBehaviour
	{
		private const float SQRT_OF_3 = 1.732f;

		public int ShadowResolution = 2048;

		private VoxelizationRuntimeData _voxelizationRuntimeData;
		private DebugData               _debugData;

		private Camera           _voxelCamera;
		private Camera           _directionalCamera;
		private VoxelizationData _voxelizationData;
		private VoxelsHandler    _voxelsHandler;

		private Vector3    _rememberPos;
		private Quaternion _rememberRot;
		private Light      _directionalLight;
		private bool       _needToRenderVoxels;

		public Camera GetDirectionalCamera
		{
			get { return _directionalCamera; }
		}

		public Camera Initialize(Camera voxelCamera, VoxelsHandler voxelsHandler, VoxelizationData voxelizationData, VoxelizationRuntimeData voxelizationRuntimeData, DebugData debugData)
		{
			_voxelsHandler           = voxelsHandler;
			_voxelizationRuntimeData = voxelizationRuntimeData;
			_voxelizationData        = voxelizationData;
			_debugData               = debugData;
			_voxelCamera             = voxelCamera;
			transform.parent         = voxelCamera.transform;

			IEnumerable<Light> lights = Object.FindObjectsOfType<Light>()
				.Where(lightComp => lightComp.type == LightType.Directional)
				.ToList();

			if (voxelizationData.DirectionalLight != null)
				_directionalLight = voxelizationData.DirectionalLight;

			if (_directionalLight == null && lights.Any())
			{
				_directionalLight = lights.FirstOrDefault(lightComp => lightComp.gameObject.activeSelf == true);
				if (_directionalLight == null)
					_directionalLight = lights.First();

				voxelizationData.DirectionalLight = _directionalLight;
			}

			gameObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
			CreateDirectionalCamera();

			return _directionalCamera;
		}

		private void CreateDirectionalCamera()
		{
			_directionalCamera = gameObject.GetComponentInChildren<Camera>();
			if (_directionalCamera == null)
			{
				GameObject cameraGo = new GameObject("Camera");
				cameraGo.transform.parent = this.gameObject.transform;
				cameraGo.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
				_directionalCamera = cameraGo.AddComponent<Camera>();
				//_directionalCamera.hideFlags = HideFlags.HideInHierarchy;//TODO: release uncomment
			}
			
			_directionalCamera.enabled = false;
			_directionalCamera.orthographic = true;
			_directionalCamera.cullingMask = ~0;
		}

		public void UpdateData(VoxelizationData voxelizationData)
		{
			_voxelizationData = voxelizationData;

			//_fakeDirectionalLight.SetBoxSpotSize(new Vector2(_voxelCamera.orthographicSize * 2 * SQRT_OF_3, _voxelCamera.orthographicSize * 2 * SQRT_OF_3));
			//_fakeDirectionalLight.range = _voxelCamera.orthographicSize * 2 * SQRT_OF_3;
		}

		public void ExecuteUpdate()
		{
			UpdateCamera();
			SetParams();
			if (_voxelizationData.VoxelizationUpdateMode == VoxelizationUpdateMode.Partial)
				OctantTransformCamera();
		}

		private void UpdateCamera()
		{
			if (_voxelizationData.DirectionalLight == null)
				return;
			//transform.position = _voxelCamera.transform.position - _voxelizationData.DirectionalLight.transform.forward * _voxelCamera.orthographicSize * SQRT_OF_3;
			//transform.rotation = _voxelizationData.DirectionalLight.transform.rotation;

			bool isTranslateNeeded = false;
			switch (_voxelizationData.VoxelizationUpdateMode)
			{
				//todo: stuggered uncomment
				// case VoxelizationUpdateMode.Staggered:
				// 	if (_voxelizationRuntimeData.FrameCount % 8 == 0)
				// 		isTranslateNeeded = true;
				// 	break;
				case VoxelizationUpdateMode.Constant:
					isTranslateNeeded = true;
					break;
				case VoxelizationUpdateMode.Partial:
					if (_voxelizationRuntimeData.FrameCount % HConstants.OCTANTS_FRAMES_LENGTH == 0)
						isTranslateNeeded = true;
					break;
			}
			
			if (isTranslateNeeded)
			{
				transform.position = _voxelCamera.transform.position - _voxelizationData.DirectionalLight.transform.forward * _voxelCamera.orthographicSize * SQRT_OF_3;
				transform.rotation = _voxelizationData.DirectionalLight.transform.rotation;
				_rememberPos = transform.position;
				_rememberRot = transform.rotation;
			}
			else
			{
				transform.position = _rememberPos;
				transform.rotation = _rememberRot;
			}
			
			_directionalCamera.transform.localPosition = Vector3.zero;

			//_fakeDirectionalLight.SetShadowResolution(ShadowResolution);

			//_cameraFakeDirLight.transform.localPosition = Vector3.zero;
			//_cameraFakeDirLight.transform.localPosition -= Vector3.forward * _voxelCamera.orthographicSize * SQRT_OF_3;
		}

		private void SetParams()
		{
			float scale = 0f;
			if (_voxelizationData.VoxelizationUpdateMode == VoxelizationUpdateMode.Partial)
				scale = _voxelizationRuntimeData.OctantIndex == OctantIndex.DynamicObjects ? 1f : 2f;
			else
				scale = 1f;
			
			float value = _voxelCamera.orthographicSize * SQRT_OF_3 * _voxelizationData.ExpandShadowmap;
			_directionalCamera.farClipPlane     = 1f * 2 * value;
			_directionalCamera.nearClipPlane    = 0f;
			_directionalCamera.orthographicSize = value / scale;
			_directionalCamera.aspect           = 1;
		}

		private void OctantTransformCamera()
		{
			_directionalCamera.transform.SetPositionAndRotation(_rememberPos, _rememberRot);
			
			Vector3 finalLocalPos = Vector3.zero;
			float   sizeOrtho     = _directionalCamera.orthographicSize;
			switch (_voxelizationRuntimeData.OctantIndex)
			{
				case OctantIndex.OctantA:
					finalLocalPos += sizeOrtho * -_directionalCamera.transform.right;
					finalLocalPos += sizeOrtho * _directionalCamera.transform.up;
					break;
				case OctantIndex.OctantB:
					finalLocalPos += sizeOrtho * _directionalCamera.transform.right;
					finalLocalPos += sizeOrtho * _directionalCamera.transform.up;
					break;
				case OctantIndex.OctantC:
					finalLocalPos += sizeOrtho * -_directionalCamera.transform.right;
					finalLocalPos += sizeOrtho * -_directionalCamera.transform.up;
					break;
				case OctantIndex.OctantD:
					finalLocalPos += sizeOrtho * _directionalCamera.transform.right;
					finalLocalPos += sizeOrtho * -_directionalCamera.transform.up;
					break;
				case OctantIndex.DynamicObjects:
					break;
			}

			_directionalCamera.transform.position += finalLocalPos;
		}

		private void Update()
		{
			if (_voxelsHandler == null || _voxelsHandler.PingFakeDirLight(this))
			{
				DestroyImmediate(this.gameObject);
			}
		}

#if UNITY_EDITOR

		private void OnDrawGizmos()
		{
			if (_debugData == null || _debugData.EnableCamerasVisualization == false || _voxelizationRuntimeData == null)
				return;

			var color = Gizmos.color;
			Gizmos.color = new Color(1, 0.92f, 0.016f, 0.2f);

			Vector3 posOffset = _directionalCamera.transform.forward * _directionalCamera.farClipPlane / 2;
			Vector3 position  = _directionalCamera.transform.position + posOffset;

			Matrix4x4 originalMatrix = Gizmos.matrix;
			Matrix4x4 rotationMatrix = transform.localToWorldMatrix;
			rotationMatrix = Matrix4x4.TRS(position, _directionalCamera.transform.rotation, _directionalCamera.transform.lossyScale);
			Gizmos.matrix  = rotationMatrix;

			// Size = height / 2
			// Aspect = width / height
			//
			// height = 2f * size;
			// width = height * aspect;
			Vector3 size = new Vector3(2f * _directionalCamera.orthographicSize, 2f * _directionalCamera.orthographicSize * _directionalCamera.aspect, _directionalCamera.farClipPlane);

			Gizmos.DrawCube(Vector3.zero, size);

			Gizmos.matrix = originalMatrix;
			Gizmos.color  = color;
		}
#endif
	}
}
