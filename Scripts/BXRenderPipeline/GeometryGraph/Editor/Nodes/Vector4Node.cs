using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace BXGeometryGraph
{
    [Title("Input", "Basic", "Vector 4")]
    class Vector4Node : AbstractGeometryNode, IGeneratesShaderBodyCode, IPropertyFromNode
    {
        [SerializeField]
        private Vector4 m_Value = Vector4.one;

        private const string kInputSlotXName = "X";
        private const string kInputSlotYName = "Y";
        private const string kInputSlotZName = "Z";
        private const string kInputSlotWName = "W";
        private const string kOutputSlotName = "Out";

        public const int OutputSlotId = 0;
        public const int InputSlotXId = 1;
        public const int InputSlotYId = 2;
        public const int InputSlotZId = 3;
        public const int InputSlotWId = 4;

        public Vector4Node()
        {
            name = "Vector 4";
            synonyms = new string[] { "4", "v4", "vec4", "float4" };
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/WhitePetal/FloowDream/tree/main/Scripts/BXRenderPipeline/GeometryGraph/Editor/Resources/Documents/Vector4Node"; }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1GeometrySlot(InputSlotXId, kInputSlotXName, kInputSlotXName, SlotType.Input, m_Value.x));
            AddSlot(new Vector1GeometrySlot(InputSlotYId, kInputSlotYName, kInputSlotYName, SlotType.Input, m_Value.y, label1: "Y"));
            AddSlot(new Vector1GeometrySlot(InputSlotZId, kInputSlotZName, kInputSlotZName, SlotType.Input, m_Value.z, label1: "Z"));
            AddSlot(new Vector1GeometrySlot(InputSlotWId, kInputSlotWName, kInputSlotWName, SlotType.Input, m_Value.w, label1: "W"));
            AddSlot(new Vector4GeometrySlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId, InputSlotXId, InputSlotYId, InputSlotZId, InputSlotWId });
        }

        public void GenerateNodeShaderCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var inputXValue = GetSlotValue(InputSlotXId, generationMode);
            var inputYValue = GetSlotValue(InputSlotYId, generationMode);
            var inputZValue = GetSlotValue(InputSlotZId, generationMode);
            var inputWValue = GetSlotValue(InputSlotWId, generationMode);
            var outputName = GetVariableNameForSlot(outputSlotID);

            var s = string.Format("$precision4 {0} = $precision4({1}, {2}, {3}, {4});",
                outputName,
                inputXValue,
                inputYValue,
                inputZValue,
                inputWValue);
            sb.AppendLine(s);
        }

        public AbstractGeometryProperty AsGeometryProperty()
        {
            var slotX = FindInputSlot<Vector1GeometrySlot>(InputSlotXId);
            var slotY = FindInputSlot<Vector1GeometrySlot>(InputSlotYId);
            var slotZ = FindInputSlot<Vector1GeometrySlot>(InputSlotZId);
            var slotW = FindInputSlot<Vector1GeometrySlot>(InputSlotWId);
            return new Vector4GeometryProperty { value = new Vector4(slotX.value, slotY.value, slotZ.value, slotW.value) };
        }

        public int outputSlotID { get { return OutputSlotId; } }
    }

}