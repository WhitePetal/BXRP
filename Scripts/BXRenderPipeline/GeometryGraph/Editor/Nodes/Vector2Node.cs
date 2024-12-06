using System.Collections;
using System.Collections.Generic;
using BXGraphing;
using UnityEngine;

namespace BXGeometryGraph
{
    [Title("Input", "Basic", "Vector 2")]
    public class Vector2Node : AbstractGeometryNode, IGeneratesBodyCode, IPropertyFromNode
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
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/WhitePetal/FloowDream/tree/main/Scripts/BXRenderPipeline/GeometryGraph/Editor/Resource/Documents/Vector2Node"; }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1GeometrySlot(InputSlotXId, kInputSlotXName, kInputSlotXName, SlotType.Input, m_Value.x));
            AddSlot(new Vector1GeometrySlot(InputSlotYId, kInputSlotYName, kInputSlotYName, SlotType.Input, m_Value.y, label1: "Y"));
            AddSlot(new Vector2GeometrySlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId, InputSlotXId, InputSlotYId });
        }

        public void GenerateNodeCode(GeometryGenerator visitor, GenerationMode generationMode)
        {
            var inputXValue = GetSlotValue(InputSlotXId, generationMode);
            var inputYValue = GetSlotValue(InputSlotYId, generationMode);
            var outputName = GetVariableNameForSlot(OutputSlotId);

            var s = string.Format("{0}2 {1} = {0}2({2},{3});",
                precision,
                outputName,
                inputXValue,
                inputYValue);
            visitor.AddGeometryChunk(s, false);
        }

        public IGeometryProperty AsGeometryProperty()
        {
            var slotX = FindInputSlot<Vector1GeometrySlot>(InputSlotXId);
            var slotY = FindInputSlot<Vector1GeometrySlot>(InputSlotYId);
            return new Vector2GeometryProperty { value = new Vector2(slotX.value, slotY.value) };
        }

        public int outputSlotID { get { return OutputSlotId; } }
    }
}
