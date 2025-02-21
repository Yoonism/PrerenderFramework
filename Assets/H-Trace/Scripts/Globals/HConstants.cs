using UnityEngine;

namespace H_Trace.Scripts.Globals
{
	internal static class HConstants
	{
		internal const int OCTANTS_FRAMES_LENGTH = 5;
		internal const int MAX_LOD_LEVEL = 10;

		internal static Vector2 ClampVoxelsResolution = new Vector2(64f, 512f); //min - 64 VoxelResolution, max - 512 VoxelResolution
	}
}
