using System;
using H_Trace.Scripts.Globals;
using UnityEngine;

namespace H_Trace.Scripts.Structs
{
	[Serializable]
	public class ScreenSpaceLightingData
	{
		/// <summary>
		/// Allows to evaluate lighting at the hit points of screen-space rays instead of relying solely on the previous Color Buffer.
		/// </summary>
		/// <Docs><see href="https://ipgames.gitbook.io/htrace-wsgi/settings-and-properties">More information</see></Docs>
		public bool EvaluateHitLighting  = false;
		
		/// <summary>
		/// Enable directional screen space occlusion.
		/// </summary>
		/// <Docs><see href="https://ipgames.gitbook.io/htrace-wsgi/settings-and-properties">More information</see></Docs>
		public bool DirectionalOcclusion = true;
		
		[SerializeField]
		private float _occlusionIntensity = 0.25f;
		/// <summary>
		/// Occlusion Intensity
		/// </summary>
		/// <value>[0.0;1.0]</value>
		/// <Docs><see href="https://ipgames.gitbook.io/htrace-wsgi/settings-and-properties">More information</see></Docs>
		[HExtensions.HRangeAttribute(0.0f,1.0f)]
		public float OcclusionIntensity
		{
			get => _occlusionIntensity;    
			set
			{
				if (Mathf.Abs(value - _occlusionIntensity) < Mathf.Epsilon)
					return;

				HExtensions.HRangeAttributeDictionary.TryGetValue(nameof(OcclusionIntensity), out HExtensions.HRangeAttributeElement attributeValue);
				_occlusionIntensity = Mathf.Clamp(value, attributeValue.minFloat, attributeValue.maxFloat);
			}
		}

	}
}
