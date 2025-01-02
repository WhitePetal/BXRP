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
        public NativeArray<int> corner_verts;
        public NativeArray<int> corner_edges;
        public NativeArray<int> face_offset_indices;

        public int verts_num;
        public int edges_num;
        public int faces_num;
        public int corners_num;

        public MeshData(int verts_num, int edges_num, int faces_num, int corners_num)
        {
            positions = new NativeArray<float3>(verts_num, Allocator.Persistent);
            edges = new NativeArray<int2>(edges_num, Allocator.Persistent);
            corner_verts = new NativeArray<int>(corners_num, Allocator.Persistent);
            corner_edges = new NativeArray<int>(corners_num, Allocator.Persistent);
            face_offset_indices = new NativeArray<int>(faces_num + 1, Allocator.Persistent);

            face_offset_indices[0] = 0;
            face_offset_indices[faces_num] = corners_num;

            this.verts_num = verts_num;
            this.edges_num = edges_num;
            this.faces_num = faces_num;
            this.corners_num = corners_num;
        }

        public void Dispose()
        {
            positions.Dispose();
            edges.Dispose();
            corner_verts.Dispose();
            corner_edges.Dispose();
            face_offset_indices.Dispose();
        }
    }
}
