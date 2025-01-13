using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Burst;

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

        public JobHandle AddToGeometry(GeometryData* geo, JobHandle dependensOn)
        {
            JobHandle jobHandle = default;
            if(points.IsCreated && points.Length > 0)
            {
                geo->points.AddRange(points.AsArray());
            }
            if(meshs.IsCreated && meshs.Length > 0)
            {
                for (int i = 0; i < meshs.Length; ++i)
                {
                    jobHandle = JobHandle.CombineDependencies(jobHandle, meshs[i].AddToGeometry(geo, dependensOn));
                }
            }
            return jobHandle;
        }

        [BurstCompile]
        public struct CopyPointsToGeometryJob : IJob
        {
            [ReadOnly]
            public NativeArray<float3> positions_from;

            [WriteOnly]
            public NativeArray<float3> positions_to;

            public void Execute()
            {
                for (int i = 0; i < positions_from.Length; ++i)
                {
                    positions_to[i] = positions_from[i];
                }
            }
        }
    }
}
