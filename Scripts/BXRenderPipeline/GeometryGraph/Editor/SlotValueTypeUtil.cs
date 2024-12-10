using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    static class SlotValueTypeUtil
    {
        public static PropertyType ToPropertyType(this ConcreteSlotValueType concreteValueType)
        {
            switch (concreteValueType)
            {
                case ConcreteSlotValueType.SamplerState:
                    return PropertyType.SamplerState;
                case ConcreteSlotValueType.Matrix4:
                    return PropertyType.Matrix4;
                case ConcreteSlotValueType.Matrix3:
                    return PropertyType.Matrix3;
                case ConcreteSlotValueType.Matrix2:
                    return PropertyType.Matrix2;
                case ConcreteSlotValueType.Texture2D:
                    return PropertyType.Texture2D;
                case ConcreteSlotValueType.Texture2DArray:
                    return PropertyType.Texture2DArray;
                case ConcreteSlotValueType.Texture3D:
                    return PropertyType.Texture3D;
                case ConcreteSlotValueType.Cubemap:
                    return PropertyType.Cubemap;
                case ConcreteSlotValueType.Gradient:
                    return PropertyType.Gradient;
                case ConcreteSlotValueType.Vector4:
                    return PropertyType.Vector4;
                case ConcreteSlotValueType.Vector3:
                    return PropertyType.Vector3;
                case ConcreteSlotValueType.Vector2:
                    return PropertyType.Vector2;
                case ConcreteSlotValueType.Vector1:
                    return PropertyType.Float;
                case ConcreteSlotValueType.Boolean:
                    return PropertyType.Boolean;
                case ConcreteSlotValueType.VirtualTexture:
                    return PropertyType.VirtualTexture;
                case ConcreteSlotValueType.PropertyConnectionState:
                    return PropertyType.PropertyConnectionState;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
