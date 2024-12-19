using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BXGeometryGraph
{
    readonly struct GraphDataReadOnly
    {
        private readonly GraphData m_Graph;

        public GraphDataReadOnly(GraphData graph)
        {
            m_Graph = graph;
        }

        private bool AnyConnectedControl<T>() where T : IControl
        {
            var matchingNodes = m_Graph.GetNodes<BlockNode>().Where(o => o.descriptor.control is T);
            return matchingNodes.SelectMany(o => o.GetInputSlots<GeometrySlot>()).Any(o => o.isConnected);
        }

        public bool AnyVertexAnimationActive()
        {
            //return AnyConnectedControl<PositionControl>();
            return false;
        }

        public bool IsVFXCompatible() => m_Graph.hasVFXCompatibleTarget;
    }
}
