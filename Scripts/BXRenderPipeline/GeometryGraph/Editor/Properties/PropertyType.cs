using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    [GenerationAPI]
    public enum PropertyType
    {
        Color,
        Texture2D,
        Texture2DArray,
        Texture3D,
        Cubemap,
        Gradient,
        Boolean,
        Float,
        Vector2,
        Vector3,
        Vector4,
        Matrix2,
        Matrix3,
        Matrix4,
        SamplerState,
        VirtualTexture,
        PropertyConnectionState
    }
}