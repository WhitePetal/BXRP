using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    [Serializable]
    class Vector2GeometrySlot : GeometrySlot, IGeometrySlotHasValue<Vector2>
    {
        [SerializeField]
        private Vector2 m_Value;

        [SerializeField]
        private Vector2 m_DefaultValue;

        [SerializeField]
        private string[] m_Labels;

        bool m_Integer = false;

        static readonly string[] k_LabelDefaults = { "X", "Y" };
        string[] labels
        {
            get
            {
                if ((m_Labels == null) || (m_Labels.Length != k_LabelDefaults.Length))
                    return k_LabelDefaults;
                return m_Labels;
            }
        }

        public Vector2GeometrySlot()
        {

        }

        public Vector2GeometrySlot(
                    int slotId,
                    string displayName,
                    string shaderOutputName,
                    SlotType slotType,
                    Vector2 value,
                    GeometryStageCapability stageCapability = GeometryStageCapability.All,
                    string label1 = null,
                    string label2 = null,
                    bool hidden = false,
                    bool integer = false)
                    : base(slotId, displayName, shaderOutputName, slotType, stageCapability, hidden)
        {
            m_DefaultValue = value;
            m_Value = value;
            m_Integer = integer;
            if ((label1 != null) || (label2 != null))
            {
                m_Labels = new[]
                {
                    label1 ?? k_LabelDefaults[0],
                    label2 ?? k_LabelDefaults[1]
                };
            }
        }

        public Vector2 defaultValue { get { return m_DefaultValue; } }

        public Vector2 value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        public override bool isDefaultValue => value.Equals(defaultValue);

        public override VisualElement InstantiateControl()
        {
            if (m_Integer)
            {
                return new MultiIntegerSlotControlView(owner, labels, () => value, (newValue) => value = newValue);
            }
            else
            {
                return new MultiFloatSlotControlView(owner, labels, () => value, (newValue) => value = newValue);
            }
        }

        protected override string ConcreteSlotValueAsVariable()
        {
            return string.Format("$precision2 ({0}, {1})"
                , NodeUtils.FloatToGeometryValue(value.x)
                , NodeUtils.FloatToGeometryValue(value.y));
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

        public override void CopyValuesFrom(GeometrySlot foundSlot)
        {
            var slot = foundSlot as Vector2GeometrySlot;
            if (slot != null)
                value = slot.value;
        }

        public override void CopyDefaultValue(GeometrySlot other)
        {
            base.CopyDefaultValue(other);
            if (other is IGeometrySlotHasValue<Vector2> ms)
            {
                m_DefaultValue = ms.defaultValue;
            }
        }
    }
}