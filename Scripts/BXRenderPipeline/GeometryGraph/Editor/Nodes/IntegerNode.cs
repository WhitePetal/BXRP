using System.Collections;
using System.Collections.Generic;
using BXGraphing;
using UnityEngine;

namespace BXGeometryGraph
{
    [Title("Input", "Basic", "Integer")]
    public class IntegerNode : AbstractGeometryNode, IGeneratesBodyCode, IPropertyFromNode
    {
        [SerializeField]
        private int m_Value;

        public const int OutputSlotId = 0;
        private const string kOutputSlotName = "Out";

        public IntegerNode()
        {
            name = "Integer";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/WhitePetal/FloowDream/tree/main/Scripts/BXRenderPipeline/GeometryGraph/Editor/Resources/Documents/IntegerNode"; }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1GeometrySlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, 0));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        [IntegerControl("")]
        public int value
        {
            get { return m_Value; }
            set
            {
                if (m_Value == value)
                    return;
                m_Value = value;
                Dirty(ModificationScope.Node);
            }
        }

        public override void CollectGeometryProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            properties.AddGeometryProperty(new Vector1GeometryProperty()
            {
                overrideReferenceName = GetVariableNameForNode(),
                generatePropertyBlock = false,
                value = value,
                floatType = FloatType.Integer
            });
        }

        public void GenerateNodeCode(GeometryGenerator visitor, GenerationMode generationMode)
        {
            if (generationMode.IsPreview())
                return;

            visitor.AddGeometryChunk(precision + " " + GetVariableNameForNode() + " = " + m_Value + ";", true);
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return GetVariableNameForNode();
        }

        public override void CollectPreviewGeometryProperties(List<PreviewProperty> properties)
        {
            properties.Add(new PreviewProperty(PropertyType.Vector1)
            {
                name = GetVariableNameForNode(),
                floatValue = m_Value
            });
        }

        public IGeometryProperty AsGeometryProperty()
        {
            return new Vector1GeometryProperty { value = value, floatType = FloatType.Integer };
        }

        public int outputSlotID { get { return OutputSlotId; } }
    }
}
