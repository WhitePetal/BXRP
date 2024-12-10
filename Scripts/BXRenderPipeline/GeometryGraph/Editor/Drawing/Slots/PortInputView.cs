using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace BXGeometryGraph
{
	public class PortInputView : GraphElement, IDisposable
	{
		private const string k_EdgeColorProperty = "edge-color";

		private Color m_EdgeColor;

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
				@from = new Vector2(212f - 21f, 11.5f),
				to = new Vector2(212f, 11.5f),
				edgeWidth = 2,
				pickingMode = PickingMode.Ignore
			};
			Add(m_EdgeControl);

			m_Container = new VisualElement { name = "container" };
			{
				m_Control = this.slot.InstantiateControl();
				if (m_Control != null)
					m_Container.Add(m_Control);

				var slotElement = new VisualElement { name = "slot" };
				{
					slotElement.Add(new VisualElement { name = "dot" });
				}
				m_Container.Add(slotElement);
			}
			Add(m_Container);

			m_Container.visible = m_EdgeControl.visible = m_Control != null;
		}

		protected override void OnCustomStyleResolved(ICustomStyle style)
		{
			base.OnCustomStyleResolved(style);
			CustomStyleProperty<Color> edgeColorProperty = new CustomStyleProperty<Color>(k_EdgeColorProperty);
			style.TryGetValue(edgeColorProperty, out m_EdgeColor);
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
			if(m_Control != null)
			{
				var disposable = m_Control as IDisposable;
				if (disposable != null)
					disposable.Dispose();
				m_Container.Remove(m_Control);
			}
			m_Control = slot.InstantiateControl();
			if (m_Control != null)
				m_Container.Insert(0, m_Control);

			m_Container.visible = m_EdgeControl.visible = m_Control != null;
		}

		public void Dispose()
		{
			var disposable = m_Control as IDisposable;
			if (disposable != null)
				disposable.Dispose();
		}
	}
}
