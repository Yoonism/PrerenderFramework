#if UNITY_EDITOR
using H_Trace.Scripts;
using H_Trace.Scripts.Globals;
using H_Trace.Scripts.PipelinesConfigurator;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace H_Trace.Editor
{
	[CustomEditor(typeof(H_Trace.Scripts.HTrace))]
	internal class HTraceEditor : UnityEditor.Editor
	{
		SerializedProperty _globalSettingsTab;
		SerializedProperty _ssLightingTab;
		SerializedProperty _wsgiTab;
		SerializedProperty _debugTab;

		SerializedProperty _showVoxelParams;
		SerializedProperty _showUpdateOptions;

		private AnimBool AnimBoolGeneralTab;
		private AnimBool AnimBoolWSGITab;
		private AnimBool AnimBoolSsLightingTab;
		private AnimBool AnimBoolDebugTab;
		private AnimBool AnimBoolEMPTY;

		SerializedProperty GeneralData;
		SerializedProperty VoxelizationData;
		SerializedProperty ScreenSpaceLightingData;
		SerializedProperty DebugData;

		// Debug Tab
		SerializedProperty DebugModeWS;
		SerializedProperty AttachToSceneCamera;
		
		SerializedProperty EnableDebug;
		SerializedProperty CameraForTests;
		SerializedProperty EnableCamerasVisualization;
		SerializedProperty TestCheckbox;
		SerializedProperty HTraceLayer;
		SerializedProperty HInjectionPoint;
		
		// General Tab
		SerializedProperty RayCountMode;
		SerializedProperty RayLength;
		SerializedProperty Multibounce;

		// Voxelization Tab
		SerializedProperty VoxelizationMask;
		SerializedProperty VoxelizationUpdateMode;
		SerializedProperty AttachTo;
		SerializedProperty DirectionalLight;
		SerializedProperty ExpandShadowmap;
		SerializedProperty LodMax;

		SerializedProperty CenterShift;
		SerializedProperty VoxelDensity;
		SerializedProperty VoxelBounds;
		SerializedProperty OverrideBoundsHeightEnable;
		SerializedProperty OverrideBoundsHeight;
		SerializedProperty GroundLevelEnable;
		SerializedProperty GroundLevel;

		//Update Options
		SerializedProperty CulledObjectsMask;
		SerializedProperty ExpandCullFov;
		SerializedProperty ExpandCullRadius;
		SerializedProperty DynamicObjectsMask;

		SerializedProperty ExactBounds;
		SerializedProperty ExactResolution;

		// Screen space lighting Tab
		SerializedProperty EvaluateHitLighting;
		SerializedProperty DirectionalOcclusion;
		SerializedProperty OcclusionIntensity;

		private bool _showStatistic;

		private void OnEnable()
		{
			PropertiesRelative();

			AnimBoolGeneralTab = new AnimBool(_globalSettingsTab.boolValue);
			AnimBoolGeneralTab.valueChanged.RemoveAllListeners();
			AnimBoolGeneralTab.valueChanged.AddListener(Repaint);

			AnimBoolSsLightingTab = new AnimBool(_ssLightingTab.boolValue);
			AnimBoolSsLightingTab.valueChanged.RemoveAllListeners();
			AnimBoolSsLightingTab.valueChanged.AddListener(Repaint);

			AnimBoolWSGITab = new AnimBool(_wsgiTab.boolValue);
			AnimBoolWSGITab.valueChanged.RemoveAllListeners();
			AnimBoolWSGITab.valueChanged.AddListener(Repaint);

			AnimBoolDebugTab = new AnimBool(_debugTab.boolValue);
			AnimBoolDebugTab.valueChanged.RemoveAllListeners();
			AnimBoolDebugTab.valueChanged.AddListener(Repaint);

			AnimBoolEMPTY = new AnimBool(false);
		}

		//https://docs.unity3d.com/ScriptReference/IMGUI.Controls.PrimitiveBoundsHandle.DrawHandle.html
		private readonly BoxBoundsHandle _BoundsHandle = new BoxBoundsHandle();

		protected virtual void OnSceneGUI()
		{
			H_Trace.Scripts.HTrace hTrace = (H_Trace.Scripts.HTrace)target;

			if (hTrace.VoxelsHandler == null || hTrace.VoxelsHandler?.BoundsGizmo == null) // it may not created yet
				return;
			// copy the target object's data to the handle
			_BoundsHandle.center = hTrace.VoxelsHandler.BoundsGizmo.center;
			_BoundsHandle.size   = hTrace.VoxelsHandler.BoundsGizmo.size;

			hTrace.VoxelsHandler.BoundsGizmoFromUI = hTrace.VoxelsHandler.BoundsGizmo;

			// draw the handle
			EditorGUI.BeginChangeCheck();
			_BoundsHandle.DrawHandle();
			if (EditorGUI.EndChangeCheck())
			{
				//Undo.RecordObject(hTrace, "Change voxel's bounds");

				// copy the handle's updated data back to the target object
				Bounds newBounds = new Bounds();
				newBounds.center                       = _BoundsHandle.center;
				newBounds.size                         = _BoundsHandle.size;
				hTrace.VoxelsHandler.BoundsGizmoFromUI = newBounds;

				//hTrace._boundsGizmoFromUIEdited = true;
				hTrace.OnSceneGUI();
			}
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			UpdateStandartStyles();
			// base.OnInspectorGUI();
			//return;

			AnimBoolEMPTY = new AnimBool(false);
			H_Trace.Scripts.HTrace trgt = (H_Trace.Scripts.HTrace)target;

			Color standartBackgroundColor = GUI.backgroundColor;
			Color standartColor           = GUI.color;

			if (HExtensions.PipelineSupportsSSGI == false)
			{
				EditorGUILayout.HelpBox("Screen Space Global Illumination disabled.\nEdit -> Project Settings -> Quality HDRP -> Lighting -> Screen Space Global Illumination", MessageType.Error);
			}

			using (new HEditorUtils.FoldoutScope(AnimBoolGeneralTab, out var shouldDraw, HEditorStyles.GlobalSettingsContent.text))
			{
				_globalSettingsTab.boolValue = shouldDraw;
				if (shouldDraw)
				{
					int rayCountIndex = RayCountMode.enumValueIndex;
					EditorGUILayout.PropertyField(RayCountMode, HEditorStyles.RayCountModeContent);
					if (rayCountIndex != RayCountMode.enumValueIndex)
					{
						trgt.GeneralData.RayCountMode = (RayCountMode)RayCountMode.enumValueIndex;
					}
					RayLength.intValue = EditorGUILayout.IntSlider(HEditorStyles.RayLengthContent, RayLength.intValue, 0, 100);
					EditorGUILayout.PropertyField(Multibounce, HEditorStyles.MultibounceContent);
				}
			}

			if (trgt.NeedToReallocForUI == true)
			{
				GUI.backgroundColor = HEditorStyles.warningBackgroundColor;
				//GUI.color           = HEditorStyles.warningColor;
			}

			using (new HEditorUtils.FoldoutScope(AnimBoolWSGITab, out var shouldDraw, HEditorStyles.VoxelizationContent.text))
			{
				_wsgiTab.boolValue = shouldDraw;

				GUI.backgroundColor = standartBackgroundColor;
				//GUI.color           = standartColor;
				if (shouldDraw)
				{
					EditorGUILayout.PropertyField(VoxelizationMask,       HEditorStyles.VoxelizationMaskContent);
					EditorGUILayout.PropertyField(VoxelizationUpdateMode, HEditorStyles.VoxelizationUpdateTypeContent);

					EditorGUILayout.PropertyField(AttachTo, HEditorStyles.AttachToContent);

					if (AttachTo.objectReferenceValue != null)
					{
						if (((Transform)AttachTo.objectReferenceValue).gameObject.GetComponent<Camera>() != null)
							EditorGUILayout.PropertyField(CenterShift, HEditorStyles.CenterShiftContent);
					}

					if (AttachTo.objectReferenceValue == null)
					{
						EditorGUILayout.HelpBox("Set object to follow voxelization camera", MessageType.Error);
					}

					EditorGUILayout.PropertyField(DirectionalLight, HEditorStyles.DirectionalLightContent);

					if (DirectionalLight.objectReferenceValue == null)
					{
						EditorGUILayout.HelpBox("Directional Light is not set", MessageType.Error);
					}

					EditorGUILayout.Slider(ExpandShadowmap, 1.0f, 3.0f, HEditorStyles.ExpandShadowmapContent);
					LodMax.intValue = EditorGUILayout.IntSlider(HEditorStyles.MaximumLodContent, LodMax.intValue, 0, HConstants.MAX_LOD_LEVEL);
					
					EditorGUILayout.Space(3f);

					if (trgt.NeedToReallocForUI == true)
					{
						GUI.backgroundColor = HEditorStyles.warningBackgroundColor;
						GUI.color           = HEditorStyles.warningColor;
					}

					_showVoxelParams.boolValue           = EditorGUILayout.BeginFoldoutHeaderGroup(_showVoxelParams.boolValue, "Parameters");
					GUI.backgroundColor                  = standartBackgroundColor;
					GUI.color                            = standartColor;

					if (_showVoxelParams.boolValue)
					{
						EditorGUI.indentLevel++;

						EditorGUILayout.Slider(VoxelDensity, 0.0f, 1.0f, HEditorStyles.VoxelDensityContent);

						VoxelBounds.intValue = EditorGUILayout.IntSlider(HEditorStyles.VoxelBoundsContent, VoxelBounds.intValue, 1, HConfig.MAX_VOXEL_BOUNDS);

						EditorGUILayout.BeginHorizontal();
						//EditorGUILayout.PropertyField(OverrideBoundsHeightEnable, HEditorStyles.OverrideBoundsHeightEnableContent);
						OverrideBoundsHeightEnable.boolValue = EditorGUILayout.ToggleLeft(
							OverrideBoundsHeightEnable.boolValue == false ? HEditorStyles.OverrideBoundsHeightEnableContent2 : GUIContent.none,
							OverrideBoundsHeightEnable.boolValue, GUILayout.MaxWidth(OverrideBoundsHeightEnable.boolValue == false ? 160f : 30f));
						if (OverrideBoundsHeightEnable.boolValue == true)
						{
							OverrideBoundsHeight.intValue = VoxelBounds.intValue < OverrideBoundsHeight.intValue ? VoxelBounds.intValue : OverrideBoundsHeight.intValue;
							OverrideBoundsHeight.intValue = OverrideBoundsHeight.intValue < 1 ? 1 : OverrideBoundsHeight.intValue;
							OverrideBoundsHeight.intValue = EditorGUILayout.IntSlider(HEditorStyles.OverrideBoundsHeightEnableContent, OverrideBoundsHeight.intValue, 1, VoxelBounds.intValue);
						}
						else
						{
							OverrideBoundsHeight.intValue = VoxelBounds.intValue;
						}

						EditorGUILayout.EndHorizontal();

						EditorGUILayout.BeginHorizontal();
						//EditorGUILayout.PropertyField(GroundLevelEnable, HEditorStyles.GroundLevelEnableContent);
						GroundLevelEnable.boolValue = EditorGUILayout.ToggleLeft(GroundLevelEnable.boolValue == false ? HEditorStyles.GroundLevelEnableContent2 : GUIContent.none,
							GroundLevelEnable.boolValue, GUILayout.MaxWidth(GroundLevelEnable.boolValue == false ? 160f : 30f));
						if (GroundLevelEnable.boolValue == true)
						{
							EditorGUILayout.PropertyField(GroundLevel, HEditorStyles.GroundLevelEnableContent);
						}

						EditorGUILayout.EndHorizontal();

						if (trgt.NeedToReallocForUI == true)
						{
							GUI.backgroundColor = HEditorStyles.warningBackgroundColor;
							GUI.color           = HEditorStyles.warningColor;
						}

						EditorGUILayout.BeginHorizontal();
						
						if (GUILayout.Button("Apply Parameters", HEditorStyles.standartButton))
						{
							trgt.VoxelizationRuntimeData.OnReallocTextures?.Invoke();
						}

						GUI.backgroundColor = standartBackgroundColor;
						GUI.color           = standartColor;

						if (GUILayout.Button(_showStatistic ? "Hide Statistics" : "Show Statistics"))
						{
							_showStatistic = !_showStatistic;
						}

						EditorGUILayout.EndHorizontal();

						if (_showStatistic)
						{
							EditorGUILayout.Space(10f);

							Vector3Int voxelResolution = HMath.CalculateVoxelResolution_UI(VoxelBounds.intValue, VoxelDensity.floatValue, OverrideBoundsHeightEnable.boolValue, OverrideBoundsHeight.intValue);
							//EditorGUILayout.LabelField($"Voxel Resolution:  Width: {(int)voxelResolution.x}   Depth: {(int)voxelResolution.y}   Height: {(int)voxelResolution.z}");

							float voxelSize = HMath.CalculateVoxelSizeInCM_UI(VoxelBounds.intValue, VoxelDensity.floatValue);
							//EditorGUILayout.LabelField($"Voxel Size:  Color {voxelSize:0.0} cm.   Position {(voxelSize / 2):0.0} cm.");

							//float texturesSizeInMB = HMath.TexturesSizeInMB_UI(VoxelBounds.intValue, VoxelDensity.floatValue, OverrideBoundsHeightEnable.boolValue, OverrideBoundsHeight.intValue);
							float texturesSizeInMB = HMath.TexturesSizeInMB_UI(ExactResolution.vector3IntValue, (VoxelizationUpdateMode)VoxelizationUpdateMode.enumValueIndex);
							//EditorGUILayout.LabelField($"GPU Memory Consumption:  {texturesSizeInMB:0.0} MB", myStyle);

							GUIStyle myStyle = GUI.skin.GetStyle("HelpBox");
							myStyle.richText = true;
							myStyle.fontSize = 12;

							Vector3 voxelsSize = new Vector3(ExactBounds.vector3Value.x / ExactResolution.vector3IntValue.x, ExactBounds.vector3Value.y / ExactResolution.vector3IntValue.y,
								ExactBounds.vector3Value.z / ExactResolution.vector3IntValue.z) * 100.0f;
							EditorGUILayout.HelpBox(
								$"Voxel Resolution:  Width: {ExactResolution.vector3IntValue.x}   Depth: {ExactResolution.vector3IntValue.y}   Height: {ExactResolution.vector3IntValue.z}\n" +
								$"Voxel Size:  Color {voxelsSize.x:0.0} cm.   Position {(voxelsSize.x / 2):0.0} cm.\n" +
								$"GPU Memory Consumption:  {texturesSizeInMB:0.00} MB",
								MessageType.None);
						}

						EditorGUI.indentLevel--;
						EditorGUILayout.Space(5f);
					}

					GUI.backgroundColor = standartBackgroundColor;
					//GUI.color           = standartColor;

					EditorGUILayout.EndFoldoutHeaderGroup();
					EditorGUILayout.Space(3f);

					if ((VoxelizationUpdateMode)VoxelizationUpdateMode.enumValueIndex == Scripts.Globals.VoxelizationUpdateMode.Partial)
					{
					
						_showUpdateOptions.boolValue = EditorGUILayout.BeginFoldoutHeaderGroup(_showUpdateOptions.boolValue, "Update Options");
					
						if (_showUpdateOptions.boolValue)
						{
							EditorGUI.indentLevel++;

							// Staggered = 0
							// Constant = 1
							// Partial = 2
							//todo: after release add
							// if (VoxelizationUpdateMode.enumValueIndex == 0 || VoxelizationUpdateMode.enumValueIndex == 1)
							// {
							// 	EditorGUILayout.PropertyField(CulledObjectsMask, HEditorStyles.CulledObjectsMaskContent);
							// 	ExpandCullFov.intValue = EditorGUILayout.IntSlider(HEditorStyles.ExpandCullFovContent, ExpandCullFov.intValue, 0, 20);
							// 	EditorGUILayout.Slider(ExpandCullRadius, 0.0f, 3.0f, HEditorStyles.ExpandCullRadiusContent);
							// }

							EditorGUILayout.PropertyField(DynamicObjectsMask, HEditorStyles.DynamicObjectsMaskContent);

							EditorGUI.indentLevel--;
							EditorGUILayout.Space(5f);
						}
					}

					EditorGUILayout.EndFoldoutHeaderGroup();
				}
			}

			GUI.backgroundColor = standartBackgroundColor;
			//GUI.color           = standartColor;
			using (new HEditorUtils.FoldoutScope(AnimBoolSsLightingTab, out var shouldDraw, HEditorStyles.ScreenSpaceLightingContent.text))
			{
				_ssLightingTab.boolValue = shouldDraw;
				if (shouldDraw)
				{
					EditorGUILayout.PropertyField(EvaluateHitLighting,  HEditorStyles.EvaluateHitLightingContent);
					if (EvaluateHitLighting.boolValue == true
					    && (HExtensions.PipelineSupportsScreenSpaceShadows == false || trgt._additionalLightData == null || trgt._additionalLightData.useScreenSpaceShadows == false))
					{
						EditorGUILayout.HelpBox("Screen Space Shadows must be active for Hit Lighting Evaluation", MessageType.Warning);
					}
					EditorGUILayout.PropertyField(DirectionalOcclusion, HEditorStyles.DirectionalOcclusionContent);
					// if (DirectionalOcclusion.boolValue == true)
					// 	EditorGUILayout.Slider(OcclusionIntensity, 0.0f, 1.0f, HEditorStyles.OcclusionIntensityContent);
				}
			}

			using (new HEditorUtils.FoldoutScope(AnimBoolDebugTab, out var shouldDraw, "Debug Settings"/*, toggle: EnableDebug*/))
			{
				_debugTab.boolValue = shouldDraw;
				if (shouldDraw) 
				{
					EditorGUILayout.PropertyField(DebugModeWS, HEditorStyles.DebugModeContent);
					EditorGUILayout.PropertyField(AttachToSceneCamera, new GUIContent("Follow Scene Camera"));
					
					
					//if (false) //todo: release uncomment
					{
						HEditorUtils.HorizontalLine(1f);
						EditorGUILayout.LabelField("DEVS FIELDS:", HEditorStyles.VersionStyle);
						EditorGUILayout.PropertyField(CameraForTests, new GUIContent("Camera For Tests"));

						EditorGUILayout.PropertyField(EnableCamerasVisualization, new GUIContent("Enable Cameras visualization"));
						EditorGUILayout.PropertyField(TestCheckbox,               new GUIContent("Test Checkbox"));
						EditorGUILayout.PropertyField(HTraceLayer,     new GUIContent("H Trace Layer"));
						EditorGUILayout.PropertyField(HInjectionPoint, new GUIContent("Injection Point"));
					}

				}
			}
			
			HEditorUtils.HorizontalLine(1f);
			EditorGUILayout.LabelField("HTrace WSGI Version: 1.3.0", HEditorStyles.VersionStyle);
			
			serializedObject.ApplyModifiedProperties();
		}

		private void UpdateStandartStyles()
		{
			HEditorStyles.foldout.fontStyle = FontStyle.Bold;
		}

		private void PropertiesRelative()
		{
			_globalSettingsTab = serializedObject.FindProperty("_globalSettingsTab");
			_ssLightingTab     = serializedObject.FindProperty("_screenSpaceLightingTab");
			_wsgiTab           = serializedObject.FindProperty("_wsgiTab");
			_debugTab          = serializedObject.FindProperty("_debugTab");

			_showUpdateOptions = serializedObject.FindProperty("_showUpdateOptions");
			_showVoxelParams   = serializedObject.FindProperty("_showVoxelParams");

			GeneralData             = serializedObject.FindProperty("GeneralData");
			VoxelizationData        = serializedObject.FindProperty("VoxelizationData");
			ScreenSpaceLightingData = serializedObject.FindProperty("ScreenSpaceLightingData");
			DebugData               = serializedObject.FindProperty("DebugData");

			//Debug Tab
			AttachToSceneCamera        = DebugData.FindPropertyRelative("_attachToSceneCamera");
			
			EnableDebug                = DebugData.FindPropertyRelative("_enableDebug");
			CameraForTests             = DebugData.FindPropertyRelative("CameraForTests");
			EnableCamerasVisualization = DebugData.FindPropertyRelative("_enableCamerasVisualization");
			TestCheckbox               = DebugData.FindPropertyRelative("TestCheckbox");
			HTraceLayer     = DebugData.FindPropertyRelative("HTraceLayer");
			HInjectionPoint = DebugData.FindPropertyRelative("HInjectionPoint");

			//Global Tab
			RayCountMode    = GeneralData.FindPropertyRelative("_rayCountMode");
			RayLength    = GeneralData.FindPropertyRelative("_rayLength");
			Multibounce     = GeneralData.FindPropertyRelative("Multibounce");
			DebugModeWS     = GeneralData.FindPropertyRelative("DebugModeWS");

			// Voxel Data
			VoxelizationMask       = VoxelizationData.FindPropertyRelative("VoxelizationMask");
			VoxelizationUpdateMode = VoxelizationData.FindPropertyRelative("VoxelizationUpdateMode");
			AttachTo               = VoxelizationData.FindPropertyRelative("AttachTo");
			ExpandShadowmap        = VoxelizationData.FindPropertyRelative("_expandShadowmap");
			LodMax    = VoxelizationData.FindPropertyRelative("_lodMax");
			DirectionalLight       = VoxelizationData.FindPropertyRelative("DirectionalLight");

			VoxelDensity               = VoxelizationData.FindPropertyRelative("_voxelDensity");
			VoxelBounds                = VoxelizationData.FindPropertyRelative("_voxelBounds");
			OverrideBoundsHeightEnable = VoxelizationData.FindPropertyRelative("_overrideBoundsHeightEnable");
			OverrideBoundsHeight       = VoxelizationData.FindPropertyRelative("_overrideBoundsHeight");
			CenterShift                = VoxelizationData.FindPropertyRelative("CenterShift");
			GroundLevelEnable          = VoxelizationData.FindPropertyRelative("GroundLevelEnable");
			GroundLevel                = VoxelizationData.FindPropertyRelative("GroundLevel");

			CulledObjectsMask  = VoxelizationData.FindPropertyRelative("CulledObjectsMask");
			ExpandCullFov      = VoxelizationData.FindPropertyRelative("_expandCullFov");
			ExpandCullRadius   = VoxelizationData.FindPropertyRelative("_expandCullRadius");
			DynamicObjectsMask = VoxelizationData.FindPropertyRelative("DynamicObjectsMask");

			ExactBounds     = VoxelizationData.FindPropertyRelative("ExactData").FindPropertyRelative("Bounds");
			ExactResolution = VoxelizationData.FindPropertyRelative("ExactData").FindPropertyRelative("Resolution");

			// Screen Space Lighting Tab
			EvaluateHitLighting  = ScreenSpaceLightingData.FindPropertyRelative("EvaluateHitLighting");
			DirectionalOcclusion = ScreenSpaceLightingData.FindPropertyRelative("DirectionalOcclusion");
			OcclusionIntensity   = ScreenSpaceLightingData.FindPropertyRelative("_occlusionIntensity");
		}
	}
}
#endif
