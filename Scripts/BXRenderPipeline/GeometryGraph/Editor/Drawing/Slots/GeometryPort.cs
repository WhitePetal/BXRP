using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    sealed class GeometryPort : Port
    {
        public static StyleSheet styleSheet;
        GeometryPort(Orientation portOrientation, Direction portDirection, Capacity portCapacity, Type type)
            : base(portOrientation, portDirection, portCapacity, type)
        {
            if (styleSheet == null)
                styleSheet = Resources.Load<StyleSheet>("Styles/GeometryPort");
            styleSheets.Add(styleSheet);
        }

        GeometrySlot m_Slot;

        public static GeometryPort Create(GeometrySlot slot, IEdgeConnectorListener connectorListener)
        {
            var port = new GeometryPort(Orientation.Horizontal, slot.isInputSlot ? Direction.Input : Direction.Output,
                slot.isInputSlot ? Capacity.Single : Capacity.Multi, null)
            {
                m_EdgeConnector = new EdgeConnector<UnityEditor.Experimental.GraphView.Edge>(connectorListener),
            };
            port.AddManipulator(port.m_EdgeConnector);
            port.slot = slot;
            port.portName = slot.displayName;
            port.visualClass = slot.concreteValueType.ToClassName();
            return port;
        }

        public void Dispose()
        {
            this.RemoveManipulator(m_EdgeConnector);
            m_EdgeConnector = null;
            m_Slot = null;
            styleSheets.Clear();
            DisconnectAll();
            OnDisconnect = null;
        }

        public GeometrySlot slot
        {
            get { return m_Slot; }
            set
            {
                if (ReferenceEquals(value, m_Slot))
                    return;
                if (value == null)
                    throw new NullReferenceException();
                if (m_Slot != null && value.isInputSlot != m_Slot.isInputSlot)
                    throw new ArgumentException("Cannot change direction of already created port");
                m_Slot = value;
                portName = slot.displayName;
                visualClass = slot.concreteValueType.ToClassName();
            }
        }

        public new Action<Port> OnDisconnect;

        public override void Disconnect(UnityEditor.Experimental.GraphView.Edge edge)
        {
            base.Disconnect(edge);
            OnDisconnect?.Invoke(this);
        }
    }

    static class GeometryPortExtensions
    {
        public static GeometrySlot GetSlot(this Port port)
        {
            var geometryPort = port as GeometryPort;
            return geometryPort != null ? geometryPort.slot : null;
        }
    }
}
