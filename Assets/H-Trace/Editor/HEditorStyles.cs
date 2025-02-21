#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace H_Trace.Editor
{
	internal static class HEditorStyles
	{
		private static float defaultLineSpace    = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
		private static float additionalLineSpace = 10f;
		private static float helpBoxHeight       = EditorGUIUtility.singleLineHeight * 2;
		private static float checkBoxOffsetWidth = 15f;
		private static float checkBoxWidth       = 15f;
		private static float tabOffset           = 8f;
		
		// General Tab
		public static readonly GUIContent GlobalSettingsContent = new GUIContent("Global Settings");
		public static readonly GUIContent RayCountModeContent      = new GUIContent("Ray Count", "Define the pixel spacing between screen-space probes, affecting the number of probes spawned. Ray Count has the biggest impact on the overall performance , memory consumption and visual quality.");
		public static readonly GUIContent RayLengthContent      = new GUIContent("Ray Length", "Define the maximum distance a ray can travel in world space. Lower values improve performance but may cause rays to miss surfaces, resulting in darker and noisier GI. " +
		                                                                                       "Higher values can degrade performance when many rays travel long distances (e.g., toward the sky) without hitting any objects.");
		public static readonly GUIContent MultibounceContent      = new GUIContent("Multibounce");
		public static readonly GUIContent DebugModeContent      = new GUIContent("Debug Mode",    "Visualizes the debug mode for different rendering components of H-Trace.");

		// Screen Space Lighting Tab
		public static readonly GUIContent ScreenSpaceLightingContent  = new GUIContent("Screen Space Lighting");
		public static readonly GUIContent EvaluateHitLightingContent  = new GUIContent("Evaluate Hit Lighting", "Evaluate lighting at the hit points of screen-space rays instead of relying solely on the previous Color Buffer.");
		public static readonly GUIContent DirectionalOcclusionContent = new GUIContent("Directional Occlusion", "Enable directional screen space occlusion");
		public static readonly GUIContent OcclusionIntensityContent   = new GUIContent("Occlusion Intensity");

		// Voxelization Tab
		public static readonly GUIContent VoxelizationContent           = new GUIContent("Voxelization");
		public static readonly GUIContent VoxelizationMaskContent       = new GUIContent("Voxelization Mask", "Excludes objects (on a per-layer basis) from voxelization and has the highest priority over all other layer masks.");
		public static readonly GUIContent VoxelizationUpdateTypeContent = new GUIContent("Update Mode", "Define how voxel data will be updated.");
		public static readonly GUIContent DirectionalLightContent       = new GUIContent("Directional Light", "Main shadow-casting directional light.");
		public static readonly GUIContent AttachToContent               = new GUIContent("Attach To", "Anchor object for the voxelization bound. Voxelization will occur around this object and will follow it when it moves.");
		public static readonly GUIContent ExpandShadowmapContent        = new GUIContent("Expand Shadowmap", "Control the area covered by the custom directional shadowmap. The shadowmap is used to evaluate direct lighting and shadowing at hit points of world-space rays.");
		public static readonly GUIContent MaximumLodContent        = new GUIContent("Maximum LOD", "Maximum LOD used for the voxelization.");

		//Update Options
		public static readonly GUIContent DynamicObjectsMaskContent = new GUIContent("Dynamic Objects");
		public static readonly GUIContent CulledObjectsMaskContent  = new GUIContent("Culled Objects");
		public static readonly GUIContent ExpandCullFovContent      = new GUIContent("Expand Cull Fov");
		public static readonly GUIContent ExpandCullRadiusContent   = new GUIContent("Expand Cull Radius");

		//Parameters voxels
		public static readonly GUIContent CenterShiftContent                 = new GUIContent("Center Shift", "Shift center of voxelization.");
		public static readonly GUIContent VoxelDensityContent                = new GUIContent("Voxel Density", "Controls the resolution of the voxel volume (3D Texture). Lower values reduce the volume resolution, while higher values provide finer detail.");
		public static readonly GUIContent VoxelBoundsContent                 = new GUIContent("Voxel Bounds", "Controls the maximum size (in meters) that the voxelization bound can cover.");
		public static readonly GUIContent OverrideBoundsHeightEnableContent  = new GUIContent("Cap Bounds Height", "The maximum height of the voxelization bound. This parameter is particularly useful for \"flat\" levels with low verticality.");
		public static readonly GUIContent OverrideBoundsHeightEnableContent2 = new GUIContent("     Cap Bounds Height", "The maximum height of the voxelization bound. This parameter is particularly useful for \"flat\" levels with low verticality.");
		public static readonly GUIContent GroundLevelEnableContent           = new GUIContent("Ground Level", "Ensures that the voxelization bounds will always remain above this specified level.");
		public static readonly GUIContent GroundLevelEnableContent2          = new GUIContent("     Ground Level", "Ensures that the voxelization bounds will always remain above this specified level.");

		public static GUIStyle bold = new GUIStyle()
		{
			alignment = TextAnchor.MiddleLeft,
			margin = new RectOffset(),
			padding = new RectOffset(2, 0, 0, 0),
			fontSize = 12,
			normal = new GUIStyleState()
			{
				textColor = new Color(0.903f, 0.903f, 0.903f, 1f),
			},
			fontStyle = FontStyle.Bold,
		};
		
		public static GUIStyle hiddenFoldout = new GUIStyle()
		{
			alignment = TextAnchor.MiddleLeft,
			margin = new RectOffset(),
			padding = new RectOffset(),
			fontSize = 12,
			normal = new GUIStyleState()
			{
				//textColor = new Color(0.703f, 0.703f, 0.703f, 1f), //default color
				textColor = new Color(0.500f, 0.500f, 0.500f, 1f),
			},
			fontStyle = FontStyle.Bold,
		};

		public static GUIStyle headerFoldout = new GUIStyle()
		{
			alignment = TextAnchor.MiddleLeft,
			margin = new RectOffset(),
			padding = new RectOffset(),
			fontSize = 12,
			normal = new GUIStyleState()
			{
				textColor = new Color(0.903f, 0.903f, 0.903f, 1f),
			},
			fontStyle = FontStyle.Bold,
		};

		public static GUIStyle foldout = EditorStyles.foldout;
		
		public static GUIStyle VersionStyle = new GUIStyle(GUI.skin.label)
		{
			//padding = new RectOffset(left: 10, right: 10, top: 2, bottom: 2),
			fontStyle  = FontStyle.Bold,
			fontSize = 10,
		};

		public static Texture2D GetTexture2D(Color color)
		{
			Texture2D texture = new Texture2D(1, 1);
			texture.SetPixel(0, 0, color);
			texture.Apply();
			return texture;
		}

		//buttons gui styles
		public static Color warningBackgroundColor = new Color(2, 0, 0);
		public static Color warningColor           = new Color(1, 1, 1);

		public static GUIStyle standartButton = GUI.skin.button;
	}
}
#endif