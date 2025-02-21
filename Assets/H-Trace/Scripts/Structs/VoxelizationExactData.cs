using System;
using H_Trace.Scripts.Globals;
using UnityEngine;

namespace H_Trace.Scripts.Structs
{
	[Serializable]
	internal class VoxelizationExactData
	{
		public Vector3Int Resolution;
		public Vector3 Bounds;
		public Vector3 PreviousVoxelCameraPosition;

		public float VoxelSize
		{
			get { return Bounds.x / Resolution.x; }
		}

		public float VoxelsPerMeter
		{
			get { return Resolution.x / Bounds.x; }
		}

		public void UpdateData(VoxelizationData voxelizationData)
		{
			Vector3Int resolution = HMath.CalculateVoxelResolution(voxelizationData);
			var realBounds = new Vector3Int(voxelizationData.VoxelBounds, voxelizationData.VoxelBounds,
				voxelizationData.OverrideBoundsHeightEnable == false ? voxelizationData.VoxelBounds : voxelizationData.OverrideBoundsHeight);
			float realVoxelSize  = (float)realBounds.x / resolution.x;
			float exactVoxelSize = realVoxelSize.RoundToCeilTail(2);

			float maxVoxelSize = 0.16f; // default case
			if (HConfig.MAX_VOXEL_BOUNDS != 80) //not default case
			{
				maxVoxelSize = ((float)realBounds.x / resolution.x).RoundToCeilTail(2);
			}
			exactVoxelSize = Mathf.Clamp(exactVoxelSize, 0.08f, maxVoxelSize); // we want voxel size between 8 and 16 cm

			Resolution = resolution;
			Bounds     = new Vector3(resolution.x * exactVoxelSize, resolution.y * exactVoxelSize, resolution.z * exactVoxelSize);
		}
	}
}
