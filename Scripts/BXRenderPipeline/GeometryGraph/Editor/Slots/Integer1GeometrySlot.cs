using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    [System.Serializable]
    class Integer1GeometrySlot : GeometrySlot, IGeometrySlotHasValue<int>
    {
        [SerializeField]
        private int m_Value;

        [SerializeField]
        private int m_DefaultValue;

        [SerializeField]
        private string[] m_Labels; // this can be null, which means fallback to k_LabelDefaults

        private static readonly string[] k_LabelDefaults = { "X" };

        private string[] labels
        {
            get
            {
                if ((m_Labels == null) || (m_Labels.Length != k_LabelDefaults.Length))
                    return k_LabelDefaults;
                return m_Labels;
            }
        }

        public Integer1GeometrySlot()
        {

        }

        public Integer1GeometrySlot(
              int slotId,
              string displayName,
              string shaderOutputName,
              SlotType slotType,
              int value,
              GeometryStageCapability stageCapability = GeometryStageCapability.All,
              string label1 = null,
              bool hidden = false)
              : base(slotId, displayName, shaderOutputName, slotType, stageCapability, hidden)
        {
            m_DefaultValue = value;
            m_Value = value;
            if (label1 != null)
                m_Labels = new[] { label1 };
        }

        public int defaultValue { get { return m_DefaultValue; } }

        public int value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        public override bool isDefaultValue => value.Equals(defaultValue);

        public override VisualElement InstantiateControl()
        {
            return new MultiIntegerSlotControlView(owner, labels, () => new Vector4(value, 0f, 0f, 0f), (newValue) => value = (int)newValue.x);
        }

        protected override string ConcreteSlotValueAsVariable()
        {
            return string.Format("$precision({0})", NodeUtils.FloatToGeometryValue(value));
        }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            var matOwner = owner as AbstractGeometryNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractGeometryNode)));

            var property = new Vector1GeometryProperty()
            {
                overrideReferenceName = matOwner.GetVariableNameForSlot(id),
                generatePropertyBlock = false,
                value = value
            };
            properties.AddGeometryProperty(property);
        }

        public override SlotValueType valueType { get { return SlotValueType.Vector1; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.Vector1; } }

        public override void GetPreviewProperties(List<PreviewProperty> properties, string name)
        {
            var pp = new PreviewProperty(PropertyType.Float)
            {
                name = name,
                floatValue = value
            };
            properties.Add(pp);
        }

        public override void CopyValuesFrom(GeometrySlot foundSlot)
        {
            var slot = foundSlot as Vector1GeometrySlot;
            if (slot != null)
                value = (int)slot.value;
        }

        public override void CopyDefaultValue(GeometrySlot other)
        {
            base.CopyDefaultValue(other);
            if (other is IGeometrySlotHasValue<float> ms)
            {
                m_DefaultValue = (int)ms.defaultValue;
            }
        }

        public override int GetIntDefaultValue()
        {
            return (int)m_Value;
        }

        public override float GetFloatDefaultValue()
        {
            return m_Value;
        }
    }
}
