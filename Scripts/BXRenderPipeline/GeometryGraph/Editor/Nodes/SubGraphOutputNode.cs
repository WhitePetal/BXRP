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
	class SubGraphOutputNode : AbstractGeometryNode
	{
		static string s_MissingOutputSlot = "A Sub Graph must have at least one output slot";
		static List<ConcreteSlotValueType> s_ValidSlotTypes = new List<ConcreteSlotValueType>()
		{
			ConcreteSlotValueType.Vector1,
			ConcreteSlotValueType.Vector2,
			ConcreteSlotValueType.Vector3,
			ConcreteSlotValueType.Vector4,
			ConcreteSlotValueType.Matrix2,
			ConcreteSlotValueType.Matrix3,
			ConcreteSlotValueType.Matrix4,
			ConcreteSlotValueType.Boolean
		};
		public bool IsFirstSlotValid = true;

		public SubGraphOutputNode()
		{
			name = "Output";
		}

		// Link to the Sub Graph overview page instead of the specific Node page, seems more useful
		public override string documentationURL => "https://github.com/WhitePetal/FloowDream/tree/main/Scripts/BXRenderPipeline/GeometryGraph/Editor/Resources/Documents/Sub-Graph";

		void ValidateGeometryStage()
		{
			List<GeometrySlot> slots = new List<GeometrySlot>();
			GetInputSlots(slots);

			// Reset all input slots back to All, otherwise they'll be incorrectly configured when traversing below
			foreach (GeometrySlot slot in slots)
				slot.stageCapability = GeometryStageCapability.All;

			foreach (var slot in slots)
			{
				slot.stageCapability = NodeUtils.GetEffectiveShaderStageCapability(slot, true);
			}
		}

		void ValidateSlotName()
		{
			List<GeometrySlot> slots = new List<GeometrySlot>();
			GetInputSlots(slots);

			foreach (var slot in slots)
			{
				var error = NodeUtils.ValidateSlotName(slot.RawDisplayName(), out string errorMessage);
				if (error)
				{
					owner.AddValidationError(objectId, errorMessage);
					break;
				}
			}
		}

		void ValidateSlotType()
		{
			List<GeometrySlot> slots = new List<GeometrySlot>();
			GetInputSlots(slots);

			if (!slots.Any())
			{
				owner.AddValidationError(objectId, s_MissingOutputSlot, GeometryCompilerMessageSeverity.Error);
			}
			else if (!s_ValidSlotTypes.Contains(slots.FirstOrDefault().concreteValueType))
			{
				IsFirstSlotValid = false;
				owner.AddValidationError(objectId, "Preview can only compile if the first output slot is a Vector, Matrix, or Boolean type. Please adjust slot types.", GeometryCompilerMessageSeverity.Error);
			}
		}

		public override void ValidateNode()
		{
			base.ValidateNode();
			IsFirstSlotValid = true;
			ValidateSlotType();
			if (IsFirstSlotValid)
				ValidateGeometryStage();
		}

		protected override void OnSlotsChanged()
		{
			base.OnSlotsChanged();
			ValidateNode();
		}

		public int AddSlot(ConcreteSlotValueType concreteValueType)
		{
			var index = this.GetInputSlots<GeometrySlot>().Count() + 1;
			var name = NodeUtils.GetDuplicateSafeNameForSlot(this, index, "Out_" + concreteValueType.ToString());
			AddSlot(GeometrySlot.CreateGeometrySlot(concreteValueType.ToSlotValueType(), index, name,
				NodeUtils.GetHLSLSafeName(name), SlotType.Input, Vector4.zero));
			return index;
		}

		public override bool canDeleteNode => false;

		public override bool canCopyNode => false;
	}
}