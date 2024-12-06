using System;
using System.Collections;
using System.Collections.Generic;
using BXGraphing;
using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    public class Vector4GoemetrySlot : GeometrySlot, IGeometrySlotHasValue<Vector4>
    {
        [SerializeField]
        private Vector4 m_Value;

        [SerializeField]
        private Vector4 m_DefaultValue;

        private string[] m_Labels;

        public Vector4GoemetrySlot()
        {
            m_Labels = new[] { "X", "Y", "Z", "W" };
        }

        public Vector4GoemetrySlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            Vector4 value,
            string label1 = "X",
            string label2 = "Y",
            string label3 = "Z",
            string label4 = "W",
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, slotType, hidden)
        {
            m_Value = value;
            m_Labels = new[] { label1, label2, label3, label4 };
        }

        public Vector4 defaultValue { get { return m_DefaultValue; } }

        public Vector4 value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        public override VisualElement InstantiateControl()
        {
            return new MultiFloatSlotControlView(owner, m_Labels, () => value, (newValue) => value = newValue);
        }

        protected override string ConcreteSlotValueAsVariable(AbstractGeometryNode.OutputPrecision precision)
        {
            return precision + "4 (" + NodeUtils.FloatToGeometryValue(value.x) + "," + NodeUtils.FloatToGeometryValue(value.y) + "," + NodeUtils.FloatToGeometryValue(value.z) + "," + NodeUtils.FloatToGeometryValue(value.w) + ")";
        }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            var matOwner = owner as AbstractGeometryNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractGeometryNode)));

            var property = new Vector4GeometryProperty()
            {
                overrideReferenceName = matOwner.GetVariableNameForSlot(id),
                generatePropertyBlock = false,
                value = value
            };
            properties.AddGeometryProperty(property);
        }

        public override void GetPreviewProperties(List<PreviewProperty> properties, string name)
        {
            var pp = new PreviewProperty(PropertyType.Vector4)
            {
                name = name,
                vector4Value = new Vector4(value.x, value.y, value.z, value.w),
            };
            properties.Add(pp);
        }

        public override SlotValueType valueType { get { return SlotValueType.Vector4; } }

        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.Vector4; } }

        public override void CopyValueFrom(GeometrySlot foundSlot)
        {
            var slot = foundSlot as Vector4GoemetrySlot;
            if (slot != null)
                value = slot.value;
        }
    }
}
