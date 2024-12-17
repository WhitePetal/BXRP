using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    public enum ModificationScope
    {
        Nothing = 0,
        Node = 1,
        Graph = 2,
        Topological = 3,
        Layout = 4
    }

    internal delegate void OnNodeModified(AbstractGeometryNode node, ModificationScope scope);

    static class NodeExtensions
    {
        public static IEnumerable<T> GetSlots<T>(this AbstractGeometryNode node) where T : GeometrySlot
        {
            var slots = new List<T>();
            node.GetSlots(slots);
            return slots;
        }

        public static IEnumerable<T> GetInputSlots<T>(this AbstractGeometryNode node) where T : GeometrySlot
        {
            var slots = new List<T>();
            node.GetInputSlots(slots);
            return slots;
        }

        public static IEnumerable<T> GetInputSlots<T>(this AbstractGeometryNode node, GeometrySlot startingSlots) where T : GeometrySlot
        {
            var slots = new List<T>();
            node.GetInputSlots(startingSlots, slots);
            return slots;
        }

        public static IEnumerable<T> GetOutputSlots<T>(this AbstractGeometryNode node) where T : GeometrySlot
        {
            var slots = new List<T>();
            node.GetOutputSlots(slots);
            return slots;
        }

        public static IEnumerable<T> GetOutputSlots<T>(this AbstractGeometryNode node, GeometrySlot startingSlots) where T : GeometrySlot
        {
            var slots = new List<T>();
            node.GetOutputSlots(startingSlots, slots);
            return slots;
        }
    }
}
