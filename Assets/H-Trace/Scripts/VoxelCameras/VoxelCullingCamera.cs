using H_Trace.Scripts.Structs;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace H_Trace.Scripts.VoxelCameras
{	
	[ExecuteInEditMode] 
	internal class VoxelCullingCamera : MonoBehaviour
	{
		public Camera Camera
		{
			get { return _camera; }
		}

		private Camera _camera;

		private VoxelizationData        _voxelizationData;
		private VoxelizationRuntimeData _voxelizationRuntimeData;
		private DebugData               _debugData;

		public void Initialize(VoxelizationData voxelizationData, VoxelizationRuntimeData voxelizationRuntimeData, DebugData debugData)
		{
			_voxelizationData        = voxelizationData;
			_voxelizationRuntimeData = voxelizationRuntimeData;
			_debugData               = debugData;
			CreateCamera();
			SetParams();
		}

		public void UpdateCamera()
		{
			transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
		}

		public void ExecuteUpdate()
		{
			CullingTransformCamera();
		}

		// Size = height / 2
		// Aspect = width / height
		//
		// height = 2f * size;
		// width = height * aspect;

		private void CullingTransformCamera()
		{
			transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

			float scale            = _voxelizationRuntimeData.OctantIndex == OctantIndex.DynamicObjects ? 2f : 1f;
			bool  isDynamicObjects = _voxelizationRuntimeData.OctantIndex == OctantIndex.DynamicObjects;
			switch (_voxelizationRuntimeData.OffsetAxisIndex)
			{
				case OffsetAxisIndex.AxisXPos:
					transform.localEulerAngles = new Vector3(0,                                          90f, 90);
					transform.localPosition    = new Vector3(-_voxelizationData.ExactData.Bounds.x / 4f, 0,   0);

					_camera.farClipPlane     = isDynamicObjects ? _voxelizationData.ExactData.Bounds.x : _voxelizationRuntimeData.OffsetWorldPosition.AxisXPos;
					_camera.orthographicSize = _voxelizationData.ExactData.Bounds.z * scale / 4f;
					_camera.aspect           = _voxelizationData.ExactData.Bounds.x * scale / 4f / _camera.orthographicSize;
					MoveAxisX(1f);
					break;
				case OffsetAxisIndex.AxisXNeg:
					transform.localEulerAngles = new Vector3(0,                                         -90f, 90);
					transform.localPosition    = new Vector3(_voxelizationData.ExactData.Bounds.x / 4f, 0,    0);

					_camera.farClipPlane     = isDynamicObjects ? _voxelizationData.ExactData.Bounds.x : _voxelizationRuntimeData.OffsetWorldPosition.AxisXNeg;
					_camera.orthographicSize = _voxelizationData.ExactData.Bounds.z * scale / 4f;
					_camera.aspect           = _voxelizationData.ExactData.Bounds.x * scale / 4f / _camera.orthographicSize;
					MoveAxisX(-1f);
					break;
				case OffsetAxisIndex.AxisYPos:
					transform.localEulerAngles = new Vector3(-180, 0, 0);
					transform.localPosition    = new Vector3(0,    0, _voxelizationData.ExactData.Bounds.z / 4f);

					_camera.farClipPlane     = isDynamicObjects ? _voxelizationData.ExactData.Bounds.z : _voxelizationRuntimeData.OffsetWorldPosition.AxisYPos;
					_camera.orthographicSize = _voxelizationData.ExactData.Bounds.x * scale / 4f;
					_camera.aspect           = 1f; //always quad
					MoveAxisY(1f);
					break;
				case OffsetAxisIndex.AxisYNeg:
					transform.localEulerAngles = new Vector3(0, 0, -180f);
					transform.localPosition    = new Vector3(0, 0, -_voxelizationData.ExactData.Bounds.z / 4f);

					_camera.farClipPlane     = isDynamicObjects ? _voxelizationData.ExactData.Bounds.z : _voxelizationRuntimeData.OffsetWorldPosition.AxisYNeg;
					_camera.orthographicSize = _voxelizationData.ExactData.Bounds.x * scale / 4f;
					_camera.aspect           = 1f; //always quad
					MoveAxisY(-1f);
					break;
				case OffsetAxisIndex.AxisZPos:
					transform.localEulerAngles = new Vector3(90f, 0,                                         0);
					transform.localPosition    = new Vector3(0,   _voxelizationData.ExactData.Bounds.x / 4f, 0);

					_camera.farClipPlane     = isDynamicObjects ? _voxelizationData.ExactData.Bounds.x : _voxelizationRuntimeData.OffsetWorldPosition.AxisZPos;
					_camera.orthographicSize = _voxelizationData.ExactData.Bounds.z * scale / 4f;
					_camera.aspect           = _voxelizationData.ExactData.Bounds.x * scale / 4f / _camera.orthographicSize;
					MoveAxisZ(1f);
					break;
				case OffsetAxisIndex.AxisZNeg:
					transform.localEulerAngles = new Vector3(-90f, 0,                                          0);
					transform.localPosition    = new Vector3(0,    -_voxelizationData.ExactData.Bounds.x / 4f, 0);

					_camera.farClipPlane     = isDynamicObjects ? _voxelizationData.ExactData.Bounds.x : _voxelizationRuntimeData.OffsetWorldPosition.AxisZNeg;
					_camera.orthographicSize = _voxelizationData.ExactData.Bounds.z * scale / 4f;
					_camera.aspect           = _voxelizationData.ExactData.Bounds.x * scale / 4f / _camera.orthographicSize;
					MoveAxisZ(-1f);
					break;
			}
		}

		private void MoveAxisX(float sign)
		{
			Vector3 finalPos = Vector3.zero;
			switch (_voxelizationRuntimeData.OctantIndex)
			{
				case OctantIndex.OctantA:
					finalPos.x += -sign * _voxelizationData.ExactData.Bounds.x / 4;
					finalPos.y += -_voxelizationData.ExactData.Bounds.y / 4;
					finalPos.z += _voxelizationData.ExactData.Bounds.z / 4;
					break;
				case OctantIndex.OctantB:
					finalPos.x += -sign * _voxelizationData.ExactData.Bounds.x / 4;
					finalPos.y += _voxelizationData.ExactData.Bounds.y / 4;
					finalPos.z += _voxelizationData.ExactData.Bounds.z / 4;
					break;
				case OctantIndex.OctantC:
					finalPos.x += -sign * _voxelizationData.ExactData.Bounds.x / 4;
					finalPos.y += -_voxelizationData.ExactData.Bounds.y / 4;
					finalPos.z += -_voxelizationData.ExactData.Bounds.z / 4;
					break;
				case OctantIndex.OctantD:
					finalPos.x += -sign * _voxelizationData.ExactData.Bounds.x / 4;
					finalPos.y += _voxelizationData.ExactData.Bounds.y / 4;
					finalPos.z += -_voxelizationData.ExactData.Bounds.z / 4;
					break;
				case OctantIndex.DynamicObjects:
					finalPos.x += -sign * _voxelizationData.ExactData.Bounds.x / 4;
					break;
			}

			transform.localPosition += finalPos;
		}

		private void MoveAxisY(float sign)
		{
			Vector3 finalPos = Vector3.zero;
			switch (_voxelizationRuntimeData.OctantIndex)
			{
				case OctantIndex.OctantA:
					finalPos.x += _voxelizationData.ExactData.Bounds.x / 4;
					finalPos.y += -_voxelizationData.ExactData.Bounds.y / 4;
					finalPos.z += sign * _voxelizationData.ExactData.Bounds.z / 4;
					break;
				case OctantIndex.OctantB:
					finalPos.x += _voxelizationData.ExactData.Bounds.x / 4;
					finalPos.y += _voxelizationData.ExactData.Bounds.y / 4;
					finalPos.z += sign * _voxelizationData.ExactData.Bounds.z / 4;
					break;
				case OctantIndex.OctantC:
					finalPos.x += -_voxelizationData.ExactData.Bounds.x / 4;
					finalPos.y += -_voxelizationData.ExactData.Bounds.y / 4;
					finalPos.z += sign * _voxelizationData.ExactData.Bounds.z / 4;
					break;
				case OctantIndex.OctantD:
					finalPos.x += -_voxelizationData.ExactData.Bounds.x / 4;
					finalPos.y += _voxelizationData.ExactData.Bounds.y / 4;
					finalPos.z += sign * _voxelizationData.ExactData.Bounds.z / 4;
					break;
				case OctantIndex.DynamicObjects:
		
					finalPos.z += sign * _voxelizationData.ExactData.Bounds.z / 4;
					break;
			}

			transform.localPosition += finalPos;
		}

		private void MoveAxisZ(float sign)
		{
			Vector3 finalPos = Vector3.zero;

			switch (_voxelizationRuntimeData.OctantIndex)
			{
				case OctantIndex.OctantA:
					finalPos.x += -_voxelizationData.ExactData.Bounds.x / 4;
					finalPos.y += sign * _voxelizationData.ExactData.Bounds.y / 4;
					finalPos.z += _voxelizationData.ExactData.Bounds.z / 4;
					break;
				case OctantIndex.OctantB:
					finalPos.x += _voxelizationData.ExactData.Bounds.x / 4;
					finalPos.y += sign * _voxelizationData.ExactData.Bounds.y / 4;
					finalPos.z += _voxelizationData.ExactData.Bounds.z / 4;
					break;
				case OctantIndex.OctantC:
					finalPos.x += -_voxelizationData.ExactData.Bounds.x / 4;
					finalPos.y += sign * _voxelizationData.ExactData.Bounds.y / 4;
					finalPos.z += -_voxelizationData.ExactData.Bounds.z / 4;
					break;
				case OctantIndex.OctantD:
					finalPos.x += _voxelizationData.ExactData.Bounds.x / 4;
					finalPos.y += sign * _voxelizationData.ExactData.Bounds.y / 4;
					finalPos.z += -_voxelizationData.ExactData.Bounds.z / 4;
					break;
				case OctantIndex.DynamicObjects:
				
					finalPos.y += sign * _voxelizationData.ExactData.Bounds.y / 4;
					break;
			}

			transform.localPosition += finalPos;
		}

		private void SetParams()
		{
			_camera.cullingMask      = ~0; //voxelizationData.VoxelizationMask;
			_camera.orthographic     = true;
			_camera.farClipPlane     = 1;
			_camera.nearClipPlane    = 0;
			_camera.orthographicSize = 0;
			_camera.aspect           = 1;
		}

		private void CreateCamera()
		{
			if (_camera == null)
			{
				_camera              = gameObject.AddComponent<Camera>();
				_camera.aspect       = 1f;
				_camera.orthographic = true;
				_camera.enabled      = false;
				//_camera.hideFlags = HideFlags.HideInHierarchy;//TODO: release uncomment

				var HDCameraData = gameObject.AddComponent<HDAdditionalCameraData>();
				HDCameraData.customRenderingSettings = true;
				FrameSettings frameSettings = HDCameraData.renderingPathCustomFrameSettings;
				HDCameraData.renderingPathCustomFrameSettings = frameSettings;
			}
		}

#if UNITY_EDITOR

		private void OnDrawGizmos()
		{
			if (_debugData == null || _debugData.EnableCamerasVisualization == false || _voxelizationRuntimeData == null)
				return;

			var color = Gizmos.color;

			Vector3 position = Vector3.zero;
			Vector3 size     = Vector3.zero;

			switch (_voxelizationRuntimeData.OffsetAxisIndex)
			{
				case OffsetAxisIndex.AxisXPos:
					position     = _camera.transform.position + new Vector3(-_voxelizationRuntimeData.OffsetWorldPosition.AxisXPos / 2f, 0, 0);
					size         = new Vector3(_voxelizationRuntimeData.OffsetWorldPosition.AxisXPos, 2f * _camera.orthographicSize, 2f * _camera.orthographicSize * _camera.aspect);
					Gizmos.color = new Color(1, 0, 0, 0.3f);
					break;
				case OffsetAxisIndex.AxisXNeg:
					position     = _camera.transform.position + new Vector3(_voxelizationRuntimeData.OffsetWorldPosition.AxisXNeg / 2f, 0, 0);
					size         = new Vector3(_voxelizationRuntimeData.OffsetWorldPosition.AxisXNeg, 2f * _camera.orthographicSize, 2f * _camera.orthographicSize * _camera.aspect);
					Gizmos.color = new Color(1, 0, 0, 0.3f);
					break;
				case OffsetAxisIndex.AxisYPos:
					position     = _camera.transform.position + new Vector3(0f, -_voxelizationRuntimeData.OffsetWorldPosition.AxisYPos / 2f, 0);
					size         = new Vector3(2f * _camera.orthographicSize, _voxelizationRuntimeData.OffsetWorldPosition.AxisYPos, 2f * _camera.orthographicSize * _camera.aspect);
					Gizmos.color = new Color(0, 1, 0, 0.3f);
					break;
				case OffsetAxisIndex.AxisYNeg:
					position     = _camera.transform.position + new Vector3(0f, _voxelizationRuntimeData.OffsetWorldPosition.AxisYPos / 2f, 0);
					size         = new Vector3(2f * _camera.orthographicSize, _voxelizationRuntimeData.OffsetWorldPosition.AxisYPos, 2f * _camera.orthographicSize * _camera.aspect);
					Gizmos.color = new Color(0, 1, 0, 0.3f);
					break;
				case OffsetAxisIndex.AxisZPos:
					position     = _camera.transform.position + new Vector3(0, 0, -_voxelizationRuntimeData.OffsetWorldPosition.AxisZPos / 2f);
					size         = new Vector3(2f * _camera.orthographicSize * _camera.aspect, 2f * _camera.orthographicSize, _voxelizationRuntimeData.OffsetWorldPosition.AxisZPos);
					Gizmos.color = new Color(0, 0, 1, 0.3f);
					break;
				case OffsetAxisIndex.AxisZNeg:
					position     = _camera.transform.position + new Vector3(0, 0, _voxelizationRuntimeData.OffsetWorldPosition.AxisZNeg / 2f);
					size         = new Vector3(2f * _camera.orthographicSize * _camera.aspect, 2f * _camera.orthographicSize, _voxelizationRuntimeData.OffsetWorldPosition.AxisZNeg);
					Gizmos.color = new Color(0, 0, 1, 0.3f);
					break;
			}

			Gizmos.DrawCube(position, size);

			Gizmos.color = color;
		}

#endif
	}
}
