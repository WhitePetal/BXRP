using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    static class SlotValueTypeUtil
    {
        public static SlotValueType ToSlotValueType(this ConcreteSlotValueType concreteValueType)
        {
            switch (concreteValueType)
            {
                case ConcreteSlotValueType.SamplerState:
                    return SlotValueType.SamplerState;
                case ConcreteSlotValueType.Matrix4:
                    return SlotValueType.Matrix4;
                case ConcreteSlotValueType.Matrix3:
                    return SlotValueType.Matrix3;
                case ConcreteSlotValueType.Matrix2:
                    return SlotValueType.Matrix2;
                case ConcreteSlotValueType.Texture2D:
                    return SlotValueType.Texture2D;
                case ConcreteSlotValueType.Texture2DArray:
                    return SlotValueType.Texture2DArray;
                case ConcreteSlotValueType.Texture3D:
                    return SlotValueType.Texture3D;
                case ConcreteSlotValueType.Cubemap:
                    return SlotValueType.Cubemap;
                case ConcreteSlotValueType.Gradient:
                    return SlotValueType.Gradient;
                case ConcreteSlotValueType.Vector4:
                    return SlotValueType.Vector4;
                case ConcreteSlotValueType.Vector3:
                    return SlotValueType.Vector3;
                case ConcreteSlotValueType.Vector2:
                    return SlotValueType.Vector2;
                case ConcreteSlotValueType.Vector1:
                    return SlotValueType.Vector1;
                case ConcreteSlotValueType.Boolean:
                    return SlotValueType.Boolean;
                case ConcreteSlotValueType.VirtualTexture:
                    return SlotValueType.VirtualTexture;
                case ConcreteSlotValueType.PropertyConnectionState:
                    return SlotValueType.PropertyConnectionState;
                case ConcreteSlotValueType.Geometry:
                    return SlotValueType.Geometry;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

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
                case ConcreteSlotValueType.Geometry:
                    return PropertyType.Geometry;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static string ToGeometryString(this ConcreteSlotValueType type, string precisionToken = PrecisionUtil.Token)
        {
            switch (type)
            {
                case ConcreteSlotValueType.SamplerState:
                    return "UnitySamplerState";
                case ConcreteSlotValueType.Matrix4:
                    return precisionToken + "4x4";
                case ConcreteSlotValueType.Matrix3:
                    return precisionToken + "3x3";
                case ConcreteSlotValueType.Matrix2:
                    return precisionToken + "2x2";
                case ConcreteSlotValueType.Texture2D:
                    return "UnityTexture2D";
                case ConcreteSlotValueType.Texture2DArray:
                    return "UnityTexture2DArray";
                case ConcreteSlotValueType.Texture3D:
                    return "UnityTexture3D";
                case ConcreteSlotValueType.Cubemap:
                    return "UnityTextureCube";
                case ConcreteSlotValueType.Gradient:
                    return "Gradient";
                case ConcreteSlotValueType.Vector4:
                    return precisionToken + "4";
                case ConcreteSlotValueType.Vector3:
                    return precisionToken + "3";
                case ConcreteSlotValueType.Vector2:
                    return precisionToken + "2";
                case ConcreteSlotValueType.Vector1:
                    return precisionToken;
                case ConcreteSlotValueType.Boolean:
                    return precisionToken;
                case ConcreteSlotValueType.PropertyConnectionState:
                    return "bool";
                case ConcreteSlotValueType.Geometry:
                    return "Geometry";
                default:
                    return "Error";
            }
        }

        public static string ToGeometryString(this ConcreteSlotValueType type, ConcretePrecision concretePrecision, string precisionToken = PrecisionUtil.Token)
        {
            string precisionString = concretePrecision.ToGeometryString();
            return type.ToGeometryString(precisionString);
        }
    }
}
