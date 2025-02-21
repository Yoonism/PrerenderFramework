using UnityEngine.Rendering.HighDefinition;

namespace H_Trace.Scripts.Globals
{
	public enum DebugModeWS
	{
		None = 0,
		GlobalIllumination,
		GeometryNormals,
		Shadowmap,
		VoxelizedColor,
		VoxelizedLighting,
	}

	public enum VoxelizationUpdateMode
	{
		//todo: stuggered uncomment
		//Staggered = 0,
		Constant = 0,
		Partial
	}

	public enum RayCountMode
	{
		Performance = 0,
		Quality,
		Cinematic
	}

	public enum HInjectionPoint
	{
		AfterOpaqueDepthAndNormal = CustomPassInjectionPoint.AfterOpaqueDepthAndNormal,
		BeforeTransparent = CustomPassInjectionPoint.BeforeTransparent,
		BeforePostProcess = CustomPassInjectionPoint.BeforePostProcess,
	}

	public enum Multibounce
	{
		None = 0,
		IrradianceCache = 1,
		AdaptiveProbeVolumes,
	}
}
