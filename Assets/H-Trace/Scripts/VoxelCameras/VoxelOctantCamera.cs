using H_Trace.Scripts.Structs;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace H_Trace.Scripts.VoxelCameras
{	
	[ExecuteInEditMode] 
	internal class VoxelOctantCamera : MonoBehaviour
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
			
			_voxelizationRuntimeData.OctantIndex = OctantIndex.None;
			CreateCamera();
		}

		public void UpdateCamera()
		{
			transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
		}

		public void ExecuteUpdate()
		{
			float scale = _voxelizationRuntimeData.OctantIndex == OctantIndex.DynamicObjects ? 1f : 2f;
			if (_camera.orthographicSize * scale < _voxelizationRuntimeData.OffsetWorldPosition.AxisXPos
			    || _camera.orthographicSize * scale < _voxelizationRuntimeData.OffsetWorldPosition.AxisXNeg)
				Debug.Log($"Culling camera size X axis less than OffsetWorldPosition.X");
			if (_camera.farClipPlane * scale < _voxelizationRuntimeData.OffsetWorldPosition.AxisYPos
			    || _camera.farClipPlane * scale < _voxelizationRuntimeData.OffsetWorldPosition.AxisYNeg)
				Debug.Log($"Culling camera size Y axis less than OffsetWorldPosition.Y");
			if (_camera.orthographicSize * _camera.aspect * scale < _voxelizationRuntimeData.OffsetWorldPosition.AxisZPos
			    || _camera.orthographicSize * _camera.aspect * scale < _voxelizationRuntimeData.OffsetWorldPosition.AxisZNeg)
				Debug.Log($"Culling camera size Y axis less than OffsetWorldPosition.Y");

			SetParams(_voxelizationRuntimeData.OctantIndex);
			OctantTransformCamera();
		}

		private void OctantTransformCamera()
		{
			transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

			switch (_voxelizationRuntimeData.OffsetAxisIndex)
			{
				case OffsetAxisIndex.AxisXPos:
					MoveAxisX(1f);
					break;
				case OffsetAxisIndex.AxisXNeg:
					MoveAxisX(-1f);
					break;
				case OffsetAxisIndex.AxisYPos:
					MoveAxisY(1f);
					break;
				case OffsetAxisIndex.AxisYNeg:
					MoveAxisY(-1f);
					break;
				case OffsetAxisIndex.AxisZPos:
					MoveAxisZ(1f);
					break;
				case OffsetAxisIndex.AxisZNeg:
					MoveAxisZ(-1f);
					break;
			}
		}

		private void MoveAxisX(float offset)
		{
			Vector3 finalPos = Vector3.zero;
			switch (_voxelizationRuntimeData.OctantIndex)
			{
				case OctantIndex.OctantA:
					finalPos.x += -offset * _voxelizationData.ExactData.Bounds.x / 4;
					finalPos.y += -_voxelizationData.ExactData.Bounds.y / 4;
					finalPos.z += _voxelizationData.ExactData.Bounds.z / 4;
					break;
				case OctantIndex.OctantB:
					finalPos.x += -offset * _voxelizationData.ExactData.Bounds.x / 4;
					finalPos.y += _voxelizationData.ExactData.Bounds.y / 4;
					finalPos.z += _voxelizationData.ExactData.Bounds.z / 4;
					break;
				case OctantIndex.OctantC:
					finalPos.x += -offset * _voxelizationData.ExactData.Bounds.x / 4;
					finalPos.y += -_voxelizationData.ExactData.Bounds.y / 4;
					finalPos.z += -_voxelizationData.ExactData.Bounds.z / 4;
					break;
				case OctantIndex.OctantD:
					finalPos.x += -offset * _voxelizationData.ExactData.Bounds.x / 4;
					finalPos.y += _voxelizationData.ExactData.Bounds.y / 4;
					finalPos.z += -_voxelizationData.ExactData.Bounds.z / 4;
					break;
				case OctantIndex.DynamicObjects:
					break;
			}
			
			transform.localPosition += finalPos;
		}

		private void MoveAxisY(float offset)
		{
			Vector3 finalPos = Vector3.zero;
			switch (_voxelizationRuntimeData.OctantIndex)
			{
				case OctantIndex.OctantA:
					finalPos.x += _voxelizationData.ExactData.Bounds.x / 4;
					finalPos.y += -_voxelizationData.ExactData.Bounds.y / 4;
					finalPos.z += offset * _voxelizationData.ExactData.Bounds.z / 4;
					break;
				case OctantIndex.OctantB:
					finalPos.x += _voxelizationData.ExactData.Bounds.x / 4;
					finalPos.y += _voxelizationData.ExactData.Bounds.y / 4;
					finalPos.z += offset * _voxelizationData.ExactData.Bounds.z / 4;
					break;
				case OctantIndex.OctantC:
					finalPos.x += -_voxelizationData.ExactData.Bounds.x / 4;
					finalPos.y += -_voxelizationData.ExactData.Bounds.y / 4;
					finalPos.z += offset * _voxelizationData.ExactData.Bounds.z / 4;
					break;
				case OctantIndex.OctantD:
					finalPos.x += -_voxelizationData.ExactData.Bounds.x / 4;
					finalPos.y += _voxelizationData.ExactData.Bounds.y / 4;
					finalPos.z += offset * _voxelizationData.ExactData.Bounds.z / 4;
					break;
				case OctantIndex.DynamicObjects:
					break;
			}

			transform.localPosition += finalPos;
		}

		private void MoveAxisZ(float offset)
		{
			Vector3 finalPos = Vector3.zero;
			switch (_voxelizationRuntimeData.OctantIndex)
			{
				case OctantIndex.OctantA:
					finalPos.x += -_voxelizationData.ExactData.Bounds.x / 4;
					finalPos.y += offset * _voxelizationData.ExactData.Bounds.y / 4;
					finalPos.z += _voxelizationData.ExactData.Bounds.z / 4;
					break;
				case OctantIndex.OctantB:
					finalPos.x += _voxelizationData.ExactData.Bounds.x / 4;
					finalPos.y += offset * _voxelizationData.ExactData.Bounds.y / 4;
					finalPos.z += _voxelizationData.ExactData.Bounds.z / 4;
					break;
				case OctantIndex.OctantC:
					finalPos.x += -_voxelizationData.ExactData.Bounds.x / 4;
					finalPos.y += offset * _voxelizationData.ExactData.Bounds.y / 4;
					finalPos.z += -_voxelizationData.ExactData.Bounds.z / 4;
					break;
				case OctantIndex.OctantD:
					finalPos.x += _voxelizationData.ExactData.Bounds.x / 4;
					finalPos.y += offset * _voxelizationData.ExactData.Bounds.y / 4;
					finalPos.z += -_voxelizationData.ExactData.Bounds.z / 4;
					break;
				case OctantIndex.DynamicObjects:
					break;
			}

			transform.localPosition += finalPos;
		}

#if UNITY_EDITOR
		
		private void OnDrawGizmos()
		{
			if (_debugData == null || _debugData.EnableCamerasVisualization == false || _voxelizationRuntimeData == null)
				return;

			var color = Gizmos.color;
			Gizmos.color = new Color(1, 1, 1, 0.2f);

			Vector3 position = _camera.transform.position;

			// Size = height / 2
			// Aspect = width / height
			//
			// height = 2f * size;
			// width = height * aspect;
			Vector3 size = new Vector3(2f * _camera.orthographicSize, _camera.farClipPlane * 2f, 2f * _camera.orthographicSize * _camera.aspect);

			Gizmos.DrawCube(position, size);

			Gizmos.color = color;
		}

#endif

		private void SetParams(OctantIndex octantIndex)
		{
			float scale = octantIndex == OctantIndex.DynamicObjects ? 1f : 2f;
			_camera.cullingMask      = ~0; //voxelizationData.VoxelizationMask;
			_camera.orthographic     = true;
			_camera.farClipPlane     = _voxelizationData.ExactData.Bounds.z / (2 * scale);
			_camera.nearClipPlane    = -_voxelizationData.ExactData.Bounds.z / (2 * scale);
			_camera.orthographicSize = .5f * _voxelizationData.ExactData.Bounds.x / scale;
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
	}
}
