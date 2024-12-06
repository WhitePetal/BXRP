using System;
using System.Collections;
using System.Collections.Generic;
using BXGraphing;
using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    [System.Serializable]
    public class Vector3GeometrySlot : GeometrySlot, IGeometrySlotHasValue<Vector3>
    {
        [SerializeField]
        private Vector3 m_Value;

        [SerializeField]
        private Vector3 m_DefaultValue;

        string[] m_Labels;

        public Vector3GeometrySlot()
        {
        }

        public Vector3GeometrySlot(
            int slotId,
            string displayName,
            string geometryOutputName,
            SlotType slotType,
            Vector3 value,
            string label1 = "X",
            string label2 = "Y",
            string label3 = "Z",
            bool hidden = false) : base(slotId, displayName, geometryOutputName, slotType, hidden)
        {
            m_Value = value;
            m_Labels = new[] { label1, label2, label3 };
        }

        public Vector3 defaultValue { get { return m_DefaultValue; } }

        public Vector3 value
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
            return precision + "3 (" + NodeUtils.FloatToGeometryValue(value.x) + "," + NodeUtils.FloatToGeometryValue(value.y) + "," + NodeUtils.FloatToGeometryValue(value.z) + ")";
        }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            var matOwner = owner as AbstractGeometryNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractGeometryNode)));

            var property = new Vector3GeometryProperty()
            {
                overrideReferenceName = matOwner.GetVariableNameForSlot(id),
                generatePropertyBlock = false,
                value = value
            };
            properties.AddGeometryProperty(property);
        }

        public override void GetPreviewProperties(List<PreviewProperty> properties, string name)
        {
            var pp = new PreviewProperty(PropertyType.Vector3)
            {
                name = name,
                vector4Value = new Vector4(value.x, value.y, value.z, 0)
            };
            properties.Add(pp);
        }

        public override SlotValueType valueType { get { return SlotValueType.Vector3; } }

        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.Vector3; } }

        public override void CopyValueFrom(GeometrySlot foundSlot)
        {
            var slot = foundSlot as Vector3GeometrySlot;
            if (slot != null)
                value = slot.value;
        }
    }
}
