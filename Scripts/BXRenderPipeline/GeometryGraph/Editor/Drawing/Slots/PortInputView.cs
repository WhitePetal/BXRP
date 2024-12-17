using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace BXGeometryGraph
{
	class PortInputView : GraphElement, IDisposable
	{
		readonly CustomStyleProperty<Color> k_EdgeColorProperty = new CustomStyleProperty<Color>("--edge-color");

		private Color m_EdgeColor = Color.red;

		public Color edgeColor
		{
			get { return m_EdgeColor; }
		}

		public GeometrySlot slot
		{
			get { return m_Slot; }
		}

		private GeometrySlot m_Slot;
		private ConcreteSlotValueType m_SlotType;
		private VisualElement m_Control;
		private VisualElement m_Container;
		private EdgeControl m_EdgeControl;

		public PortInputView(GeometrySlot slot)
		{
			this.AddStyleSheetPath("Assets/Scripts/BXRenderPipeline/GeometryGraph/Editor/Resources/Styles/PortInputView.uss");
			pickingMode = PickingMode.Ignore;
			ClearClassList();
			m_Slot = slot;
			m_SlotType = slot.concreteValueType;
			AddToClassList("type" + m_SlotType);

			m_EdgeControl = new EdgeControl
			{
				@from = new Vector2(232f - 21f, 11.5f),
				to = new Vector2(232f, 11.5f),
				edgeWidth = 2,
				pickingMode = PickingMode.Ignore
			};
			Add(m_EdgeControl);

			m_Container = new VisualElement { name = "container" };
			{
				CreateControl();

				var slotElement = new VisualElement { name = "slot" };
				{
					slotElement.Add(new VisualElement { name = "dot" });
				}
				m_Container.Add(slotElement);
			}
			Add(m_Container);

			m_Container.Add(new VisualElement() { name = "disabledOverlay", pickingMode = PickingMode.Ignore });
			RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
		}

		void OnCustomStyleResolved(CustomStyleResolvedEvent e)
		{
			Color colorValue;

			if (e.customStyle.TryGetValue(k_EdgeColorProperty, out colorValue))
				m_EdgeColor = colorValue;

			m_EdgeControl.UpdateLayout();
			m_EdgeControl.inputColor = edgeColor;
			m_EdgeControl.outputColor = edgeColor;
		}

		public void UpdateSlot(GeometrySlot newSlot)
		{
			m_Slot = newSlot;
			Recreate();
		}

		public void UpdateSlotType()
		{
			if (slot.concreteValueType != m_SlotType)
				Recreate();
		}

		private void Recreate()
		{
			RemoveFromClassList("type" + m_SlotType);
			m_SlotType = slot.concreteValueType;
			AddToClassList("type" + m_SlotType);
			if (m_Control != null)
			{
				if (m_Control is IDisposable disposable)
					disposable.Dispose();
				m_Container.Remove(m_Control);
			}
			CreateControl();
		}

		void CreateControl()
		{
			// Specially designated properties (Use Custom Binding) are shown as a label on the slot when the slot is disconnected, with no ability to set an explicit default.
			// If the port for this property is connected to, it will use the regular slot control.
			m_Control = (!slot.isConnected && slot.IsConnectionTestable()) ? slot.InstantiateCustomControl() : slot.InstantiateControl();
			if (m_Control != null)
			{
				m_Container.Insert(0, m_Control);
			}
			else
			{
				// Some slot types don't support an input control, so hide this
				m_Container.visible = m_EdgeControl.visible = false;
			}
		}

		public void Dispose()
		{
			if (m_Control is IDisposable disposable)
				disposable.Dispose();

			styleSheets.Clear();
			m_Control = null;
			m_EdgeControl = null;
			UnregisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
		}
	}
}
