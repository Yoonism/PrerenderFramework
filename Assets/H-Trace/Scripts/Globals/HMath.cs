using H_Trace.Scripts.Structs;
using UnityEngine;

namespace H_Trace.Scripts.Globals
{
	public static class HMath
	{
		/// <summary>
		/// Remap from one range to another
		/// </summary>
		/// <param name="input"></param>
		/// <param name="oldLow"></param>
		/// <param name="oldHigh"></param>
		/// <param name="newLow"></param>
		/// <param name="newHigh"></param>
		/// <returns></returns>
		public static float Remap(float input, float oldLow, float oldHigh, float newLow, float newHigh)
		{
			float t = Mathf.InverseLerp(oldLow, oldHigh, input);
			return Mathf.Lerp(newLow, newHigh, t);
		}

		/// <summary>
		/// Thickness value pre-calculation for GI
		/// </summary>
		/// <param name="baseThickness"></param>
		/// <param name="camera"></param>
		/// <returns></returns>
		public static Vector2 ThicknessBias(float baseThickness, Camera camera)
		{
			baseThickness = Remap(baseThickness, 0f, 1f, 0f, 0.5f);
			float n = camera.nearClipPlane;
			float f = camera.farClipPlane;
			float thicknessScale = 1.0f / (1.0f + baseThickness);
			float thicknessBias = -n / (f - n) * (baseThickness * thicknessScale);
			return new Vector2(thicknessScale, thicknessBias);
		}

		public static Vector4 ComputeViewportScaleAndLimit(Vector2Int viewportSize, Vector2Int bufferSize)
		{
			return new Vector4(ComputeViewportScale(viewportSize.x, bufferSize.x), // Scale(x)
				ComputeViewportScale(viewportSize.y, bufferSize.y), // Scale(y)
				ComputeViewportLimit(viewportSize.x, bufferSize.x), // Limit(x)
				ComputeViewportLimit(viewportSize.y, bufferSize.y)); // Limit(y)
		}

		public static float PixelSpreadTangent(float Fov, int Width, int Height)
		{
			return Mathf.Tan(Fov * Mathf.Deg2Rad * 0.5f) * 2.0f / Mathf.Min(Width, Height);
		}

		public static Vector3Int CalculateVoxelResolution(VoxelizationData voxelizationData)
		{
			Vector3Int resolutionResult = new Vector3Int();
			float resolution = HMath.Remap(voxelizationData.VoxelDensity, 0f, 1f, HConstants.ClampVoxelsResolution.x, HConstants.ClampVoxelsResolution.y);
			resolutionResult.x = Mathf.CeilToInt(resolution);
			resolutionResult.y = Mathf.CeilToInt(resolution);
			float targetHeight = (voxelizationData.OverrideBoundsHeightEnable == false ? voxelizationData.VoxelBounds : voxelizationData.OverrideBoundsHeight);
			resolutionResult.z = Mathf.CeilToInt(targetHeight / (voxelizationData.VoxelBounds / resolution));

			resolutionResult.x = HMath.DevisionBy32(resolutionResult.x);
			resolutionResult.y = HMath.DevisionBy32(resolutionResult.y);
			resolutionResult.z = HMath.DevisionBy32(resolutionResult.z);

			return resolutionResult;
		}

		public static float CalculateVoxelSizeInCM_UI(int bounds, float density)
		{
			float resolution = Mathf.CeilToInt(bounds / (bounds / HMath.Remap(density, 0f, 1f, HConstants.ClampVoxelsResolution.x, HConstants.ClampVoxelsResolution.y)));
			return bounds / resolution * 100f; //100 -> cm
		}

		public static float TexturesSizeInMB_UI(int voxelBounds, float density, bool overrideGroundEnable, int GroundLevel)
		{
			float resolution = voxelBounds / (voxelBounds / HMath.Remap(density, 0f, 1f, HConstants.ClampVoxelsResolution.x, HConstants.ClampVoxelsResolution.y));
			float voxelSize = voxelBounds / resolution;
			float textureResolution = resolution * resolution;
			textureResolution *= overrideGroundEnable == true ? (GroundLevel / voxelSize) : resolution;
			float colorMemorySize = textureResolution * 32 / (1024 * 1024 * 8);
			float positionMemorySize = (textureResolution * 32 / (1024 * 1024 * 8)) + (textureResolution * 8 / (1024 * 1024 * 8));

			return colorMemorySize + positionMemorySize;
		}

		public static float TexturesSizeInMB_UI(Vector3Int voxelsRelosution, VoxelizationUpdateMode voxelizationUpdateMode)
		{
			float textureResolution = voxelsRelosution.x * voxelsRelosution.y * voxelsRelosution.z;
			float textureDataMemorySize = textureResolution * 32 / (1024 * 1024 * 8); //32 bits
			float textureOccupancyMemorySize = (textureResolution * 8 / (1024 * 1024 * 8)); //8 bits
			textureOccupancyMemorySize *= 1.33f; //mipmaps
			float textureIntermediateMemorySize = ((textureResolution / (4^3)) * 8 / (1024 * 1024 * 8)); //8 bits

			if (voxelizationUpdateMode == VoxelizationUpdateMode.Partial)
				textureDataMemorySize *= 2f;

			return textureDataMemorySize + textureOccupancyMemorySize + textureIntermediateMemorySize;
		}

		public static Vector3Int CalculateVoxelResolution_UI(int voxelBounds, float density, bool overrideGroundEnable, int GroundLevel)
		{
			Vector3Int resolutionResult = new Vector3Int();
			float resolution = HMath.Remap(density, 0f, 1f, HConstants.ClampVoxelsResolution.x, HConstants.ClampVoxelsResolution.y);
			resolutionResult.x = Mathf.CeilToInt(resolution);
			resolutionResult.y = Mathf.CeilToInt(resolution);

			float height = (overrideGroundEnable == false ? voxelBounds : GroundLevel);
			resolutionResult.z = Mathf.CeilToInt(height / (voxelBounds / resolution));

			resolutionResult.x = HMath.DevisionBy32(resolutionResult.x);
			resolutionResult.y = HMath.DevisionBy32(resolutionResult.y);
			resolutionResult.z = HMath.DevisionBy32(resolutionResult.z);

			return resolutionResult;
		}

		public static Vector3 Truncate(this Vector3 input, int digits)
		{
			return new Vector3(input.x.RoundTail(digits), input.y.RoundTail(digits), input.z.RoundTail(digits));
		}

		public static Vector3 Ceil(this Vector3 input, int digits)
		{
			return new Vector3(input.x.RoundToCeilTail(digits), input.y.RoundToCeilTail(digits), input.z.RoundToCeilTail(digits));
		}

		public static float RoundTail(this float value, int digits)
		{
			float mult = Mathf.Pow(10.0f, digits);
			float result = Mathf.Round(mult * value) / mult;
			return result;
		}

		public static float RoundToCeilTail(this float value, int digits)
		{
			float mult = Mathf.Pow(10.0f, digits);
			float result = Mathf.Ceil(mult * value) / mult;
			return result;
		}

		public static int CalculateStepCountSSGI(float giRadius, float giAccuracy)
		{
			if (giRadius <= 25.0f)
			{
				//5 -> 16, 10 -> 20, 25 -> 25
				return Mathf.FloorToInt((-0.0233f * giRadius * giRadius + 1.15f * giRadius + 10.833f) * giAccuracy);
			}

			//50 -> 35, 100 -> 50, 150 -> 64
			return Mathf.FloorToInt((-0.0002f * giRadius * giRadius + 0.33f * giRadius + 19f) * giAccuracy);
		}

		internal static Vector3 OptimizeForVoxelization(this Vector3 position, VoxelizationExactData exactData)
		{
			Vector3 newPosition = new Vector3(Mathf.Round(position.x / exactData.VoxelSize) * exactData.VoxelSize,
				Mathf.Round(position.y / exactData.VoxelSize) * exactData.VoxelSize,
				Mathf.Round(position.z / exactData.VoxelSize) * exactData.VoxelSize);
			return newPosition;
		}

		private static int DevisionBy32(int value)
		{
			return value % 32 == 0 ? value : DevisionBy32(value + 1);
		}

		private static float ComputeViewportScale(int viewportSize, int bufferSize)
		{
			float rcpBufferSize = 1.0f / bufferSize;

			// Scale by (vp_dim / buf_dim).
			return viewportSize * rcpBufferSize;
		}

		private static float ComputeViewportLimit(int viewportSize, int bufferSize)
		{
			float rcpBufferSize = 1.0f / bufferSize;

			// Clamp to (vp_dim - 0.5) / buf_dim.
			return (viewportSize - 0.5f) * rcpBufferSize;
		}
	}
}
