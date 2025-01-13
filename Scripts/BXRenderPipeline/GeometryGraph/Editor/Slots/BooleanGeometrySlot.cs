using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    [Serializable]
    internal class BooleanGeometrySlot : GeometrySlot, IGeometrySlotHasValue<bool>
    {
        [SerializeField]
        private bool m_Value;

        [SerializeField]
        private bool m_DefaultValue;

        public BooleanGeometrySlot()
        { }

        public BooleanGeometrySlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            bool value,
            GeometryStageCapability geometryStage = GeometryStageCapability.All,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, slotType, geometryStage, hidden)
        {
            m_DefaultValue = value;
            m_Value = value;
        }

        public override VisualElement InstantiateControl()
        {
            return new BooleanSlotControlView(this);
        }

        public bool defaultValue { get { return m_DefaultValue; } }

        public bool value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        protected override string ConcreteSlotValueAsVariable()
        {
            return (value ? 1 : 0).ToString();
        }

        public override bool GetBooleanDefaultValue()
        {
            return value;
        }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            var matOwner = owner as AbstractGeometryNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractGeometryNode)));

            var property = new BooleanGeometryProperty()
            {
                overrideReferenceName = matOwner.GetVariableNameForSlot(id),
                generatePropertyBlock = false,
                value = value
            };
            properties.AddGeometryProperty(property);
        }

        public override SlotValueType valueType { get { return SlotValueType.Boolean; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.Boolean; } }

        public override bool isDefaultValue => value.Equals(defaultValue);

        public override void GetPreviewProperties(List<PreviewProperty> properties, string name)
        {
            var pp = new PreviewProperty(PropertyType.Boolean)
            {
                name = name,
                booleanValue = value
            };
            properties.Add(pp);
        }

        public override void CopyValuesFrom(GeometrySlot foundSlot)
        {
            var slot = foundSlot as BooleanGeometrySlot;
            if (slot != null)
                value = slot.value;
        }
    }
}
