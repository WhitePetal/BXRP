using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
	public class GeometryPort : Port
	{
		GeometryPort(Orientation portOrientation, Direction portDirection, Capacity portCapacity, Type type)
			: base(portOrientation, portDirection, portCapacity, type)
		{
			this.AddStyleSheetPath("Assets/Scripts/BXRenderPipeline/GeometryGraph/Editor/Resources/Styles/GeometryPort.uss");
		}

		private GeometrySlot m_Slot;

		public GeometrySlot slot
		{
			get { return m_Slot; }
			set
			{
				if (ReferenceEquals(value, m_Slot))
					return;
				if(value == null)
					throw new NullReferenceException();
				if(m_Slot != null && value.isInputSlot != m_Slot.isInputSlot)
					throw new ArgumentException("Cannot change direction of already created port");
				m_Slot = value;
				portName = slot.displayName;
				visualClass = slot.concreteValueType.ToClassName();
			}
		}

		public static Port Create(GeometrySlot slot, IEdgeConnectorListener connectorListener)
		{
			var port = new GeometryPort(Orientation.Horizontal, slot.isInputSlot ? Direction.Input : Direction.Output,
				slot.isInputSlot ? Capacity.Single : Capacity.Multi, null)
			{
				m_EdgeConnector = new EdgeConnector<UnityEditor.Experimental.GraphView.Edge>(connectorListener)
			};
			port.AddManipulator(port.m_EdgeConnector);
			port.slot = slot;
			port.portName = slot.displayName;
			port.visualClass = slot.concreteValueType.ToClassName();
			return port;
		}
	}

	static class GeometryPortExtensions
	{
		public static GeometrySlot GetSlot(this Port port)
		{
			var goemetryPort = port as GeometryPort;
			return goemetryPort != null ? goemetryPort.slot : null;
		}
	}
}