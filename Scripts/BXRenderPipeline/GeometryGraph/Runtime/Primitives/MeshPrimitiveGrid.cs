using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace BXGeometryGraph.Runtime
{
    static class MeshPrimitiveGrid
    {
        [BurstCompile]
        internal static (JobHandle, MeshData) create_grid_mesh(int verticesX, int verticesY, float sizeX, float sizeY, JobHandle dependsOn = default)
        {
            int edgesX = verticesX - 1;
            int edgesY = verticesY - 1;

            MeshData mesh = new MeshData(verticesX * verticesY, edgesX * verticesY + edgesY * verticesX, edgesX + edgesY, edgesX * edgesY * 4);

            create_grid_mesh_job job = new create_grid_mesh_job()
            {
                edgesX = edgesX,
                edgesY = edgesY,
                sizeX = sizeX,
                sizeY = sizeY,
                verticesX = verticesX,
                verticesY = verticesY,
                positions = mesh.positions,
                edges = mesh.edges,
                cornerVertices = mesh.corner_verts,
                cornerEdges = mesh.corner_edges
            };
            JobHandle jobHandle = job.Schedule(dependsOn);

            return (jobHandle, mesh);
        }

        [BurstCompile]
        private struct create_grid_mesh_job : IJob
        {
            public int edgesX;
            public int edgesY;
            public float sizeX;
            public float sizeY;
            public int verticesX;
            public int verticesY;

            [WriteOnly]
            public NativeArray<float3> positions;
            [WriteOnly]
            public NativeArray<int2> edges;
            [WriteOnly]
            public NativeArray<int> cornerVertices;
            [WriteOnly]
            public NativeArray<int> cornerEdges;


            public void Execute()
            {
                // mesh_smooth_set

                float dx = edgesX == 0 ? 0f : sizeX / edgesX;
                float dy = edgesY == 0 ? 0f : sizeY / edgesY;
                float xShift = edgesX * 0.5f;
                float yShift = edgesY * 0.5f;
                for (int x = 0; x < verticesX; ++x)
                {
                    int yoffset = x * verticesY;
                    for (int y = 0; y < verticesY; ++y)
                    {
                        int vertexIndex = yoffset + y;
                        float3 pos = new float3(
                            (x - xShift) * dx,
                            (y - yShift) * dy,
                            0f);
                        positions[vertexIndex] = pos;
                    }
                }

                int yEdgesStart = 0;
                int xEdgesStart = verticesX * edgesY;

                /* Build the horizontal edges in the X direction. */
                for (int x = 0; x < verticesX; ++x)
                {
                    int yVertexOffset = x * verticesY;
                    int yEdgeOffset = yEdgesStart + x * edgesY;
                    for (int y = 0; y < edgesY; ++y)
                    {
                        int vertexIndex = yVertexOffset + y;
                        edges[yEdgeOffset + y] = new int2(vertexIndex, vertexIndex + 1);
                    }
                }

                /* Build the vertical edges in the Y direction. */
                for (int y = 0; y < verticesY; ++y)
                {
                    int xEdgeOffset = xEdgesStart + y * edgesX;
                    for (int x = 0; x < edgesX; ++x)
                    {
                        int vertexIndex = x * verticesY + y;
                        edges[xEdgeOffset + x] = new int2(vertexIndex, vertexIndex + verticesY);
                    }
                }

                for (int x = 0; x < edgesX; ++x)
                {
                    int yOffset = x * edgesY;
                    for (int y = 0; y < edgesY; ++y)
                    {
                        int faceIndex = yOffset + y;
                        int loopIndex = faceIndex * 4;
                        int vertexIndex = x * verticesY + y;

                        cornerVertices[loopIndex] = vertexIndex;
                        cornerEdges[loopIndex] = xEdgesStart + edgesX * y + x;

                        cornerVertices[loopIndex + 1] = vertexIndex + verticesY;
                        cornerEdges[loopIndex + 1] = yEdgesStart + edgesY * (x + 1) + y;

                        cornerVertices[loopIndex + 2] = vertexIndex + verticesY + 1;
                        cornerEdges[loopIndex + 2] = xEdgesStart + edgesX * (y + 1) + x;

                        cornerVertices[loopIndex + 3] = vertexIndex + 1;
                        cornerEdges[loopIndex + 3] = yEdgesStart + edgesY * x + y;
                    }
                }

                // calculate_uvs
            }
        }
    }
}
