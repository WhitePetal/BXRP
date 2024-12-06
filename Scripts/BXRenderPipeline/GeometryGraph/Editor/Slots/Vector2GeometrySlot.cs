using System;
using System.Collections;
using System.Collections.Generic;
using BXGraphing;
using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    public class Vector2GeometrySlot : GeometrySlot, IGeometrySlotHasValue<Vector2>
    {
        [SerializeField]
        private Vector2 m_Value;

        [SerializeField]
        private Vector2 m_DefaultValue;

        private string[] m_Labels;

        public Vector2GeometrySlot()
        {

        }

        public Vector2GeometrySlot(
            int slotId,
            string displayName,
            string geometryOutputName,
            SlotType slotType,
            Vector2 value,
            string label1 = "X",
            string label2 = "Y",
            bool hidden = false) : base(slotId, displayName, geometryOutputName, slotType, hidden)
        {
            m_Value = value;
            m_Labels = new[] { label1, label2 };
        }

        public Vector2 defaultValue { get { return m_DefaultValue; } }

        public Vector2 value
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
            return precision + "2 (" + NodeUtils.FloatToGeometryValue(value.x) + "," + NodeUtils.FloatToGeometryValue(value.y) + ")";
        }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            var matOwner = owner as AbstractGeometryNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractGeometryNode)));

            var property = new Vector2GeometryProperty
            {
                overrideReferenceName = matOwner.GetVariableNameForSlot(id),
                generatePropertyBlock = false,
                value = value
            };
            properties.AddGeometryProperty(property);
        }

        public override void GetPreviewProperties(List<PreviewProperty> properties, string name)
        {
            var pp = new PreviewProperty(PropertyType.Vector2)
            {
                name = name,
                vector4Value = new Vector4(value.x, value.y, 0, 0),
            };
            properties.Add(pp);
        }

        public override SlotValueType valueType { get { return SlotValueType.Vector2; } }

        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.Vector2; } }

        public override void CopyValueFrom(GeometrySlot foundSlot)
        {
            var slot = foundSlot as Vector2GeometrySlot;
            if (slot != null)
                value = slot.value;
        }
    }
}