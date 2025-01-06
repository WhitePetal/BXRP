using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BXGeometryGraph.Runtime;
using Unity.Mathematics;
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
            [Slot(1, Binding.None, 2, 0, 0, 0)] int VerticesX,
            [Slot(2, Binding.None, 2, 0, 0, 0)] int VerticesY,
            [Slot(3, Binding.None, 2, 0, 0, 0)] int VerticesZ,
            [Slot(4, Binding.None)] out Mesh Mesh,
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

        public override AbstractGeometryJob BuildGeometryJob()
        {
            AbstractGeometryJob[] depenedJobs = new AbstractGeometryJob[4];
            (ValueFrom sizeValueFrom, int sizeValueID, float3 sizeValueDefault) = GenerationUtils.GetSlotVector3DataForGeoJob(this, 0, depenedJobs);
            (ValueFrom verticesXValueFrom, int verticesXValueID, int verticesXValueDefault) = GenerationUtils.GetSlotIntDataForGeoJob(this, 1, depenedJobs);
            (ValueFrom verticesYValueFrom, int verticesYValueID, int verticesYValueDefault) = GenerationUtils.GetSlotIntDataForGeoJob(this, 2, depenedJobs);
            (ValueFrom verticesZValueFrom, int verticesZValueID, int verticesZValueDefault) = GenerationUtils.GetSlotIntDataForGeoJob(this, 3, depenedJobs);


            CubeMeshJobManaged job = new CubeMeshJobManaged(objectId, sizeValueFrom, verticesXValueFrom, verticesYValueFrom, verticesZValueFrom,
                sizeValueID, verticesXValueID, verticesYValueID, verticesZValueID,
                sizeValueDefault, verticesXValueDefault, verticesYValueDefault, verticesZValueDefault);
            return job;
        }
    }
}
