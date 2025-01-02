using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace BXGeometryGraph.Runtime
{
    static class MeshPrimitiveLine
    {
        [BurstCompile]
        internal static MeshData CreateLineMesh(float3 start, float3 delta, int count)
        {
            if (count < 1)
                return new MeshData();

            int edgesNum = count - 1;
            MeshData mesh = new MeshData(count, edgesNum, 0, 0);
            var postitions = mesh.positions;
            var edges = mesh.edges;
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
            return mesh;
        }
    }
}
