using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    internal static class BlockFields
    {
        [GenerateBlocks]
        public struct GeometryDescription
        {
            public static string name = "GeometryDescription";
            public static BlockFieldDescriptor Geometry = new BlockFieldDescriptor(GeometryDescription.name, "Geometry", "GEOMETRYDESCRIPTION_GEOMETRY",
                new GeometryControl(), GeometryStage.Geometry);
        }
    }
}
