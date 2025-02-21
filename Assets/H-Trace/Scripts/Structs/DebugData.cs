using System;
using H_Trace.Scripts.Globals;
using UnityEngine;

namespace H_Trace.Scripts.Structs
{
	[Serializable]
	internal class DebugData
	{
		[SerializeField]
		private bool _enableDebug = true;

		public bool EnableDebug
		{
			get { return _enableDebug; }
			set { _enableDebug = value; }
		}
		
		[SerializeField]
		private bool _attachToSceneCamera = true;

		public bool AttachToSceneCamera
		{
			get { return _attachToSceneCamera; }
			set { _attachToSceneCamera = value; }
		}
		
		public Camera CameraForTests;

		[SerializeField]
		private bool _enableCamerasVisualization = false;

		public bool EnableCamerasVisualization
		{
			get { return _enableCamerasVisualization; }
			set { _enableCamerasVisualization = value; }
		}

		public bool TestCheckbox = false;
		
		public LayerMask HTraceLayer = ~0;
		public HInjectionPoint HInjectionPoint = HInjectionPoint.AfterOpaqueDepthAndNormal;
	}
}
