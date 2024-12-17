using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    [Flags]
    public enum NeededCoordinateSpace
    {
        None = 0,
        Object = 1 << 0,
        View = 1 << 1,
        World = 1 << 2,
        Tangent = 1 << 3,
        AbsoluteWorld = 1 << 4,
        Screen = 1 << 5
    }

    public enum CoordinateSpace
    {
        Object,
        View,
        World,
        Tangent,
        AbsoluteWorld,
        Screen
    }

    public enum InterpolatorType
    {
        Normal,
        BiTangent,
        Tangent,
        ViewDirection,
        Position,
        PositionPredisplacement,
    }
}
