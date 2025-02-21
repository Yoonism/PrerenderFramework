namespace H_Trace.Scripts.Globals
{
	public static class HTraceNames
	{
		public const string HTRACE_NAME = "H-Trace";
		
		public const string HTRACE_PRE_PASS_NAME = "H-Trace Pre Pass";
		public const string HTRACE_VOXEL_PASS_NAME = "H-Trace Voxelization Pass";
		public const string HTRACE_MAIN_PASS_NAME = "H-Trace Main Pass";
		public const string HTRACE_FINAL_PASS_NAME = "H-Trace Final Pass";

		public const string HTRACE_PRE_PASS_NAME_FRAME_DEBUG             = "___HTrace Pre Pass"; //TODO: release delete
		public const string HTRACE_VOXEL_STAGGERED_PASS_NAME_FRAME_DEBUG = "___H-Trace Voxelizaion Staggered"; //TODO: release delete
		public const string HTRACE_VOXEL_CONSTANT_PASS_NAME_FRAME_DEBUG  = "___H-Trace Voxelizaion Constant"; //TODO: release delete
		public const string HTRACE_VOXEL_PARTIAL_PASS_NAME_FRAME_DEBUG   = "___H-Trace Voxelizaion Partial"; //TODO: release delete
		public const string HTRACE_MAIN_PASS_NAME_FRAME_DEBUG            = "___H-Trace Main Pass"; //TODO: release delete
		public const string HTRACE_FINAL_PASS_NAME_FRAME_DEBUG           = "___H-Trace Final Pass"; //TODO: release delete
		public const string HTRACE_VOXEL_CAMERA_NAME                     = "___HTrace Voxelization Camera";
		public const string HTRACE_VOXEL_CULLING_CAMERA_NAME             = "___HTrace Voxelization Culling Camera";
		public const string HTRACE_VOXEL_OCTANT_CAMERA_NAME              = "___HTrace Voxelization Octant Camera";

		//public const string HTRACE_PRE_PASS_NAME_FRAME_DEBUG             = "H-Trace Pre Pass";//TODO: release uncomment
		//public const string HTRACE_VOXEL_STAGGERED_PASS_NAME_FRAME_DEBUG = "H-Trace Voxelizaion Staggered";//TODO: release uncomment
		//public const string HTRACE_VOXEL_CONSTANT_PASS_NAME_FRAME_DEBUG  = "H-Trace Voxelizaion Constant";//TODO: release uncomment
		//public const string HTRACE_VOXEL_PARTIAL_PASS_NAME_FRAME_DEBUG   = "H-Trace Voxelizaion Partial";//TODO: release uncomment
		//public const string HTRACE_MAIN_PASS_NAME_FRAME_DEBUG            = "H-Trace Main Pass";//TODO: release uncomment
		//public const string HTRACE_FINAL_PASS_NAME_FRAME_DEBUG           = "H-Trace Final Pass";//TODO: release uncomment

		public const string KEYWORD_SWITCHER = "HTRACE_OVERRIDE";
		public const string KEYWORD_DEMO_SBS = "KEYWORD_DEMO_SBS"; //TODO: release delete

		public const string HTRACE_VOXELIZATION_SHADER_TAG_ID = "HTraceVoxelization";
	}
}
