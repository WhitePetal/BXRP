using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace BXGeometryGraph.Runtime
{
    public struct MeshData : IDisposable
    {
        public NativeArray<float3> positions;
        public NativeArray<int2> edges;
        public NativeArray<int> cornerVertices;
        public NativeArray<int> cornerEdges;

        public MeshData(int verticesNum, int edgesNum, int facesNum, int cornersNum)
        {
            positions = verticesNum == 0 ? default : new NativeArray<float3>(verticesNum, Allocator.Persistent);
            edges = edgesNum == 0 ? default : new NativeArray<int2>(edgesNum, Allocator.Persistent);
            cornerVertices = cornersNum == 0 ? default : new NativeArray<int>(cornersNum, Allocator.Persistent);
            cornerEdges = cornersNum == 0 ? default : new NativeArray<int>(cornersNum, Allocator.Persistent);
        }

        public void Dispose()
        {
            positions.Dispose();
            edges.Dispose();
            cornerVertices.Dispose();
            cornerEdges.Dispose();
        }
    }
}
