#if UNITY_EDITOR
using System;
using System.Collections; // UNITY 6
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Object = UnityEngine.Object;

namespace H_Trace.Scripts.PipelinesConfigurator
{
	internal class HPipelinesConfigurator
	{
		public static void AlwaysIncludedShaders()
		{
			AddShaderToGraphicsSettings("HTrace/Voxelization");
			AddShaderToGraphicsSettings("HTrace/VoxelVisualization");
			AddShaderToGraphicsSettings("HTrace/Shadowmap");
		}

		private static void AddShaderToGraphicsSettings(string shaderName)
		{
			var shader = Shader.Find(shaderName);
			if (shader == null)
				return;

			var  graphicsSettings = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
			var  serializedObject = new SerializedObject(graphicsSettings);
			var  arrayProp        = serializedObject.FindProperty("m_AlwaysIncludedShaders");
			bool hasShader        = false;
			for (int i = 0; i < arrayProp.arraySize; ++i)
			{
				var arrayElem = arrayProp.GetArrayElementAtIndex(i);
				if (shader == arrayElem.objectReferenceValue)
				{
					hasShader = true;
					break;
				}
			}

			if (!hasShader)
			{
				int arrayIndex = arrayProp.arraySize;
				arrayProp.InsertArrayElementAtIndex(arrayIndex);
				var arrayElem = arrayProp.GetArrayElementAtIndex(arrayIndex);
				arrayElem.objectReferenceValue = shader;

				serializedObject.ApplyModifiedProperties();

				AssetDatabase.SaveAssets();
			}
		}
	}
}
#endif
