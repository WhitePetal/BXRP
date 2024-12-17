using System;
using System.Collections;
using System.Collections.Generic;
using BXGraphing;
using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    [System.Serializable]
    class Vector1GeometrySlot : GeometrySlot, IGeometrySlotHasValue<float>
    {
        [SerializeField]
        private float m_Value;

        [SerializeField]
        private float m_DefaultValue;

        private string[] m_Labels;

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

        public Vector1GeometrySlot()
        {

        }

        public Vector1GeometrySlot(
            int slotId,
            string displayName,
            string geometryOutputName,
            SlotType slotType,
            float value,
            GeometryStageCapability stageCapability = GeometryStageCapability.All,
            string label1 = null,
            bool hidden = false) : base(slotId, displayName, geometryOutputName, slotType, stageCapability, hidden)
        {
            m_DefaultValue = value;
            m_Value = value;
            if (label1 != null)
                m_Labels = new[] { label1 };
        }

        public float defaultValue
        {
            get { return m_DefaultValue; }
        }

        public float value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        public override bool isDefaultValue => value.Equals(defaultValue);

        public override VisualElement InstantiateControl()
        {
            return new MultiFloatSlotControlView(owner, m_Labels, () => new Vector4(value, 0f, 0f, 0f), (newValue) => value = newValue.x);
        }

        protected override string ConcreteSlotValueAsVariable(AbstractGeometryNode.OutputPrecision precision)
        {
            return string.Format("$precision({0})", NodeUtils.FloatToGeometryValue(value));
        }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            var geoOwner = owner as AbstractGeometryNode;
            if (owner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractGeometryNode)));

            var property = new Vector1GeometryProperty()
            {
                overrideReferenceName = geoOwner.GetVariableNameForSlot(id),
                generatePropertyBlock = false,
                value = value
            };
            properties.AddGeometryProperty(property);
        }

        public override SlotValueType valueType {get { return SlotValueType.Vector1; } }
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
                value = slot.value;
        }

        public override void CopyDefaultValue(GeometrySlot other)
        {
            base.CopyDefaultValue(other);
            if(other is IGeometrySlotHasValue<float> ms)
            {
                m_DefaultValue = ms.defaultValue;
            }
        }
    }
}
