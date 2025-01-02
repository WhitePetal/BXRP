using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace BXGeometryGraph.Runtime
{
    static class MeshPrimitiveLine
    {
        [BurstCompile]
        internal static (JobHandle, MeshData) create_line_mesh(float3 start, float3 delta, int count, JobHandle dependsOn = default)
        {
            if (count < 1)
                return (default, new MeshData());

            int edgesNum = count - 1;
            MeshData mesh = new MeshData(count, edgesNum, 0, 0);
            var postitions = mesh.positions;
            var edges = mesh.edges;

            create_line_mesh_job job = new create_line_mesh_job()
            {
                start = start,
                delta = delta,
                postitions = postitions,
                edges = edges
            };
            JobHandle jobHandle = job.Schedule(dependsOn);

            return (jobHandle, mesh);
        }

        [BurstCompile]
        private struct create_line_mesh_job : IJob
        {
            public float3 start;
            public float3 delta;

            [WriteOnly]
            public NativeArray<float3> postitions;
            [WriteOnly]
            public NativeArray<int2> edges;

            public void Execute()
            {
                for (int i = 0; i < postitions.Length; ++i)
                {
                    postitions[i] = start + delta * i;
                }
                for (int i = 0; i < edges.Length; ++i)
                {
                    int2 e;
                    e.x = i;
                    e.y = i + 1;
                    edges[i] = e;
                }
            }
        }
    }
}
