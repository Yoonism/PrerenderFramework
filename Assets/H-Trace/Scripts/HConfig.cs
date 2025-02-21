using UnityEngine;

namespace H_Trace.Scripts
{
	public static class HConfig
	{
		//todo: can we update it in runtime?
		[Range(0f,1f)]
		public static float SkyOcclusionCone = 0f;
			
		// Indirect Intensity Multipliers (values higher than 1.0 are NOT physically correct!)
		public static float DirectionalLightIntensity = 1.0f;
		public static float SurfaceDiffuseIntensity = 1.0f;
		public static float SkyLightIntensity = 1.0f;
		
		/// <summary>
		/// Voxelization volume side length in Metres
		/// </summary>
		/// IMPORTANT!!! IF YOU CHANGE IT - DISABLE AND ENABLE HTRACE
		public const int MAX_VOXEL_BOUNDS = 80;
	}
}
