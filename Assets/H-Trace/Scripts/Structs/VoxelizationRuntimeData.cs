using System;
using System.Collections.Generic;
using H_Trace.Scripts.VoxelCameras;
using UnityEngine;

namespace H_Trace.Scripts.Structs
{
	internal enum OffsetAxisIndex
	{
		AxisXPos = 0,
		AxisYPos = 1,
		AxisZPos = 2,
		AxisXNeg = 3,
		AxisYNeg = 4,
		AxisZNeg = 5,
	}

	internal enum OctantIndex
	{
		None           = 0,
		OctantA        = 1,
		OctantB        = 2,
		OctantC        = 3,
		OctantD        = 4,
		DynamicObjects = 5,
	}
	
	[Serializable]
	internal class VoxelizationRuntimeData
	{
		public VoxelCamera             VoxelCamera           { get; set; } = null;
		public VoxelCullingCamera      CullingCamera         { get; set; } = null;
		public VoxelOctantCamera       VoxelOctantCamera     { get; set; } = null;
		public HTraceDirectionalCamera FakeDirectionalCamera { get; set; } = null;

		public OffsetWorldPosition OffsetWorldPosition = OffsetWorldPosition.zero;
		public OctantIndex         OctantIndex         = OctantIndex.None;
		public OffsetAxisIndex     OffsetAxisIndex     = OffsetAxisIndex.AxisXPos;
		public bool                FullVoxelization;
		public uint                FrameCount;
		public bool                EvaluateHitLighting;
		public bool                VoxelizationModeChanged; //When we switch mode we got many errors with non alloc textures  

		[SerializeField] private float _prevDensityUI              = 0f;
		[SerializeField] private int   _prevVoxelBoundsUI          = 0;
		[SerializeField] private int   _prevOverrideBoundsHeightUI = 0;

		public Action OnReallocTextures;

		public VoxelizationRuntimeData()
		{
			FullVoxelization = true;
			VoxelizationModeChanged = true;
		}

		public void SetParamsForApplyButton(float prevDensityUI, int prevVoxelBoundsUI, int prevOverrideBoundsHeightUI)
		{
			_prevDensityUI              = prevDensityUI;
			_prevVoxelBoundsUI          = prevVoxelBoundsUI;
			_prevOverrideBoundsHeightUI = prevOverrideBoundsHeightUI;
		}

		public bool CheckBounds(float voxelDensity, int voxelBounds, int overrideBoundsHeight)
		{
			return Mathf.Abs(voxelDensity - _prevDensityUI) > Mathf.Epsilon || voxelBounds != _prevVoxelBoundsUI || overrideBoundsHeight != _prevOverrideBoundsHeightUI;
		}
	}

	internal struct OffsetWorldPosition
	{
		public float AxisXPos;
		public float AxisYPos;
		public float AxisZPos;
		public float AxisXNeg;
		public float AxisYNeg;
		public float AxisZNeg;

		public OffsetWorldPosition(float axisXPos, float axisYPos, float axisZPos, float axisXNeg, float axisYNeg, float axisZNeg)
		{
			AxisXPos = axisXPos;
			AxisYPos = axisYPos;
			AxisZPos = axisZPos;
			AxisXNeg = axisXNeg;
			AxisYNeg = axisYNeg;
			AxisZNeg = axisZNeg;
		}

		public static OffsetWorldPosition zero
		{
			get => OffsetWorldPosition.zeroVector;
		}

		private static readonly OffsetWorldPosition zeroVector = new OffsetWorldPosition(0.0f, 0.0f, 0.0f, 0.0f, 0.0f,
			0.0f);

		public static OffsetWorldPosition operator +(OffsetWorldPosition a, OffsetWorldPosition b)
		{
			return new OffsetWorldPosition(
				a.AxisXPos + b.AxisXPos,
				a.AxisYPos + b.AxisYPos,
				a.AxisZPos + b.AxisZPos,
				a.AxisXNeg + b.AxisXNeg,
				a.AxisYNeg + b.AxisYNeg,
				a.AxisZNeg + b.AxisZNeg
			);
		}

		public OffsetAxisIndex MaxAxisOffset()
		{
			Dictionary<OffsetAxisIndex, float> dictionary = new Dictionary<OffsetAxisIndex, float>()
			{
				{OffsetAxisIndex.AxisXPos, AxisXPos},
				{OffsetAxisIndex.AxisYPos, AxisYPos},
				{OffsetAxisIndex.AxisZPos, AxisZPos},
				{OffsetAxisIndex.AxisXNeg, AxisXNeg},
				{OffsetAxisIndex.AxisYNeg, AxisYNeg},
				{OffsetAxisIndex.AxisZNeg, AxisZNeg},
			};

			float           maxValue  = -1;
			OffsetAxisIndex axisIndex = OffsetAxisIndex.AxisXPos;
			foreach (var element in dictionary)
			{
				if (element.Value > maxValue)
				{
					axisIndex = element.Key;
					maxValue  = element.Value;
				}
			}

			return axisIndex;
		}
	}
}
