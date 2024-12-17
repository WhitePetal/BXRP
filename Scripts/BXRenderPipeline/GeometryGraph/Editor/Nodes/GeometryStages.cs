using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    [Flags]
    enum GeometryStageCapability
    {
        None = 0,
        Vertex = 1 << 0,
        Fragment = 1 << 1,
        Geometry = 1 << 2,
        All = Vertex | Fragment | Geometry
    }

    enum GeometryStage
    {
        Vertex = 1 << 0,
        Fragment = 1 << 1,
        Geometry = 1 << 2
    }

    static class GeometryStageExtensions
    {
        /// <summary>
        /// Tries to convert a GeometryStageCapability into a GeometryStage. The conversion is only successful if the given ShaderStageCapability <paramref name="capability"/> refers to exactly 1 shader stage.
        /// </summary>
        /// <param name="capability">The GeometryStageCapability to convert.</param>
        /// <param name="stage">If <paramref name="capability"/> refers to exactly 1 geometry stage, this parameter will contain the equivalent GeometryStage of that. Otherwise the value is undefined.</param>
        /// <returns>True is <paramref name="capability"/> holds exactly 1 shader stage.</returns>
        public static bool TryGetShaderStage(this GeometryStageCapability capability, out GeometryStage stage)
        {
            switch (capability)
            {
                case GeometryStageCapability.Vertex:
                    stage = GeometryStage.Vertex;
                    return true;
                case GeometryStageCapability.Fragment:
                    stage = GeometryStage.Fragment;
                    return true;
                case GeometryStageCapability.Geometry:
                    stage = GeometryStage.Geometry;
                    return true;
                default:
                    stage = GeometryStage.Geometry;
                    return false;
            }
        }

        public static GeometryStageCapability GetGeometryStageCapability(this GeometryStage stage)
        {
            switch (stage)
            {
                case GeometryStage.Vertex:
                    return GeometryStageCapability.Vertex;
                case GeometryStage.Fragment:
                    return GeometryStageCapability.Fragment;
                case GeometryStage.Geometry:
                    return GeometryStageCapability.Geometry;
                default:
                    return GeometryStageCapability.Geometry;
            }
        }
    }
}
