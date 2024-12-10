using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    static class ValueUtilities
    {
        public static string ToGeometryString(this GeometryValueType type, string precisionToken = PrecisionUtil.Token)
        {
            switch (type)
            {
                case GeometryValueType.Boolean:
                    return precisionToken;
                case GeometryValueType.Float:
                    return precisionToken;
                case GeometryValueType.Float2:
                    return $"{precisionToken}2";
                case GeometryValueType.Float3:
                    return $"{precisionToken}3";
                case GeometryValueType.Float4:
                    return $"{precisionToken}4";
                case GeometryValueType.Matrix2:
                    return $"{precisionToken}2x2";
                case GeometryValueType.Matrix3:
                    return $"{precisionToken}3x3";
                case GeometryValueType.Matrix4:
                    return $"{precisionToken}4x4";
                case GeometryValueType.Integer:
                    return "int";
                case GeometryValueType.Uint:
                    return "uint";
                case GeometryValueType.Uint4:
                    return "uint4";
                default:
                    return "Error";
            }
        }

        public static int GetVectorCount(this GeometryValueType type)
        {
            switch (type)
            {
                case GeometryValueType.Float:
                    return 1;
                case GeometryValueType.Float2:
                    return 2;
                case GeometryValueType.Float3:
                    return 3;
                case GeometryValueType.Float4:
                    return 4;
                default:
                    return 0;
            }
        }
    }
}
