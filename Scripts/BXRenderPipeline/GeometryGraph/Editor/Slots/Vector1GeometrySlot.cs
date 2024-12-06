using System;
using System.Collections;
using System.Collections.Generic;
using BXGraphing;
using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    [System.Serializable]
    public class Vector1GeometrySlot : GeometrySlot, IGeometrySlotHasValue<float>
    {
        [SerializeField]
        private float m_Value;

        [SerializeField]
        private float m_DefaultValue;

        private string[] m_Labels;

        public Vector1GeometrySlot()
        {

        }

        public Vector1GeometrySlot(
            int slotId,
            string displayName,
            string geometryOutputName,
            SlotType slotType,
            float value,
            string label1 = "X",
            bool hidden = false) : base(slotId, displayName, geometryOutputName, slotType, hidden)
        {
            m_DefaultValue = value;
            m_Value = value;
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

        public override VisualElement InstantiateControl()
        {
            return new MultiFloatSlotControlView(owner, m_Labels, () => new Vector4(value, 0f, 0f, 0f), (newValue) => value = newValue.x);
        }

        protected override string ConcreteSlotValueAsVariable(AbstractGeometryNode.OutputPrecision precision)
        {
            return NodeUtils.FloatToGeometryValue(value);
        }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            var geoOwner = owner as AbstractGeometryNode;
            if(owner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractGeometryNode)));

            //var property = new vec1
            // TODO
        }

        public override ConcreteSlotValueType concreteValueType => throw new System.NotImplementedException();

        public override SlotValueType valueType => throw new System.NotImplementedException();

        public override void CopyValueFrom(GeometrySlot foundSlot)
        {
            throw new System.NotImplementedException();
        }
    }
}
