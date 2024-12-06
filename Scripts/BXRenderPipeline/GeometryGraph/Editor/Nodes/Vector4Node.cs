using System.Collections;
using System.Collections.Generic;
using BXGraphing;
using UnityEngine;

namespace BXGeometryGraph
{
    [Title("Input", "Basic", "Vector 4")]
    public class Vector4Node : AbstractGeometryNode, IGeneratesBodyCode, IPropertyFromNode
    {
        [SerializeField]
        private Vector4 m_Value;

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
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/WhitePetal/FloowDream/tree/main/Scripts/BXRenderPipeline/GeometryGraph/Editor/Resource/Documents/Vector4Node"; }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1GeometrySlot(InputSlotXId, kInputSlotXName, kInputSlotXName, SlotType.Input, m_Value.x));
            AddSlot(new Vector1GeometrySlot(InputSlotYId, kInputSlotYName, kInputSlotYName, SlotType.Input, m_Value.y, label1: "Y"));
            AddSlot(new Vector1GeometrySlot(InputSlotZId, kInputSlotZName, kInputSlotZName, SlotType.Input, m_Value.z, label1: "Z"));
            AddSlot(new Vector1GeometrySlot(InputSlotWId, kInputSlotWName, kInputSlotWName, SlotType.Input, m_Value.w, label1: "W"));
            //AddSlot(new Vector4GeometryProperty(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            // TODO
            RemoveSlotsNameNotMatching(new[] { OutputSlotId, InputSlotXId, InputSlotYId, InputSlotZId, InputSlotWId });
        }

        public int outputSlotID => throw new System.NotImplementedException();

        public IGeometryProperty AsGeometryProperty()
        {
            throw new System.NotImplementedException();
        }

        public void GenerateNodeCode(GeometryGenerator visitor, GenerationMode generationMode)
        {
            throw new System.NotImplementedException();
        }
    }

}