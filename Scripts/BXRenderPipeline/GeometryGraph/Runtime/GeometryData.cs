using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;

namespace BXGeometryGraph.Runtime
{
	[Serializable]
	public unsafe struct GeometryData : IDisposable
	{
		[SerializeField]
		public NativeList<float3> points;
		[SerializeField]
		public NativeList<MeshData> meshs;

		public void CreatePersistent()
        {
			//points = UnsafeUtility.AsRef<NativeList<float3>>((NativeList<float3>*)UnsafeUtility.Malloc(sizeof(NativeList<float3>), UnsafeUtility.AlignOf<NativeList<float3>>(), Allocator.Persistent));
			points = new NativeList<float3>(Allocator.Persistent);
			meshs = new NativeList<MeshData>(Allocator.Persistent);
        }

		public void CreateEmpty(Allocator allocator)
        {
			points = new NativeList<float3>(0, allocator);
			meshs = new NativeList<MeshData>(0, allocator);
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
