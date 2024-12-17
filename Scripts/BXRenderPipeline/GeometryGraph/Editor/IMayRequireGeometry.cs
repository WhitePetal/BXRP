using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    interface IMayRequireGeometry
    {
        bool RequiresGeometry(GeometryStageCapability stageCapability = GeometryStageCapability.All);
    }

    static class MayRequireGeometryExtensions
    {
        public static bool RequiresGeometry(this GeometrySlot slot)
        {
            var mayRequireGeometry = slot as IMayRequireGeometry;
            return mayRequireGeometry != null ? mayRequireGeometry.RequiresGeometry() : false;
        }
    }
}
