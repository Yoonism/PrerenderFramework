using H_Trace.Scripts.Globals;
using H_Trace.Scripts.Infrastructure;
using H_Trace.Scripts.Passes;
using H_Trace.Scripts.PassHandlers;
using H_Trace.Scripts.Structs;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
#if UNITY_EDITOR
using UnityEditor;
using H_Trace.Scripts.PipelinesConfigurator;
using HTrace.Scripts.Patcher;
#endif

namespace H_Trace.Scripts
{
	[ExecuteInEditMode, DefaultExecutionOrder(100)]
	public class HTrace : MonoBehaviour, IPing
	{
		private GlobalIllumination _giOverrideComponent;
		private ScreenSpaceAmbientOcclusion _ambientOcclusionComponent;
		private Volume _volumeComponent;

		private PassService _passService;

		private CustomPassObject _prePassObject;
		private CustomPassObject _voxelObject;
		private CustomPassObject _mainPassObject;
		private CustomPassObject _finalPassObject;

		private  VoxelsHandler _voxelsHandler;
		internal VoxelsHandler VoxelsHandler => _voxelsHandler;

		public GeneralData             GeneralData             = new GeneralData();
		public VoxelizationData        VoxelizationData        = new VoxelizationData();
		public ScreenSpaceLightingData ScreenSpaceLightingData = new ScreenSpaceLightingData();

		[SerializeField]
		private DebugData DebugData = new DebugData();
		internal VoxelizationRuntimeData VoxelizationRuntimeData = new VoxelizationRuntimeData();

		[SerializeField] private bool _globalSettingsTab      = true;
		[SerializeField] private bool _screenSpaceLightingTab = true;
		[SerializeField] private bool _wsgiTab                = true;
		[SerializeField] private bool _debugTab               = true;

		[SerializeField] private bool _showVoxelParams   = true;
		[SerializeField] private bool _showUpdateOptions = true;

		private  Light                 _prevDirLight;
		internal HDAdditionalLightData _additionalLightData;

		public bool Ping(CustomPassObject customPassObject)
		{
			return _passService.CustomPassObjectContains(customPassObject);
		}

		/// <summary>
		/// Forced scene voxelization
		/// </summary>
		public void VoxelizeNow()
		{
			VoxelizationRuntimeData.FullVoxelization = true;
		}

		private void OnEnable()
		{
			HExtensions.FillAttributeDictionary();
#if UNITY_EDITOR
			// Pipeleins feature will add after time
			//HRenderPipeline hRenderPipeline = HRenderer.CurrentHRenderPipeline;
			HPipelinesConfigurator.AlwaysIncludedShaders();
			
			HPatcher.RenderPipelineRuntimeResourcesPatch(true);
#endif
			InitComponents();
			if (HExtensions.PipelineSupportsSSGI)
				Shader.EnableKeyword(HTraceNames.KEYWORD_SWITCHER);
		}

		private void InitComponents()
		{
			_passService = new PassService(this);

			_prePassObject   = _passService.GetOrCreateCustomPassObject<PassHandler>(CustomPassInjectionPoint.AfterOpaqueDepthAndNormal,    passName: HTraceNames.HTRACE_PRE_PASS_NAME,   priority: 10,  typeof(HTracePrePass));
			_mainPassObject  = _passService.GetOrCreateCustomPassObject<PassHandler>(CustomPassInjectionPoint.AfterOpaqueDepthAndNormal, passName: HTraceNames.HTRACE_MAIN_PASS_NAME,  priority: -20, typeof(HTraceMainPass));
			_finalPassObject = _passService.GetOrCreateCustomPassObject<PassHandler>(CustomPassInjectionPoint.BeforePostProcess,            passName: HTraceNames.HTRACE_FINAL_PASS_NAME, priority: -30, typeof(HTraceFinalPass));
			_voxelObject = _passService.GetOrCreateCustomPassObject<VoxelsHandler>(CustomPassInjectionPoint.AfterOpaqueDepthAndNormal, passName: HTraceNames.HTRACE_VOXEL_PASS_NAME, priority: -10,
				typeof(VoxelizationPassStaggered), typeof(VoxelizationPassConstant), typeof(VoxelizationPassPartial));

			SetupVolume();
			
			((VoxelsHandler)_voxelObject.Handler).Initialize(VoxelizationData, VoxelizationRuntimeData, _voxelObject.CustomPass, DebugData);
			((HTracePrePass)_prePassObject.CustomPass[0]).Initialize(VoxelizationRuntimeData);
			((HTraceMainPass)_mainPassObject.CustomPass[0]).Initialize(DebugData, GeneralData, VoxelizationData, ScreenSpaceLightingData, VoxelizationRuntimeData);
			((HTraceFinalPass)_finalPassObject.CustomPass[0]).Initialize(GeneralData, VoxelizationRuntimeData);

			_voxelsHandler = (VoxelsHandler)_voxelObject.Handler;
		}

		private void Update()
		{
			
#if UNITY_EDITOR
			if (HExtensions.PipelineSupportsSSGI == true)
				Shader.EnableKeyword(HTraceNames.KEYWORD_SWITCHER);
			else
				Shader.DisableKeyword(HTraceNames.KEYWORD_SWITCHER);
			
			if (gameObject.name != HTraceNames.HTRACE_NAME) gameObject.name = HTraceNames.HTRACE_NAME;
#endif

			if (_prevDirLight != VoxelizationData.DirectionalLight)
			{
				_prevDirLight        = VoxelizationData.DirectionalLight;
				_additionalLightData = VoxelizationData.DirectionalLight != null ? VoxelizationData.DirectionalLight.gameObject.GetComponent<HDAdditionalLightData>() : null;
			}

			VoxelizationRuntimeData.EvaluateHitLighting = ScreenSpaceLightingData.EvaluateHitLighting
			                                              && HExtensions.PipelineSupportsScreenSpaceShadows
			                                              && _additionalLightData != null
			                                              && _additionalLightData.useScreenSpaceShadows == true;

			//_mainPassObject.CustomPassVolume.injectionPoint = (CustomPassInjectionPoint)DebugData.HInjectionPoint;
		}

		internal bool NeedToReallocForUI
		{
			get
			{
				if (_voxelObject == null || _voxelsHandler == null)
					return false;

				return _voxelsHandler.NeedToReallocForUI;
			}
		}

		internal void OnSceneGUI()
		{
#if UNITY_EDITOR
			_voxelsHandler.OnSceneGUI();
#endif
		}

		private void OnDisable()
		{
			((VoxelsHandler)_voxelObject.Handler).Release();
			((HTracePrePass)_prePassObject.CustomPass[0]).Release();
			((HTraceMainPass)_mainPassObject.CustomPass[0]).Release();
			((HTraceFinalPass)_finalPassObject.CustomPass[0]).Release();
			
			_passService.Cleanup();
			_passService = null;

			_volumeComponent.enabled = false;
			_giOverrideComponent = null;
			_ambientOcclusionComponent = null;
			
			VoxelizationRuntimeData.OnReallocTextures = null;

			Shader.DisableKeyword(HTraceNames.KEYWORD_SWITCHER);
		}

		#region SSGI override component ------------------------------------------------------------------------------------------------------

		private void SetupVolume()
		{
			CreateSSGIOverrideComponent();
			SetSSGIOverrideComponentSettings();
			ChangeObjectWithSerialization();
		}
		
		private void CreateSSGIOverrideComponent()
		{
			_volumeComponent = gameObject.GetComponent<Volume>();
			if (_volumeComponent == null)
			{
				_volumeComponent = gameObject.AddComponent<Volume>();
			}

			//_volumeComponent.hideFlags = HideFlags.HideInInspector; //TODO: release uncomment

			if (_volumeComponent.enabled == false)
				_volumeComponent.enabled = true;

			if (_volumeComponent.profile == null || _volumeComponent.profile.name.Contains("HTrace") == false)
			{
				//We can't crate it in runtime, because after build it will break.
				//it will call only in editor, but if someone changes it in runtime, we will override.
				_volumeComponent.profile = Resources.Load<VolumeProfile>("HTRaceWSGI/Volume Profile HTrace");
			}
			
			_volumeComponent.profile.TryGet(out _giOverrideComponent);
		}

		private void SetSSGIOverrideComponentSettings()
		{
			_volumeComponent.weight = 1;
			_volumeComponent.priority = 100;
#if UNITY_EDITOR
			_volumeComponent.runInEditMode = true;
#endif

			_giOverrideComponent.enable.overrideState = true;
			_giOverrideComponent.enable.value = true;
			_giOverrideComponent.tracing.overrideState = true;
			//_giOverrideComponent.tracing.value = RayCastingMode.RayMarching;
			//_giOverrideComponent.quality.value = (int) ScalableSettingLevelParameter.Level.High;
			_giOverrideComponent.quality.overrideState = true;
			_giOverrideComponent.quality.levelAndOverride = ((int)ScalableSettingLevelParameter.Level.Low, true);
		}

		private void ChangeObjectWithSerialization()
		{
#if UNITY_EDITOR
			//Global illumination
			SerializedObject giVolumeSerializedObject = new SerializedObject(_giOverrideComponent);

			var m_MaxRaySteps = giVolumeSerializedObject.FindProperty("m_MaxRaySteps");
			var m_OverrideState_m_MaxRaySteps = m_MaxRaySteps.FindPropertyRelative("m_OverrideState");
			var m_Value_m_MaxRaySteps = m_MaxRaySteps.FindPropertyRelative("m_Value");
			m_OverrideState_m_MaxRaySteps.boolValue = true;
			m_Value_m_MaxRaySteps.intValue = 0;
			// _giOverrideComponent.maxRaySteps = 0;

			var m_Denoise = giVolumeSerializedObject.FindProperty("m_Denoise");
			var m_OverrideState_m_Denoise = m_Denoise.FindPropertyRelative("m_OverrideState");
			var m_Value_m_Denoise = m_Denoise.FindPropertyRelative("m_Value");
			m_OverrideState_m_Denoise.boolValue = true;
			m_Value_m_Denoise.boolValue = false;
			// _giOverrideComponent.denoise = false;

			var m_DenoiseSS = giVolumeSerializedObject.FindProperty("m_DenoiseSS");
			var m_OverrideState_m_DenoiseSS = m_DenoiseSS.FindPropertyRelative("m_OverrideState");
			var m_Value_m_DenoiseSS = m_DenoiseSS.FindPropertyRelative("m_Value");
			m_OverrideState_m_DenoiseSS.boolValue = true;
			m_Value_m_DenoiseSS.boolValue = false;
			// _giOverrideComponent.denoiseSS = false;

			var m_FullResolution = giVolumeSerializedObject.FindProperty("m_FullResolution");
			var m_OverrideState_m_FullResolution = m_FullResolution.FindPropertyRelative("m_OverrideState");
			var m_Value_m_FullResolution = m_FullResolution.FindPropertyRelative("m_Value");
			m_OverrideState_m_FullResolution.boolValue = true;
			m_Value_m_FullResolution.boolValue = true;
			// _giOverrideComponent.fullResolution = false;

			var fullResolutionSS = giVolumeSerializedObject.FindProperty("fullResolutionSS");
			var m_OverrideState_fullResolutionSS = fullResolutionSS.FindPropertyRelative("m_OverrideState");
			var m_Value_fullResolutionSS = fullResolutionSS.FindPropertyRelative("m_Value");
			m_OverrideState_fullResolutionSS.boolValue = true;
			m_Value_fullResolutionSS.boolValue = true;
			// _giOverrideComponent.fullResolutionSS = false;

			var rayMiss = giVolumeSerializedObject.FindProperty("rayMiss");
			var m_OverrideState_fallbackHierarchy = rayMiss.FindPropertyRelative("m_OverrideState");
			var m_Value_fallbackHierarchy = rayMiss.FindPropertyRelative("m_Value");
			m_OverrideState_fallbackHierarchy.boolValue = true;
			m_Value_fallbackHierarchy.enumValueIndex = (int)RayMarchingFallbackHierarchy.None;
			// _giOverrideComponent.rayMiss.value = RayMarchingFallbackHierarchy.None;
			
			giVolumeSerializedObject.ApplyModifiedProperties();
			
			//Ambient Occlusion
			// SerializedObject aoVolumeSerializedObject = new SerializedObject(_ambientOcclusionComponent);
			// var intensity = aoVolumeSerializedObject.FindProperty("intensity");
			// var m_OverrideState_intensity = intensity.FindPropertyRelative("m_OverrideState");
			// var m_Value_intensity = intensity.FindPropertyRelative("m_Value");
			// m_OverrideState_intensity.boolValue = true;
			// m_Value_intensity.floatValue = 0f;
			//
			// aoVolumeSerializedObject.ApplyModifiedProperties();
#endif
		}

		#endregion SSGI override component ------------------------------------------------------------------------------------------------------

		#region UTILITIES --------------------------------------------------------------------------------------------------------------------------

#if UNITY_EDITOR
		
		private void OnTransformChildrenChanged()
		{
			foreach (Transform child in this.transform)
			{
				if (child.name == HTraceNames.HTRACE_PRE_PASS_NAME ||
				    child.name == HTraceNames.HTRACE_MAIN_PASS_NAME ||
				    child.name == HTraceNames.HTRACE_FINAL_PASS_NAME ||
				    child.name == HTraceNames.HTRACE_VOXEL_PASS_NAME ||
				    child.name == HTraceNames.HTRACE_VOXEL_CAMERA_NAME)
					continue;

				child.parent = null;
				Debug.Log($"Cann't add a \"{child.name}\" gameobject to H-Trace.");
			}
		}
#endif

		#endregion UTILITIES --------------------------------------------------------------------------------------------------------------------------
		
	}
}
