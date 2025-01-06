using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BXGeometryGraph.Runtime;
using Unity.Mathematics;
using UnityEngine;

namespace BXGeometryGraph
{
    [Title("Geometry", "Write", "SetPosition")]
    internal class SetPositionNode : CodeFunctionNode
    {
        public SetPositionNode()
        {
            name = "SetPosition";
            synonyms = new string[] { "setposition", "set position", "position" };
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("BX_Set_Position", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string BX_Set_Position(
            [Slot(0, Binding.None)] Geometry Geometry,
            [Slot(1, Binding.None, 1, 1, 1, 1)] Boolean Selection,
            [Slot(2, Binding.None)] Vector3 Position,
            [Slot(3, Binding.None)] Vector3 Offset,
            [Slot(4, Binding.None)] out Geometry _Geometry
        )
        {
            _Geometry = default;
            return
            @"
            {
                SetPosition
            }
            ";
        }

        public override AbstractGeometryJob BuildGeometryJob()
        {
            AbstractGeometryJob[] depenedJobs = new AbstractGeometryJob[4];
            (ValueFrom geometryValueFrom, int geometryValueID) = GenerationUtils.GetSlotGeometryDataForGeoJob(this, 0, depenedJobs);
            (ValueFrom selectionValueFrom, int selectionValueID, bool selectionValueDefault) = GenerationUtils.GetSlotBooleanDataForGeoJob(this, 1, depenedJobs);
            (ValueFrom positionValueFrom, int positionValueID, float3 positionValueDefault) = GenerationUtils.GetSlotVector3DataForGeoJob(this, 2, depenedJobs);
            (ValueFrom offsetValueFrom, int offsetValueID, float3 offsetValueDefault) = GenerationUtils.GetSlotVector3DataForGeoJob(this, 3, depenedJobs);

            SetPositionJob job = new SetPositionJob(objectId, geometryValueFrom, selectionValueFrom, positionValueFrom, offsetValueFrom,
                geometryValueID, selectionValueID, positionValueID, offsetValueID,
                selectionValueDefault, positionValueDefault, offsetValueDefault);
            return job;
        }
    }
}
