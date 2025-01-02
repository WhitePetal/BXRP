using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace BXGeometryGraph.Runtime
{
	[Serializable]
	public struct GeometryData : IDisposable
	{
		[SerializeField]
		public NativeList<float3> points;
		[SerializeField]
		public NativeList<MeshData> meshs;

		public void Init()
        {
			points = new NativeList<float3>(Allocator.Persistent);
			meshs = new NativeList<MeshData>(Allocator.Persistent);
        }

		public void Clear()
        {
			points.Clear();
			for(int i = 0; i < meshs.Length; ++i)
            {
				meshs[i].Dispose();
            }
			meshs.Clear();
        }

        public void Dispose()
        {
			points.Dispose();
			for(int i = 0; i < meshs.Length; ++i)
            {
				meshs[i].Dispose();
            }
			meshs.Dispose();
        }
    }
}
