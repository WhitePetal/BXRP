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
        Texture2DArray,
        Texture3D,
        Cubemap,
        Gradient,
        DynamicVector,
        Vector4,
        Vector3,
        Vector2,
        Vector1,
        Dynamic,
        Boolean,
        VirtualTexture,
        PropertyConnectionState,
        Geometry,
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
        PropertyConnectionState,
        Geometry
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

        static Dictionary<ConcreteSlotValueType, List<SlotValueType>> s_ValidConversions;
        static List<SlotValueType> s_ValidSlotTypes;
        public static bool AreCompatible(SlotValueType inputType, ConcreteSlotValueType outputType, bool outputTypeIsConnectionTestable = false)
        {
            if (s_ValidConversions == null)
            {
                var validVectors = new List<SlotValueType>()
                {
                    SlotValueType.Dynamic, SlotValueType.DynamicVector,
                    SlotValueType.Vector1, SlotValueType.Vector2, SlotValueType.Vector3, SlotValueType.Vector4,
                    SlotValueType.Boolean
                };

                s_ValidConversions = new Dictionary<ConcreteSlotValueType, List<SlotValueType>>()
                {
                    {ConcreteSlotValueType.Boolean, new List<SlotValueType>() {SlotValueType.Boolean, SlotValueType.Dynamic, SlotValueType.DynamicVector, SlotValueType.Vector1, SlotValueType.Vector2, SlotValueType.Vector3, SlotValueType.Vector4}},
                    {ConcreteSlotValueType.Vector1, validVectors},
                    {ConcreteSlotValueType.Vector2, validVectors},
                    {ConcreteSlotValueType.Vector3, validVectors},
                    {ConcreteSlotValueType.Vector4, validVectors},
                    {ConcreteSlotValueType.Matrix2, new List<SlotValueType>()
                     {SlotValueType.Dynamic, SlotValueType.DynamicMatrix, SlotValueType.Matrix2}},
                    {ConcreteSlotValueType.Matrix3, new List<SlotValueType>()
                     {SlotValueType.Dynamic, SlotValueType.DynamicMatrix, SlotValueType.Matrix2, SlotValueType.Matrix3}},
                    {ConcreteSlotValueType.Matrix4, new List<SlotValueType>()
                     {SlotValueType.Dynamic, SlotValueType.DynamicMatrix, SlotValueType.Matrix2, SlotValueType.Matrix3, SlotValueType.Matrix4}},
                    {ConcreteSlotValueType.Texture2D, new List<SlotValueType>() {SlotValueType.Texture2D}},
                    {ConcreteSlotValueType.Texture3D, new List<SlotValueType>() {SlotValueType.Texture3D}},
                    {ConcreteSlotValueType.Texture2DArray, new List<SlotValueType>() {SlotValueType.Texture2DArray}},
                    {ConcreteSlotValueType.Cubemap, new List<SlotValueType>() {SlotValueType.Cubemap}},
                    {ConcreteSlotValueType.SamplerState, new List<SlotValueType>() {SlotValueType.SamplerState}},
                    {ConcreteSlotValueType.Gradient, new List<SlotValueType>() {SlotValueType.Gradient}},
                    {ConcreteSlotValueType.VirtualTexture, new List<SlotValueType>() {SlotValueType.VirtualTexture}},
                    {ConcreteSlotValueType.Geometry, new List<SlotValueType>(){SlotValueType.Geometry} },
                };
            }

            if (inputType == SlotValueType.PropertyConnectionState)
            {
                return outputTypeIsConnectionTestable;
            }

            if (s_ValidConversions.TryGetValue(outputType, out s_ValidSlotTypes))
            {
                return s_ValidSlotTypes.Contains(inputType);
            }
            throw new ArgumentOutOfRangeException("Unknown Concrete Slot Type: " + outputType);
        }
    }
}
