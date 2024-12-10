using System.Collections;
using System.Collections.Generic;
using BXGraphing;
using UnityEngine;

namespace BXGeometryGraph
{
    [Title("Input", "Basic", "Vector 3")]
    public class Vector3Node : AbstractGeometryNode, IGeneratesBodyCode, IPropertyFromNode
    {
        [SerializeField]
        private Vector3 m_Value;

        const string kInputSlotXName = "X";
        const string kInputSlotYName = "Y";
        const string kInputSlotZName = "Z";
        const string kOutputSlotName = "Out";

        public const int OutputSlotId = 0;
        public const int InputSlotXId = 1;
        public const int InputSlotYId = 2;
        public const int InputSlotZId = 3;

        public Vector3Node()
        {
            name = "Vector 3";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/WhitePetal/FloowDream/tree/main/Scripts/BXRenderPipeline/GeometryGraph/Editor/Resources/Documents/Vector3Node"; }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1GeometrySlot(InputSlotXId, kInputSlotXName, kInputSlotXName, SlotType.Input, m_Value.x));
            AddSlot(new Vector1GeometrySlot(InputSlotYId, kInputSlotYName, kInputSlotYName, SlotType.Input, m_Value.y, label1: "Y"));
            AddSlot(new Vector1GeometrySlot(InputSlotZId, kInputSlotZName, kInputSlotZName, SlotType.Input, m_Value.z, label1: "Z"));
            AddSlot(new Vector3GeometrySlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId, InputSlotXId, InputSlotYId, InputSlotZId });
        }

        public void GenerateNodeCode(GeometryGenerator visitor, GenerationMode generationMode)
        {
            var inputXValue = GetSlotValue(InputSlotXId, generationMode);
            var inputYValue = GetSlotValue(InputSlotYId, generationMode);
            var inputZValue = GetSlotValue(InputSlotZId, generationMode);
            var outputName = GetVariableNameForSlot(outputSlotID);

            var s = string.Format("{0}3 {1} = {0}3({2},{3},{4});",
                precision,
                outputName,
                inputXValue,
                inputYValue,
                inputZValue);
            visitor.AddGeometryChunk(s, false);
        }

        public IGeometryProperty AsGeometryProperty()
        {
            var slotX = FindInputSlot<Vector1GeometrySlot>(InputSlotXId);
            var slotY = FindInputSlot<Vector1GeometrySlot>(InputSlotYId);
            var slotZ = FindInputSlot<Vector1GeometrySlot>(InputSlotZId);
            return new Vector3GeometryProperty { value = new Vector3(slotX.value, slotY.value, slotZ.value) };
        }

        public int outputSlotID { get { return OutputSlotId; } }
    }
}
