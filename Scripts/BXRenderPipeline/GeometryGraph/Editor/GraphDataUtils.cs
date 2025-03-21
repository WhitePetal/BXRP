using System;
using System.Collections;
using System.Collections.Generic;
using BXCommon;
using UnityEngine;

namespace BXGeometryGraph
{
    sealed partial class GraphData : ISerializationCallbackReceiver
    {
        public static class GraphDataUtils
        {
            public static void ApplyActionLeafFirst(GraphData graph, Action<AbstractGeometryNode> action)
            {
                var temporaryMarks = PooledHashSet<string>.Get();
                var permanentMarks = PooledHashSet<string>.Get();
                var slots = ListPool<GeometrySlot>.Get();

                // Make sure we process a node's children before the node itself.
                var stack = StackPool<AbstractGeometryNode>.Get();
                foreach (var node in graph.GetNodes<AbstractGeometryNode>())
                {
                    stack.Push(node);
                }

                while(stack.Count > 0)
                {
                    var node = stack.Pop();
                    if (permanentMarks.Contains(node.objectId))
                    {
                        continue;
                    }

                    if (temporaryMarks.Contains(node.objectId))
                    {
                        action.Invoke(node);
                        permanentMarks.Add(node.objectId);
                    }
                    else
                    {
                        temporaryMarks.Add(node.objectId);
                        stack.Push(node);
                        node.GetInputSlots(slots);
                        foreach (var inputSlot in slots)
                        {
                            var nodeEdges = graph.GetEdges(inputSlot.slotReference);
                            foreach (var edge in nodeEdges)
                            {
                                var fromSocketRef = edge.outputSlot;
                                var childNode = fromSocketRef.node;
                                if (childNode != null)
                                {
                                    stack.Push(childNode);
                                }
                            }
                        }
                        slots.Clear();
                    }
                }

                StackPool<AbstractGeometryNode>.Release(stack);
                ListPool<GeometrySlot>.Release(slots);
                temporaryMarks.Dispose();
                permanentMarks.Dispose();
            }
        }
    }
}
