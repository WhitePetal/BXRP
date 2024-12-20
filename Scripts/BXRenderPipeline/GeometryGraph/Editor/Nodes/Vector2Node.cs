using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace BXGeometryGraph
{
    [Title("Input", "Basic", "Vector 2")]
    class Vector2Node : AbstractGeometryNode, IGeneratesShaderBodyCode, IPropertyFromNode
    {
        [SerializeField]
        private Vector2 m_Value;

        const string kInputSlotXName = "X";
        const string kInputSlotYName = "Y";
        const string kOutputSlotName = "Out";

        public const int OutputSlotId = 0;
        public const int InputSlotXId = 1;
        public const int InputSlotYId = 2;

        public Vector2Node()
        {
            name = "Vector 2";
            synonyms = new string[] { "2", "v2", "vec2", "float2" };
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/WhitePetal/FloowDream/tree/main/Scripts/BXRenderPipeline/GeometryGraph/Editor/Resources/Documents/Vector2Node"; }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1GeometrySlot(InputSlotXId, kInputSlotXName, kInputSlotXName, SlotType.Input, m_Value.x));
            AddSlot(new Vector1GeometrySlot(InputSlotYId, kInputSlotYName, kInputSlotYName, SlotType.Input, m_Value.y, label1: "Y"));
            AddSlot(new Vector2GeometrySlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId, InputSlotXId, InputSlotYId });
        }

        public void GenerateNodeShaderCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var inputXValue = GetSlotValue(InputSlotXId, generationMode);
            var inputYValue = GetSlotValue(InputSlotYId, generationMode);
            var outputName = GetVariableNameForSlot(OutputSlotId);

            var s = string.Format("$precision2 {0} = $precision2({1}, {2});",
                outputName,
                inputXValue,
                inputYValue);
            sb.AppendLine(s);
        }

        public AbstractGeometryProperty AsGeometryProperty()
        {
            var slotX = FindInputSlot<Vector1GeometrySlot>(InputSlotXId);
            var slotY = FindInputSlot<Vector1GeometrySlot>(InputSlotYId);
            return new Vector2GeometryProperty { value = new Vector2(slotX.value, slotY.value) };
        }

        int IPropertyFromNode.outputSlotID { get { return OutputSlotId; } }
    }
}
