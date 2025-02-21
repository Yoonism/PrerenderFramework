using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace H_Trace._Temp.ProceduralRenderTests
{
	
	public class InstancingScriptTest : MonoBehaviour
	{
		public Material StandardMaterial;
		
		[Space]
		public Mesh Mesh;
		public float ObjectScale = 0.16f;
		
		[Space]
		[Range(0,5)]
		public float SpeedRadial = 1f;
		[Range(0.1f,20f)]
		public float Radius = 1f;
		[Range(10,50)]
		public int ObjectCount = 10;

		private readonly List<Matrix4x4> _matrices = new List<Matrix4x4>();

		private void Update()
		{
			GenerateInstanceAnimatedMatrix(transform.position, in _matrices);
			RenderParams rp1 = new RenderParams(StandardMaterial) { shadowCastingMode = ShadowCastingMode.On };
			Graphics.RenderMeshInstanced(rp1, Mesh, 0, _matrices);
		}
		
		private void GenerateInstanceAnimatedMatrix(in Vector3 origin, in List<Matrix4x4> matrices)
		{
			Matrix4x4 m = Matrix4x4.identity;
			Vector3 pos = Vector3.zero;

			matrices.Clear();

			for (int i = 0; i < ObjectCount; i++)
			{
				float angle = 2 * Mathf.PI * i / ObjectCount + Time.time * SpeedRadial;
				pos.x = Radius * Mathf.Cos(angle);
				pos.y = 0;
				pos.z = Radius * Mathf.Sin(angle);
				m.SetTRS(pos + origin, Quaternion.identity, Vector3.one * ObjectScale);
				matrices.Add(m);
			}
		}
	}
}