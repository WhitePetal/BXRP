using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    [Serializable]
    public enum SlotValueType
    {
        SamplerState,
        DynamicMatrix,
        Matrix4,
        Matrix3,
        Matrix2,
        Texture2D,
        Cubemap,
        Gradient,
        DynamicVector,
        Vector4,
        Vector3,
        Vector2,
        Vector1,
        Dynamic,
        Boolean
    }

    public enum ConcreteSlotValueType
    {
        SamplerState,
        Matrix4,
        Matrix3,
        Matrix2,
        Texture2D,
        Texture2DArray,
        Texture3D,
        Cubemap,
        Gradient,
        Vector4,
        Vector3,
        Vector2,
        Vector1,
        Boolean,
        VirtualTexture,
        PropertyConnectionState
    }

    public static class SlotValueHelper
    {
        public static int GetChannelCount(this ConcreteSlotValueType type)
        {
            switch (type)
            {
                case ConcreteSlotValueType.Vector4:
                    return 4;
                case ConcreteSlotValueType.Vector3:
                    return 3;
                case ConcreteSlotValueType.Vector2:
                    return 2;
                case ConcreteSlotValueType.Vector1:
                    return 1;
                default:
                    return 0;
            }
        }

		static readonly string[] k_ConcreteSlotValueTypeClassNames =
{
			null,
			"typeMatrix",
			"typeMatrix",
			"typeMatrix",
			"typeTexture2D",
			"typeCubemap",
			"typeGradient",
			"typeFloat4",
			"typeFloat3",
			"typeFloat2",
			"typeFloat1",
			"typeBoolean"
		};

		public static string ToClassName(this ConcreteSlotValueType type)
		{
			return k_ConcreteSlotValueTypeClassNames[(int)type];
		}

		public static string ToString(this ConcreteSlotValueType type, AbstractGeometryNode.OutputPrecision precision)
		{
			return NodeUtils.ConvertConcreteSlotValueTypeToString(precision, type);
		}
	}
}
