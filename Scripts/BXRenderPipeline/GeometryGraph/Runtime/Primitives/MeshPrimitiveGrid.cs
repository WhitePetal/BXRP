using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace BXGeometryGraph.Runtime
{
    static class MeshPrimitiveGrid
    {
        [BurstCompile]
        internal static MeshData CreateGridMesh(int verticesX, int verticesY, float sizeX, float sizeY)
        {
            int edgesX = verticesX - 1;
            int edgesY = verticesY - 1;

            MeshData mesh = new MeshData(verticesX * verticesY, edgesX * verticesY + edgesY * verticesX, edgesX + edgesY, edgesX * edgesY * 4);
            // mesh_smooth_set

            var positions = mesh.positions;
            var edges = mesh.edges;
            var cornerVertices = mesh.cornerVertices;
            var cornerEdges = mesh.cornerEdges;

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
            return mesh;
        }
    }
}
