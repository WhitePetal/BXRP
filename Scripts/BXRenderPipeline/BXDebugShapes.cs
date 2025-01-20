using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXRenderPipeline
{
    public static class BXDebugShapes
    {
        // This code has been grabbed from http://wiki.unity3d.com/index.php/ProceduralPrimitives
        private static void BuildSphere(ref Mesh outputMesh, float radius, uint longSubdiv, uint latSubdiv)
        {
            // Make sure it is empty before pushing anything to it
            outputMesh.Clear();

            // Build the vertices array
            Vector3[] vertices = new Vector3[(longSubdiv + 1) * latSubdiv + 2];
            float _pi = Mathf.PI;
            float _2pi = _pi * 2f;

            vertices[0] = Vector3.up * radius;
            for (int lat = 0; lat < latSubdiv; lat++)
            {
                float a1 = _pi * (float)(lat + 1) / (latSubdiv + 1);
                float sin1 = Mathf.Sin(a1);
                float cos1 = Mathf.Cos(a1);

                for (int lon = 0; lon <= longSubdiv; lon++)
                {
                    float a2 = _2pi * (float)(lon == longSubdiv ? 0 : lon) / longSubdiv;
                    float sin2 = Mathf.Sin(a2);
                    float cos2 = Mathf.Cos(a2);

                    vertices[lon + lat * (longSubdiv + 1) + 1] = new Vector3(sin1 * cos2, cos1, sin1 * sin2) * radius;
                }
            }
            vertices[vertices.Length - 1] = Vector3.up * -radius;

            // Build the normals array
            Vector3[] normals = new Vector3[vertices.Length];
            for (int n = 0; n < vertices.Length; n++)
            {
                normals[n] = vertices[n].normalized;
            }

            // Build the UV array
            Vector2[] uvs = new Vector2[vertices.Length];
            uvs[0] = Vector2.up;
            uvs[uvs.Length - 1] = Vector2.zero;
            for (int lat = 0; lat < latSubdiv; lat++)
            {
                for (int lon = 0; lon <= longSubdiv; lon++)
                {
                    uvs[lon + lat * (longSubdiv + 1) + 1] = new Vector2((float)lon / longSubdiv, 1f - (float)(lat + 1) / (latSubdiv + 1));
                }
            }

            // Build the index array
            uint nbTriangles = longSubdiv * 2 +                    // Top and bottom cap
                               (latSubdiv - 1) * longSubdiv * 2;   // Middle part
            uint nbIndexes = nbTriangles * 3;
            int[] triangles = new int[nbIndexes];

            // Top Cap
            int i = 0;
            for (int lon = 0; lon < longSubdiv; lon++)
            {
                triangles[i++] = lon + 2;
                triangles[i++] = lon + 1;
                triangles[i++] = 0;
            }

            //Middle
            for (uint lat = 0; lat < latSubdiv - 1; lat++)
            {
                for (uint lon = 0; lon < longSubdiv; lon++)
                {
                    uint current = lon + lat * (longSubdiv + 1) + 1;
                    uint next = current + longSubdiv + 1;

                    triangles[i++] = (int)current;
                    triangles[i++] = (int)current + 1;
                    triangles[i++] = (int)next + 1;

                    triangles[i++] = (int)current;
                    triangles[i++] = (int)next + 1;
                    triangles[i++] = (int)next;
                }
            }

            // Bottom Cap
            for (int lon = 0; lon < longSubdiv; lon++)
            {
                triangles[i++] = vertices.Length - 1;
                triangles[i++] = vertices.Length - (lon + 2) - 1;
                triangles[i++] = vertices.Length - (lon + 1) - 1;
            }

            // Assign them to
            outputMesh.vertices = vertices;
            outputMesh.normals = normals;
            outputMesh.uv = uvs;
            outputMesh.triangles = triangles;

            outputMesh.RecalculateBounds();
        }
        /// <summary>
        /// Build a custom Sphere Mesh
        /// </summary>
        /// <param name="radius">The radius of the generated sphere</param>
        /// <param name="longSubdiv">The number of subdivisions along the equator of the sphere. Must be at least 3 to give a relevant shape.</param>
        /// <param name="latSubdiv">The number of subdivisions from north to south. Must be at least 1 to give a relevant shape.</param>
        /// <returns>A Sphere Mesh</returns>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// Mesh lowPolyDebugMesh = BXDebugShapes.BuildCustomSphereMesh(0.5f, 9, 8); // Generates a 82 vert sphere
        /// ]]>
        /// </code>
        /// </example>
        public static Mesh BuildCustomSphereMesh(float radius, uint longSubdiv, uint latSubdiv)
        {
            Mesh sphereMesh = new Mesh();
            BuildSphere(ref sphereMesh, radius, longSubdiv, latSubdiv);
            return sphereMesh;
        }
    }
}
