using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    [Title("Input", "Basic", "Boolean")]
    internal class BooleanNode : AbstractGeometryNode, IGeneratesShaderBodyCode, IPropertyFromNode
    {
        [SerializeField]
        internal bool m_Value;

        public const int OutputSlotId = 0;
        private const string kOutputSlotName = "Out";

        public BooleanNode()
        {
            name = "Boolean";
            synonyms = new string[] { "switch", "true", "false", "on", "off" };
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new BooleanGeometrySlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, false));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        [ToggleControl("")]
        public ToggleData value
        {
            get { return new ToggleData(m_Value); }
            set
            {
                if (m_Value == value.isOn)
                    return;
                m_Value = value.isOn;
                Dirty(ModificationScope.Node);
            }
        }

        public override void CollectGeometryProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            properties.AddGeometryProperty(new BooleanGeometryProperty()
            {
                overrideReferenceName = GetVariableNameForNode(),
                generatePropertyBlock = false,
                value = m_Value
            });
        }

        public void GenerateNodeShaderCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (generationMode.IsPreview())
                return;

            sb.AppendLine("$precision {0} = {1};", GetVariableNameForNode(), (m_Value ? 1 : 0));
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return GetVariableNameForNode();
        }

        public override void CollectPreviewGeometryProperties(List<PreviewProperty> properties)
        {
            properties.Add(new PreviewProperty(PropertyType.Boolean)
            {
                name = GetVariableNameForNode(),
                booleanValue = m_Value
            });
        }

        public AbstractGeometryProperty AsGeometryProperty()
        {
            return new BooleanGeometryProperty { value = m_Value };
        }

        public int outputSlotID { get { return OutputSlotId; } }
    }
}
