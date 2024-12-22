using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BXGeometryGraph
{
    [Title("Mesh", "Primitives", "Cube")]
    class CubeMeshNode : CodeFunctionNode
    {
        public CubeMeshNode()
        {
            name = "Cube";
            synonyms = new string[] { "cube", "cubemesh" };
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("BX_Cube_Mesh", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string BX_Cube_Mesh(
            [Slot(0, Binding.None, 1, 1, 1, 0)] Vector3 Size,
            [Slot(1, Binding.None, 2, 0, 0, 0)] CodeFunctionNode.Vector1 VerticesX,
            [Slot(2, Binding.None, 2, 0, 0, 0)] CodeFunctionNode.Vector1 VerticesY,
            [Slot(3, Binding.None, 2, 0, 0, 0)] CodeFunctionNode.Vector1 VerticesZ,
            [Slot(4, Binding.None)] out CodeFunctionNode.Mesh Mesh,
            [Slot(5, Binding.None)] out Vector2 UVMap
            )
        {
			UVMap = default;
            return
            @"
            {
                CubeMesh
            }
            ";
        }
    }
}
