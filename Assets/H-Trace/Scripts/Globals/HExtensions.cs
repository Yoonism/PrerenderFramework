using System;
using System.Collections.Generic;
using System.Reflection;
using H_Trace.Scripts.Structs;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace H_Trace.Scripts.Globals
{
	public static class HExtensions
	{
		public static string ERROR_OUT_RANGE_VALUE = "Your \"{0}\" value is out of range: {1}";

		public static ComputeShader LoadComputeShader(string shaderName)
		{
			var computeShader = (ComputeShader)Resources.Load($"HTRaceWSGI/Computes/{shaderName}");
			if (computeShader == null)
			{
				Debug.LogError($"{shaderName} is missing in H-Trace/Resources/Computes folder");
				return null;
			}

			return computeShader;
		}

		public static HDRenderPipelineAsset CurrentAsset
		{
			get
			{
				return GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset hdrpAsset ? hdrpAsset : null;
			}
		}

		private static HDRenderPipelineAsset _hdrpAsset;
		public static HDRenderPipelineAsset HdrpAsset
		{
			get
			{
				if (_hdrpAsset == null)
				{
					if (GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset hdrpAsset)
						_hdrpAsset = hdrpAsset;
				}
				return _hdrpAsset;
			}
		}
		
		internal static bool PipelineSupportsScreenSpaceShadows => HdrpAsset != null ? HdrpAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams.supportScreenSpaceShadows : false;
		internal static bool PipelineSupportsSSGI => HdrpAsset != null ? HdrpAsset.currentPlatformRenderPipelineSettings.supportSSGI : false;

		public static T GetCopyOf<T>(this Component comp, T other) where T : Component
		{
			Type type = comp.GetType();
			if (type != other.GetType()) return null; // type mis-match
			BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
			PropertyInfo[] pinfos = type.GetProperties(flags);
			foreach (var pinfo in pinfos)
			{
				if (pinfo.CanWrite)
				{
					try
					{
						pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
					}
					catch
					{
					} // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
				}
			}

			FieldInfo[] finfos = type.GetFields(flags);
			foreach (var finfo in finfos)
			{
				finfo.SetValue(comp, finfo.GetValue(other));
			}

			return comp as T;
		}

		public static T AddComponent<T>(this GameObject go, T toAdd) where T : Component
		{
			return go.AddComponent<T>().GetCopyOf(toAdd) as T;
		}

		public static void RemoveComponent(Component component, bool immediate = false)
		{
			if (component != null)
			{
				if (immediate)
				{
					UnityEngine.Object.DestroyImmediate(component as UnityEngine.Object, true);
				}
				else
				{
					UnityEngine.Object.Destroy(component as UnityEngine.Object);
				}
			}
		}

		public static T Next<T>(this T src) where T : struct
		{
			if (!typeof(T).IsEnum) throw new ArgumentException(String.Format("Argument {0} is not an Enum", typeof(T).FullName));

			T[] Arr = (T[])Enum.GetValues(src.GetType());
			int j = Array.IndexOf<T>(Arr, src) + 1;
			return (Arr.Length == j) ? Arr[0] : Arr[j];
		}
		
		public static void HRelease(RTHandle rtHandle)
		{
			RTHandles.Release(rtHandle);
		}

		public static void HRelease(ComputeBuffer cumputeBuffer)
		{
			if (cumputeBuffer != null)
				cumputeBuffer.Release();
		}

		public static int ParseToProbeSize(this RayCountMode rayCountMode)
		{
			switch (rayCountMode)
			{
				case RayCountMode.Performance:
					return 8;
				case RayCountMode.Quality:
					return 6;
				case RayCountMode.Cinematic:
					return 4;
			}

			return 6;
		}

		//custom Attributes
#if UNITY_EDITOR
		
		/// <summary>
		/// Read Only attribute.
		/// Attribute is use only to mark ReadOnly properties.
		/// </summary>
		public class ReadOnlyAttribute : PropertyAttribute
		{
		}

		/// <summary>
		/// This class contain custom drawer for ReadOnly attribute.
		/// </summary>
		[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
		public class ReadOnlyDrawer : PropertyDrawer
		{
			/// <summary>
			/// Unity method for drawing GUI in Editor
			/// </summary>
			/// <param name="position">Position.</param>
			/// <param name="property">Property.</param>
			/// <param name="label">Label.</param>
			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
			{
				// Saving previous GUI enabled value
				var previousGUIState = GUI.enabled;
				// Disabling edit for property
				GUI.enabled = false;
				// Drawing Property
				EditorGUI.PropertyField(position, property, label);
				// Setting old GUI enabled value
				GUI.enabled = previousGUIState;
			}
		}
#endif
		
		/// <summary>
		///   <para>Attribute used to make a float or int variable in a script be restricted to a specific range.</para>
		/// </summary>
		[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
		public class HRangeAttribute : Attribute
		{
			public readonly bool isFloat;

			public readonly float minFloat;
			public readonly float maxFloat;
			public readonly int minInt;
			public readonly int maxInt;

			/// <summary>
			///   <para>Attribute used to make a float or int variable in a script be restricted to a specific range.</para>
			/// </summary>
			/// <param name="minFloat">The minimum allowed value.</param>
			/// <param name="maxFloat">The maximum allowed value.</param>
			public HRangeAttribute(float minFloat, float maxFloat)
			{
				this.minFloat = minFloat;
				this.maxFloat = maxFloat;
				isFloat = true;
			}

			/// <summary>
			///   <para>Attribute used to make a float or int variable in a script be restricted to a specific range.</para>
			/// </summary>
			/// <param name="minFloat">The minimum allowed value.</param>
			/// <param name="maxFloat">The maximum allowed value.</param>
			public HRangeAttribute(int minInt, int maxInt)
			{
				this.minInt = minInt;
				this.maxInt = maxInt;
				isFloat = false;
			}
		}

		public struct HRangeAttributeElement
		{
			public bool isFloat;
			public float minFloat;
			public float maxFloat;
			public int minInt;
			public int maxInt;
		}

		public static readonly Dictionary<string, HRangeAttributeElement> HRangeAttributeDictionary = new Dictionary<string, HRangeAttributeElement>();

		public static void FillAttributeDictionary()
		{
			if (HRangeAttributeDictionary.Count != 0)
				return;

			List<PropertyInfo> props = new List<PropertyInfo>();
			props.AddRange(typeof(GeneralData).GetProperties());
			props.AddRange(typeof(ScreenSpaceLightingData).GetProperties());
			props.AddRange(typeof(VoxelizationData).GetProperties());
			
			foreach (PropertyInfo prop in props)
			{
				object[] attrs = prop.GetCustomAttributes(true);
				foreach (object attr in attrs)
				{
					HRangeAttribute authAttr = attr as HRangeAttribute;
					if (authAttr != null)
					{
						string propName = prop.Name;
						HRangeAttributeElement auth = new HRangeAttributeElement()
						{
							isFloat = authAttr.isFloat,
							minFloat = authAttr.minFloat,
							maxFloat = authAttr.maxFloat,
							minInt = authAttr.minInt,
							maxInt = authAttr.maxInt,
						};

						HRangeAttributeDictionary.Add(propName, auth);
					}
				}
			}
		}
	}
}
