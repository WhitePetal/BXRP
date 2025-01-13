using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace BXGeometryGraph.Runtime
{
    public unsafe struct MeshData : IDisposable
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

        private bool disposed;

        public MeshData(int verts_num, int edges_num, int faces_num, int corners_num, Allocator allocator)
        {
            positions = new NativeArray<float3>(verts_num, allocator);
            edges = new NativeArray<int2>(edges_num, allocator);
            corner_verts = new NativeArray<int>(corners_num, allocator);
            corner_edges = new NativeArray<int>(corners_num, allocator);
            face_offset_indices = new NativeArray<int>(faces_num + 1, allocator);

            face_offset_indices[0] = 0;
            face_offset_indices[faces_num] = corners_num;

            this.verts_num = verts_num;
            this.edges_num = edges_num;
            this.faces_num = faces_num;
            this.corners_num = corners_num;

            disposed = false;
        }

        public JobHandle AddToGeometry(GeometryData* geo, JobHandle dependensOn)
        {
            MeshData mesh = new MeshData(verts_num, edges_num, faces_num, corners_num, Allocator.Persistent);
            geo->meshs.Add(mesh);
            CopyToGeometryJob job = new CopyToGeometryJob()
            {
                positions_from = positions,
                edges_from = edges,
                corner_verts_from = corner_verts,
                corner_edges_from = corner_edges,
                face_offset_indices_from = face_offset_indices,

                positions_to = mesh.positions,
                edges_to = mesh.edges,
                corner_verts_to = mesh.corner_verts,
                corner_edges_to = mesh.corner_edges,
                face_offset_indices_to = mesh.face_offset_indices
            };
            return job.Schedule(dependensOn);
        }

        [BurstCompile]
        public struct CopyToGeometryJob : IJob
        {
            [ReadOnly]
            public NativeArray<float3> positions_from;
            [ReadOnly]
            public NativeArray<int2> edges_from;
            [ReadOnly]
            public NativeArray<int> corner_verts_from;
            [ReadOnly]
            public NativeArray<int> corner_edges_from;
            [ReadOnly]
            public NativeArray<int> face_offset_indices_from;

            [WriteOnly]
            public NativeArray<float3> positions_to;
            [WriteOnly]
            public NativeArray<int2> edges_to;
            [WriteOnly]
            public NativeArray<int> corner_verts_to;
            [WriteOnly]
            public NativeArray<int> corner_edges_to;
            [WriteOnly]
            public NativeArray<int> face_offset_indices_to;

            public void Execute()
            {
                for(int i = 0; i < positions_from.Length; ++i)
                {
                    positions_to[i] = positions_from[i];
                }
                for (int i = 0; i < edges_from.Length; ++i)
                {
                    edges_to[i] = edges_from[i];
                }
                for (int i = 0; i < corner_verts_from.Length; ++i)
                {
                    corner_verts_to[i] = corner_verts_from[i];
                }
                for (int i = 0; i < corner_edges_from.Length; ++i)
                {
                    corner_edges_to[i] = corner_edges_from[i];
                }
                for (int i = 0; i < face_offset_indices_from.Length; ++i)
                {
                    face_offset_indices_to[i] = face_offset_indices_from[i];
                }
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            positions.Dispose();
            edges.Dispose();
            corner_verts.Dispose();
            corner_edges.Dispose();
            face_offset_indices.Dispose();
            disposed = true;
        }

        public JobHandle Dispose(JobHandle depend)
        {
            if (disposed)
                return new JobHandle();

            JobHandle handle = JobHandle.CombineDependencies(positions.Dispose(depend), edges.Dispose(depend), corner_verts.Dispose(depend));
            handle = JobHandle.CombineDependencies(handle, corner_edges.Dispose(depend), face_offset_indices.Dispose(depend));
            disposed = true;
            return handle;
        }
    }
}
