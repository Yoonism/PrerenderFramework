//////////////////////////////////////////////////////
// MK Glow Editor HDRP              	      		//
//					                                //
// Created by Michael Kremmel                       //
// www.michaelkremmel.de                            //
// Copyright © 2020 All rights reserved.            //
//////////////////////////////////////////////////////
#if UNITY_EDITOR
using MK.Glow;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.Rendering;

namespace MK.Glow.HDRP.Editor
{
    using MKEditor = MK.Glow.Editor;
	using Tooltips = MK.Glow.Editor.EditorHelper.EditorUIContent.Tooltips;
	#if UNITY_2022_2_OR_NEWER
	[CustomEditor(typeof(MK.Glow.HDRP.MKGlow))]
	#else
	[VolumeComponentEditor(typeof(MK.Glow.HDRP.MKGlow))]
	#endif
	internal sealed class MKGlowEditor : VolumeComponentEditor
	{
		//Behaviors
		private SerializedDataParameter _showEditorMainBehavior;
		private SerializedDataParameter _showEditorBloomBehavior;
		private SerializedDataParameter _showEditorLensSurfaceBehavior;
		private SerializedDataParameter _showEditorLensFlareBehavior;
		private SerializedDataParameter _showEditorGlareBehavior;
		private SerializedDataParameter _isInitialized;

		//Main
		private SerializedDataParameter _allowGeometryShaders;
		private SerializedDataParameter _allowComputeShaders;
		private SerializedDataParameter _renderPriority;
		private SerializedDataParameter _debugView;
		private SerializedDataParameter _quality;
		private SerializedDataParameter _antiFlickerMode;
		private SerializedDataParameter _workflow;
		private SerializedDataParameter _selectiveRenderLayerMask;
		private SerializedDataParameter _anamorphicRatio;
		private SerializedDataParameter _lumaScale;
		private SerializedDataParameter _blooming;

		//Bloom
		private SerializedDataParameter _bloomThreshold;
		private SerializedDataParameter _bloomScattering;
		private SerializedDataParameter _bloomIntensity;

		//Lens Surface
		private SerializedDataParameter _allowLensSurface;
		private SerializedDataParameter _lensSurfaceDirtTexture;
		private SerializedDataParameter _lensSurfaceDirtIntensity;
		private SerializedDataParameter _lensSurfaceDiffractionTexture;
		private SerializedDataParameter _lensSurfaceDiffractionIntensity;

		//Lens Flare
		private SerializedDataParameter _allowLensFlare;
		private SerializedDataParameter _lensFlareStyle;
		private SerializedDataParameter _lensFlareGhostFade;
		private SerializedDataParameter _lensFlareGhostIntensity;
		private SerializedDataParameter _lensFlareThreshold;
		private SerializedDataParameter _lensFlareScattering;
		private SerializedDataParameter _lensFlareColorRamp;
		private SerializedDataParameter _lensFlareChromaticAberration;
		private SerializedDataParameter _lensFlareGhostCount;
		private SerializedDataParameter _lensFlareGhostDispersal;
		private SerializedDataParameter _lensFlareHaloFade;
		private SerializedDataParameter _lensFlareHaloIntensity;
		private SerializedDataParameter _lensFlareHaloSize;

		//Glare
		private SerializedDataParameter _allowGlare;
		private SerializedDataParameter _glareBlend;
		private SerializedDataParameter _glareIntensity;
		private SerializedDataParameter _glareThreshold;
		private SerializedDataParameter _glareScattering;
		private SerializedDataParameter _glareStyle;
		private SerializedDataParameter _glareStreaks;
		private SerializedDataParameter _glareAngle;
		private SerializedDataParameter _glareSample0Scattering;
		private SerializedDataParameter _glareSample0Intensity;
		private SerializedDataParameter _glareSample0Angle;
		private SerializedDataParameter _glareSample0Offset;
		private SerializedDataParameter _glareSample1Scattering;
		private SerializedDataParameter _glareSample1Intensity;
		private SerializedDataParameter _glareSample1Angle;
		private SerializedDataParameter _glareSample1Offset;
		private SerializedDataParameter _glareSample2Scattering;
		private SerializedDataParameter _glareSample2Intensity;
		private SerializedDataParameter _glareSample2Angle;
		private SerializedDataParameter _glareSample2Offset;
		private SerializedDataParameter _glareSample3Scattering;
		private SerializedDataParameter _glareSample3Intensity;
		private SerializedDataParameter _glareSample3Angle;
		private SerializedDataParameter _glareSample3Offset;

		public override void OnEnable()
		{
            var o = new PropertyFetcher<MK.Glow.HDRP.MKGlow>(serializedObject);

			//Editor
			_showEditorMainBehavior = Unpack(o.Find(x => x.showEditorMainBehavior));
			_showEditorBloomBehavior = Unpack(o.Find(x => x.showEditorBloomBehavior));
			_showEditorLensSurfaceBehavior = Unpack(o.Find(x => x.showEditorLensSurfaceBehavior));
			_showEditorLensFlareBehavior = Unpack(o.Find(x => x.showEditorLensFlareBehavior));
			_showEditorGlareBehavior = Unpack(o.Find(x => x.showEditorGlareBehavior));
			_isInitialized = Unpack(o.Find(x => x.isInitialized));

			//Main
			_allowGeometryShaders = Unpack(o.Find(x => x.allowGeometryShaders));
			_allowComputeShaders = Unpack(o.Find(x => x.allowComputeShaders));
			_renderPriority = Unpack(o.Find(x => x.renderPriority));
			_debugView = Unpack(o.Find(x => x.debugView));
			_quality = Unpack(o.Find(x => x.quality));
			_antiFlickerMode = Unpack(o.Find(x => x.antiFlickerMode));
			_workflow = Unpack(o.Find(x => x.workflow));
			_selectiveRenderLayerMask = Unpack(o.Find(x => x.selectiveRenderLayerMask));
			_anamorphicRatio = Unpack(o.Find(x => x.anamorphicRatio));
			_lumaScale = Unpack(o.Find(x => x.lumaScale));
			_blooming = Unpack(o.Find(x => x.blooming));

			//Bloom
			_bloomThreshold = Unpack(o.Find(x => x.bloomThreshold));
			_bloomScattering = Unpack(o.Find(x => x.bloomScattering));
			_bloomIntensity = Unpack(o.Find(x => x.bloomIntensity));

			_allowLensSurface = Unpack(o.Find(x => x.allowLensSurface));
			_lensSurfaceDirtTexture = Unpack(o.Find(x => x.lensSurfaceDirtTexture));
			_lensSurfaceDirtIntensity = Unpack(o.Find(x => x.lensSurfaceDirtIntensity));
			_lensSurfaceDiffractionTexture = Unpack(o.Find(x => x.lensSurfaceDiffractionTexture));
			_lensSurfaceDiffractionIntensity = Unpack(o.Find(x => x.lensSurfaceDiffractionIntensity));

			_allowLensFlare = Unpack(o.Find(x => x.allowLensFlare));
			_lensFlareStyle = Unpack(o.Find(x => x.lensFlareStyle));
			_lensFlareGhostFade = Unpack(o.Find(x => x.lensFlareGhostFade));
			_lensFlareGhostIntensity = Unpack(o.Find(x => x.lensFlareGhostIntensity));
			_lensFlareThreshold = Unpack(o.Find(x => x.lensFlareThreshold));
			_lensFlareScattering = Unpack(o.Find(x => x.lensFlareScattering));
			_lensFlareColorRamp = Unpack(o.Find(x => x.lensFlareColorRamp));
			_lensFlareChromaticAberration = Unpack(o.Find(x => x.lensFlareChromaticAberration));
			_lensFlareGhostCount = Unpack(o.Find(x => x.lensFlareGhostCount));
			_lensFlareGhostDispersal = Unpack(o.Find(x => x.lensFlareGhostDispersal));
			_lensFlareHaloFade = Unpack(o.Find(x => x.lensFlareHaloFade));
			_lensFlareHaloIntensity = Unpack(o.Find(x => x.lensFlareHaloIntensity));
			_lensFlareHaloSize = Unpack(o.Find(x => x.lensFlareHaloSize));

			_allowGlare = Unpack(o.Find(x => x.allowGlare));
			_glareBlend = Unpack(o.Find(x => x.glareBlend));
			_glareIntensity = Unpack(o.Find(x => x.glareIntensity));
			_glareThreshold = Unpack(o.Find(x => x.glareThreshold));
			_glareScattering = Unpack(o.Find(x => x.glareScattering));
			_glareAngle = Unpack(o.Find(x => x.glareAngle));
			_glareStyle = Unpack(o.Find(x => x.glareStyle));
			_glareStreaks = Unpack(o.Find(x => x.glareStreaks));
			_glareSample0Scattering = Unpack(o.Find(x => x.glareSample0Scattering));
			_glareSample0Intensity = Unpack(o.Find(x => x.glareSample0Intensity));
			_glareSample0Angle = Unpack(o.Find(x => x.glareSample0Angle));
			_glareSample0Offset = Unpack(o.Find(x => x.glareSample0Offset));
			_glareSample1Scattering = Unpack(o.Find(x => x.glareSample1Scattering));
			_glareSample1Intensity = Unpack(o.Find(x => x.glareSample1Intensity));
			_glareSample1Angle = Unpack(o.Find(x => x.glareSample1Angle));
			_glareSample1Offset = Unpack(o.Find(x => x.glareSample1Offset));
			_glareSample2Scattering = Unpack(o.Find(x => x.glareSample2Scattering));
			_glareSample2Intensity = Unpack(o.Find(x => x.glareSample2Intensity));
			_glareSample2Angle = Unpack(o.Find(x => x.glareSample2Angle));
			_glareSample2Offset = Unpack(o.Find(x => x.glareSample2Offset));
			_glareSample3Scattering = Unpack(o.Find(x => x.glareSample3Scattering));
			_glareSample3Intensity = Unpack(o.Find(x => x.glareSample3Intensity));
			_glareSample3Angle = Unpack(o.Find(x => x.glareSample3Angle));
			_glareSample3Offset = Unpack(o.Find(x => x.glareSample3Offset));
		}

		public override void OnInspectorGUI()
		{
			if(_isInitialized.value.boolValue == false)
			{
				_bloomIntensity.value.floatValue = 1f;
				_bloomIntensity.overrideState.boolValue = true;

				_lensSurfaceDirtIntensity.value.floatValue = 2.5f;
				_lensSurfaceDirtIntensity.overrideState.boolValue = true;
				_lensSurfaceDiffractionIntensity.value.floatValue = 2f;
				_lensSurfaceDiffractionIntensity.overrideState.boolValue = true;

				_lensFlareGhostIntensity.value.floatValue = 1.0f;
				_lensFlareGhostIntensity.overrideState.boolValue = true;
				_lensFlareHaloIntensity.value.floatValue = 1.0f;
				_lensFlareHaloIntensity.overrideState.boolValue = true;

				_glareSample0Intensity.value.floatValue = 1.0f;
				_glareSample0Intensity.overrideState.boolValue = true;
				_glareSample1Intensity.value.floatValue = 1.0f;
				_glareSample1Intensity.overrideState.boolValue = true;
				_glareSample2Intensity.value.floatValue = 1.0f;
				_glareSample2Intensity.overrideState.boolValue = true;
				_glareSample3Intensity.value.floatValue = 1.0f;
				_glareSample3Intensity.overrideState.boolValue = true;

				_isInitialized.value.boolValue = true;
			}

			MKEditor.EditorHelper.EditorUIContent.IsNotSupportedWarning();
			MKEditor.EditorHelper.EditorUIContent.XRUnityVersionWarning();
			if(_workflow.value.enumValueIndex == 1)
            {
				MKEditor.EditorHelper.EditorUIContent.SelectiveWorkflowDeprecated();
			}

			if(MKEditor.EditorHelper.HandleBehavior(_showEditorMainBehavior.value.serializedObject.targetObject, MKEditor.EditorHelper.EditorUIContent.mainTitle, "", _showEditorMainBehavior.value, null))
			{
				PropertyField(_allowGeometryShaders, Tooltips.allowGeometryShaders);
				PropertyField(_allowComputeShaders, Tooltips.allowComputeShaders);
				PropertyField(_renderPriority, Tooltips.renderPriority);
				PropertyField(_debugView, Tooltips.debugView);
				PropertyField(_quality, Tooltips.quality);
				PropertyField(_antiFlickerMode, Tooltips.antiFlickerMode);
				PropertyField(_workflow, Tooltips.workflow);
				MKEditor.EditorHelper.EditorUIContent.SelectiveWorkflowVRWarning((Workflow)_workflow.value.enumValueIndex);
                if(_workflow.value.enumValueIndex == 1)
                {
                    PropertyField(_selectiveRenderLayerMask, Tooltips.selectiveRenderLayerMask);
                }
				PropertyField(_anamorphicRatio, Tooltips.anamorphicRatio);
				PropertyField(_lumaScale, Tooltips.lumaScale);
				if(_renderPriority.value.enumValueIndex != 2 && _workflow.value.enumValueIndex != 2)
					PropertyField(_blooming, Tooltips.blooming);
				MKEditor.EditorHelper.VerticalSpace();
			}
			
			if(MKEditor.EditorHelper.HandleBehavior(_showEditorBloomBehavior.value.serializedObject.targetObject, MKEditor.EditorHelper.EditorUIContent.bloomTitle, "", _showEditorBloomBehavior.value, null))
			{
				if(_workflow.value.enumValueIndex == 0)
                {
					PropertyField(_bloomThreshold, Tooltips.bloomThreshold);
                }
				PropertyField(_bloomScattering, Tooltips.bloomScattering);
				PropertyField(_bloomIntensity, Tooltips.bloomIntensity);
				_bloomIntensity.value.floatValue = Mathf.Max(0, _bloomIntensity.value.floatValue);

				MKEditor.EditorHelper.VerticalSpace();
			}

			if(MKEditor.EditorHelper.HandleBehavior(_showEditorLensSurfaceBehavior.value.serializedObject.targetObject, MKEditor.EditorHelper.EditorUIContent.lensSurfaceTitle, "", _showEditorLensSurfaceBehavior.value, _allowLensSurface.value))
			{
				using (new EditorGUI.DisabledScope(!_allowLensSurface.value.boolValue))
                {
					MKEditor.EditorHelper.DrawHeader(MKEditor.EditorHelper.EditorUIContent.dirtTitle);
					PropertyField(_lensSurfaceDirtTexture, Tooltips.lensSurfaceDirtTexture);
					PropertyField(_lensSurfaceDirtIntensity, Tooltips.lensSurfaceDirtIntensity);
					_lensSurfaceDirtIntensity.value.floatValue = Mathf.Max(0, _lensSurfaceDirtIntensity.value.floatValue);
					EditorGUILayout.Space();
					MKEditor.EditorHelper.DrawHeader(MKEditor.EditorHelper.EditorUIContent.diffractionTitle);
					PropertyField(_lensSurfaceDiffractionTexture, Tooltips.lensSurfaceDiffractionTexture);
					PropertyField(_lensSurfaceDiffractionIntensity, Tooltips.lensSurfaceDiffractionIntensity);
					_lensSurfaceDiffractionIntensity.value.floatValue = Mathf.Max(0, _lensSurfaceDiffractionIntensity.value.floatValue);
				}
				MKEditor.EditorHelper.VerticalSpace();
			}

			if(Compatibility.CheckLensFlareFeatureSupport() && _quality.value.intValue <= 4)
			{
				if(MKEditor.EditorHelper.HandleBehavior(_showEditorLensFlareBehavior.value.serializedObject.targetObject, MKEditor.EditorHelper.EditorUIContent.lensFlareTitle, "", _showEditorLensFlareBehavior.value, _allowLensFlare.value))
				{
					using (new EditorGUI.DisabledScope(!_allowLensFlare.value.boolValue))
					{
						PropertyField(_lensFlareStyle, Tooltips.lensFlareStyle);
						if(_workflow.value.enumValueIndex == 0)
							PropertyField(_lensFlareThreshold, Tooltips.lensFlareThreshold);
						PropertyField(_lensFlareScattering, Tooltips.lensFlareScattering);
						PropertyField(_lensFlareColorRamp, Tooltips.lensFlareColorRamp);
						PropertyField(_lensFlareChromaticAberration, Tooltips.lensFlareChromaticAberration);

						EditorGUILayout.Space();
						MKEditor.EditorHelper.DrawHeader(MKEditor.EditorHelper.EditorUIContent.ghostsTitle);
						if(_lensFlareStyle.value.enumValueIndex == 0)
						{
							PropertyField(_lensFlareGhostFade, Tooltips.lensFlareGhostFade);
							PropertyField(_lensFlareGhostCount, Tooltips.lensFlareGhostCount);
							PropertyField(_lensFlareGhostDispersal, Tooltips.lensFlareGhostDispersal);
						}
						PropertyField(_lensFlareGhostIntensity, Tooltips.lensFlareGhostIntensity);
						_lensFlareGhostIntensity.value.floatValue = Mathf.Max(0, _lensFlareGhostIntensity.value.floatValue);

						EditorGUILayout.Space();
						MKEditor.EditorHelper.DrawHeader(MKEditor.EditorHelper.EditorUIContent.haloTitle);
						if(_lensFlareStyle.value.enumValueIndex == 0)
						{
							PropertyField(_lensFlareHaloFade, Tooltips.lensFlareHaloFade);
							PropertyField(_lensFlareHaloSize, Tooltips.lensFlareHaloSize);
						}
						PropertyField(_lensFlareHaloIntensity, Tooltips.lensFlareHaloIntensity);
						_lensFlareHaloIntensity.value.floatValue = Mathf.Max(0, _lensFlareHaloIntensity.value.floatValue);
					}
					MKEditor.EditorHelper.VerticalSpace();
				}
			}
			else
			{
				MKEditor.EditorHelper.DrawSplitter();
				MKEditor.EditorHelper.EditorUIContent.LensFlareFeatureNotSupportedWarning();
			}

			if(Compatibility.CheckGlareFeatureSupport() && _quality.value.intValue <= 4)
			{
				if(MKEditor.EditorHelper.HandleBehavior(_showEditorGlareBehavior.value.serializedObject.targetObject, MKEditor.EditorHelper.EditorUIContent.glareTitle, "", _showEditorGlareBehavior.value, _allowGlare.value))
				{
					using (new EditorGUI.DisabledScope(!_allowGlare.value.boolValue))
					{
						PropertyField(_glareStyle, Tooltips.glareStyle);
						if(_workflow.value.enumValueIndex == 0)
							PropertyField(_glareThreshold, Tooltips.glareThreshold);
						if(_glareStyle.value.enumValueIndex == 0)
							PropertyField(_glareStreaks, Tooltips.glareStreaks);
						PropertyField(_glareBlend, Tooltips.glareBlend);
						PropertyField(_glareScattering, Tooltips.glareScattering);
						PropertyField(_glareAngle, Tooltips.glareAngle);
						PropertyField(_glareIntensity, Tooltips.glareIntensity);
						_glareScattering.value.floatValue = Mathf.Max(0, _glareScattering.value.floatValue);
						_glareIntensity.value.floatValue = Mathf.Max(0, _glareIntensity.value.floatValue);

						if(_glareStyle.value.enumValueIndex == 0)
						{
							EditorGUILayout.Space();
							MKEditor.EditorHelper.DrawHeader(MKEditor.EditorHelper.EditorUIContent.sample0Title);
							PropertyField(_glareSample0Scattering, Tooltips.glareSample0Scattering);
							PropertyField(_glareSample0Angle, Tooltips.glareSample0Angle);
							PropertyField(_glareSample0Offset, Tooltips.glareSample0Offset);
							PropertyField(_glareSample0Intensity, Tooltips.glareSample0Intensity);
							_glareSample0Intensity.value.floatValue = Mathf.Max(0, _glareSample0Intensity.value.floatValue);
							
							if(_glareStreaks.value.intValue >= 2)
							{
								EditorGUILayout.Space();
								MKEditor.EditorHelper.DrawHeader(MKEditor.EditorHelper.EditorUIContent.sample1Title);
								PropertyField(_glareSample1Scattering, Tooltips.glareSample1Scattering);
								PropertyField(_glareSample1Angle, Tooltips.glareSample1Angle);
								PropertyField(_glareSample1Offset, Tooltips.glareSample1Offset);
								PropertyField(_glareSample1Intensity, Tooltips.glareSample1Intensity);
								_glareSample1Intensity.value.floatValue = Mathf.Max(0, _glareSample1Intensity.value.floatValue);
							}

							if(_glareStreaks.value.intValue >= 3)
							{
								EditorGUILayout.Space();
								MKEditor.EditorHelper.DrawHeader(MKEditor.EditorHelper.EditorUIContent.sample2Title);
								PropertyField(_glareSample2Scattering, Tooltips.glareSample2Scattering);
								PropertyField(_glareSample2Angle, Tooltips.glareSample2Angle);
								PropertyField(_glareSample2Offset, Tooltips.glareSample2Offset);
								PropertyField(_glareSample2Intensity, Tooltips.glareSample2Intensity);
								_glareSample2Intensity.value.floatValue = Mathf.Max(0, _glareSample2Intensity.value.floatValue);
							}
							
							if(_glareStreaks.value.intValue >= 4)
							{
								EditorGUILayout.Space();
								MKEditor.EditorHelper.DrawHeader(MKEditor.EditorHelper.EditorUIContent.sample3Title);
								PropertyField(_glareSample3Scattering, Tooltips.glareSample3Scattering);
								PropertyField(_glareSample3Angle, Tooltips.glareSample3Angle);
								PropertyField(_glareSample3Offset, Tooltips.glareSample3Offset);
								PropertyField(_glareSample3Intensity, Tooltips.glareSample3Intensity);
								_glareSample3Intensity.value.floatValue = Mathf.Max(0, _glareSample3Intensity.value.floatValue);
							}
						}
					}
					MKEditor.EditorHelper.VerticalSpace();
				}
			}
			else
			{
				MKEditor.EditorHelper.DrawSplitter();
				MKEditor.EditorHelper.EditorUIContent.GlareFeatureNotSupportedWarning();
			}
			MKEditor.EditorHelper.DrawSplitter();
		}
    }
}
#endif