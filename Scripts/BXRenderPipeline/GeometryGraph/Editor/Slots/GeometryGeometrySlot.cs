using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    [Serializable]
    class GeometryGeometrySlot : GeometrySlot, IMayRequireGeometry
    {
        [SerializeField]
        private float m_Value;

        [SerializeField]
        private float m_DefaultValue;

        [SerializeField]
        string[] m_Labels;

        static readonly string[] k_LabelDefaults = { "Geometry" };
        string[] labels
        {
            get
            {
                if ((m_Labels == null) || (m_Labels.Length != k_LabelDefaults.Length))
                    return k_LabelDefaults;
                return m_Labels;
            }
        }

        public GeometryGeometrySlot()
        {

        }

        public GeometryGeometrySlot(int slotId, string displayName, string geometryOuputName, SlotType slotType, GeometryStageCapability stageCapability = GeometryStageCapability.All, string label1 = null, bool hidden = false)
            : base(slotId, displayName, geometryOuputName, slotType, stageCapability, hidden)
        {
            m_DefaultValue = value;
            m_Value = value;
            if (label1 != null)
                m_Labels = new[] { label1 };
        }

        public float defaultValue { get { return m_DefaultValue; } }

        public float value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        public override bool isDefaultValue => value.Equals(defaultValue);

        public override VisualElement InstantiateControl()
        {
            return new LabelSlotControlView("Gepmetry");
        }

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            return "Geometry";
        }

        public override SlotValueType valueType { get { return SlotValueType.Geometry; } }

        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.Geometry; } }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            var geoOwner = owner as AbstractGeometryNode;
            if (geoOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractGeometryNode)));

            var property = new GeometryGeometryProperty();
            properties.AddGeometryProperty(property);
        }

        public override void CopyValuesFrom(GeometrySlot foundSlot)
        {
            var slot = foundSlot as GeometryGeometrySlot;
            if (slot != null)
                value = slot.value;
        }

        public override void CopyDefaultValue(GeometrySlot other)
        {
            base.CopyDefaultValue(other);
            if (other is IGeometrySlotHasValue<float> ms)
            {
                m_DefaultValue = ms.defaultValue;
            }
            else if(other is GeometryGeometrySlot gs)
            {
                m_DefaultValue = gs.defaultValue;
            }
        }

        public bool RequiresGeometry(GeometryStageCapability stageCapability = GeometryStageCapability.All)
        {
            return true;
        }
    }
}
