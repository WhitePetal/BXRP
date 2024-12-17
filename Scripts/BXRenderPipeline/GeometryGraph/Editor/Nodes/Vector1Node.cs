using System.Collections;
using System.Collections.Generic;
using BXGraphing;
using UnityEngine;

namespace BXGeometryGraph
{
    [Title("Input", "Basic", "Float")]
    class Vector1Node : AbstractGeometryNode, IGeneratesShaderBodyCode, IPropertyFromNode
    {
        [SerializeField]
        private float m_Value = 0;

        private const string kInputSlotXName = "X";
        private const string kOutputSlotName = "Out";

        public const int InputSlotXId = 1;
        public const int OutputSlotId = 0;

        public Vector1Node()
        {
            name = "Float";
            synonyms = new string[] { "Vector 1", "1", "v1", "vec1", "scalar" };
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1GeometrySlot(InputSlotXId, kInputSlotXName, kInputSlotXName, SlotType.Input, m_Value));
            AddSlot(new Vector1GeometrySlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, 0));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId, InputSlotXId });
        }

        public void GenerateNodeShaderCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var inputValue = GetSlotValue(InputSlotXId, generationMode);
            sb.AppendLine(string.Format("$precision {0} = {1};", GetVariableNameForSlot(OutputSlotId), inputValue));
        }

        public AbstractGeometryProperty AsGeometryProperty()
        {
            var slot = FindInputSlot<Vector1GeometrySlot>(InputSlotXId);
            return new Vector1GeometryProperty() { value = slot.value };
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            name = "Float";
        }

        int IPropertyFromNode.outputSlotID { get { return OutputSlotId; } }
    }
}
