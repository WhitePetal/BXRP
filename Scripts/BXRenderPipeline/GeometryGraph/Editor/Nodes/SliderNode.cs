using System.Collections;
using System.Collections.Generic;
using BXGraphing;
using UnityEngine;

namespace BXGeometryGraph
{
    public class SliderNode : AbstractGeometryNode, IGeneratesBodyCode, IPropertyFromNode
    {
        [SerializeField]
        private Vector3 m_Value = new Vector3(0f, 0f, 1f);

        public const int OutputSlotId = 0;
        private const string kOutputSlotName = "Out";

        public SliderNode()
        {
            name = "Slider";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/WhitePetal/FloowDream/tree/main/Scripts/BXRenderPipeline/GeometryGraph/Editor/Resources/Documents/SliderNode"; }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1GeometrySlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, 0));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        [SliderControl("", true)]
        public Vector3 value
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
                value = value.x,
                rangeValues = new Vector2(value.y, value.z),
                floatType = FloatType.Slider
            });
        }

        public void GenerateNodeCode(GeometryGenerator visitor, GenerationMode generationMode)
        {
            if (generationMode.IsPreview())
                return;

            visitor.AddGeometryChunk(precision + " " + GetVariableNameForNode() + " = " + m_Value.x + ";", true);
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
                floatValue = m_Value.x
            });
        }

        public IGeometryProperty AsGeometryProperty()
        {
            return new Vector1GeometryProperty
            {
                value = value.x,
                rangeValues = new Vector2(value.y, value.z),
                floatType = FloatType.Slider
            };
        }

        public int outputSlotID { get { return OutputSlotId; } }
    }
}
