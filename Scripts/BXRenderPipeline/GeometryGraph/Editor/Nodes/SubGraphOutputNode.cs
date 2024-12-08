using BXGraphing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
	public class SubGraphOutputControlAttribute : Attribute, IControlAttribute
	{
		public VisualElement InstantiateControl(AbstractGeometryNode node, PropertyInfo propertyInfo)
		{
			if(!(node is SubGraphOutputNode))
				throw new ArgumentException("Node must inherit from AbstractSubGraphIONode.", "node");
			return new SubGraphOutputControlView((SubGraphOutputNode)node);
		}
	}

	public class SubGraphOutputControlView : VisualElement
	{
		private SubGraphOutputNode m_Node;

		public SubGraphOutputControlView(SubGraphOutputNode node)
		{
			m_Node = node;
			Add(new Button(OnAdd) { text = "Add Slot" });
			Add(new Button(OnRemove) { text = "Remove Slot" });
		}

		private void OnAdd()
		{
			m_Node.AddSlot();
		}

		private void OnRemove()
		{
			// tell the user that they might cchange things up.
			if(EditorUtility.DisplayDialog("Sub Graph Will Change", "If you remove a slot and save the sub graph, you might change other graphs that are using this sub graph.\n\nDo you want to continue?", "Yes", "No"))
			{
				m_Node.owner.owner.RegisterCompleteObjectUndo("Removing SLot");
				m_Node.RemoveSlot();
			}
		}
	}

	public class SubGraphOutputNode : AbstractGeometryNode
	{
		[SubGraphOutputControl]
		private int controlDummy { get; set; }

		public SubGraphOutputNode()
		{
			name = "SubGraphOutputs";
		}

		public override bool hasPreview
		{
			get { return true; }
		}

		public override PreviewMode previewMode
		{
			get { return PreviewMode.Preview3D; }
		}

		public virtual int AddSlot()
		{
			var index = this.GetInputSlots<ISlot>().Count() + 1;
			AddSlot(new Vector4GoemetrySlot(index, "Output" + index, "Output" + index, SlotType.Input, Vector4.zero));
			return index;
		}

		public virtual void RemoveSlot()
		{
			var index = this.GetInputSlots<ISlot>().Count();
			if (index == 0)
				return;

			RemoveSlot(index);
		}

		public void RemapOutputs(GeometryGenerator visitor, GenerationMode generationMode)
		{
			foreach (var slot in graphOutputs)
				visitor.AddGeometryChunk(string.Format("{0} = {1};", slot.geometryOutputName, GetSlotValue(slot.id, generationMode)), true);
		}

		public IEnumerable<GeometrySlot> graphOutputs
		{
			get
			{
				return NodeExtensions.GetInputSlots<GeometrySlot>(this).OrderBy(x => x.id);
			}
		}
	}

}