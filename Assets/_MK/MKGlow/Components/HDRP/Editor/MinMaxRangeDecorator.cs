//////////////////////////////////////////////////////
// MK Glow Range Drawer    	    	       			//
//					                                //
// Created by Michael Kremmel                       //
// www.michaelkremmel.de                            //
// Copyright © 2020 All rights reserved.            //
//////////////////////////////////////////////////////

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Globalization;

namespace MK.Glow.HDRP
{
	using MinMaxRangeParameter = MK.Glow.HDRP.MKGlow.MinMaxRangeParameter;

	[UnityEditor.Rendering.VolumeParameterDrawer(typeof(MinMaxRangeParameter))]
    sealed class MinMaxRangeParameterDrawer : UnityEditor.Rendering.VolumeParameterDrawer
    {
        public override bool OnGUI(UnityEditor.Rendering.SerializedDataParameter parameter, GUIContent title)
        {
			var value = parameter.value;

			MinMaxRangeParameter minMaxRangeParameter = parameter.GetObjectRef<MinMaxRangeParameter>();

			SerializedProperty minRange = parameter.value.FindPropertyRelative("minValue");
			SerializedProperty maxRange = parameter.value.FindPropertyRelative("maxValue");

			float minValue = minMaxRangeParameter.value.minValue;
			float maxValue = minMaxRangeParameter.value.maxValue;

			Rect startRect = EditorGUILayout.GetControlRect();

			Rect minRect = new Rect(EditorGUIUtility.labelWidth + EditorGUIUtility.fieldWidth * 0.7f, startRect.y, EditorGUIUtility.fieldWidth, startRect.height);
			float p = minRect.x + EditorGUIUtility.standardVerticalSpacing * 2f + EditorGUIUtility.fieldWidth;
			Rect sliderRect = new Rect(p, startRect.y, EditorGUIUtility.currentViewWidth - p - EditorGUIUtility.fieldWidth * 1.5f, startRect.height);
			Rect maxRect = new Rect(sliderRect.x + sliderRect.width + EditorGUIUtility.standardVerticalSpacing * 2f, startRect.y, EditorGUIUtility.fieldWidth, startRect.height);

			EditorGUI.LabelField(startRect, title.text);
			minValue = EditorGUI.FloatField(minRect, minValue);

			EditorGUI.MinMaxSlider(sliderRect, ref minValue, ref maxValue, 0, 10);
			maxValue = EditorGUI.FloatField(maxRect, maxValue);

			minRange.floatValue = minValue;
			maxRange.floatValue = maxValue;
			
			return true;
        }
    }
}
#endif