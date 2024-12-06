using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BXGraphing;
using UnityEngine.UIElements;
using System.Linq;
using System;

namespace BXGeometryGraph
{
    [System.Serializable]
    public class DynamicVectorGeometrySlot : GeometrySlot
    {
        [SerializeField]
        private Vector4 m_Value;

        [SerializeField]
        private Vector4 m_DefaultValue;

        private static readonly string[] k_Labels = { "X", "Y", "Z", "W" };

        private ConcreteSlotValueType m_ConcreteValueType = ConcreteSlotValueType.Vector4;

        public DynamicVectorGeometrySlot()
        {

        }

        public DynamicVectorGeometrySlot(
            int slotId,
            string displayName,
            string geometryOutputName,
            SlotType slotType,
            Vector4 value,
            bool hidden = false) : base(slotId, displayName, geometryOutputName, slotType, hidden)
        {
            m_Value = value;
        }

        public Vector4 defaultValue { get { return m_DefaultValue; } }

        public Vector4 value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        public override VisualElement InstantiateControl()
        {
            var labels = k_Labels.Take(concreteValueType.GetChannelCount()).ToArray();
            return new MultiFloatSlotControlView(owner, labels, () => value, (newValue) => value = newValue);
        }

        public override SlotValueType valueType { get { return SlotValueType.DynamicVector; } }

        public override ConcreteSlotValueType concreteValueType
        {
            get { return m_ConcreteValueType; }
        }

        public void SetConcreteType(ConcreteSlotValueType valueType)
        {
            m_ConcreteValueType = valueType;
        }

        public override void GetPreviewProperties(List<PreviewProperty> properties, string name)
        {
            var propType = ConvertConcreteSlotValueTypeToPropertyType(concreteValueType);
            var pp = new PreviewProperty(propType) { name = name };
            if (propType == PropertyType.Vector1)
                pp.floatValue = value.x;
            else
                pp.vector4Value = new Vector4(value.x, value.y, value.z, value.w);
            properties.Add(pp);
        }

        protected override string ConcreteSlotValueAsVariable(AbstractGeometryNode.OutputPrecision precision)
        {
            var channelCount = SlotValueHelper.GetChannelCount(concreteValueType);
            string values = NodeUtils.FloatToGeometryValue(value.x);
            if (channelCount == 1)
                return values;
            for (var i = 1; i < channelCount; ++i)
                values += ", " + NodeUtils.FloatToGeometryValue(value[i]);
            return string.Format("{0}{1}{2}", precision, channelCount, values);
        }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            var matOwner = owner as AbstractGeometryNode;
            if(matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractGeometryNode)));

            IGeometryProperty property;
            switch (concreteValueType)
            {
                case ConcreteSlotValueType.Vector4:
                    property = new Vector4GeometryProperty();
                    break;
                case ConcreteSlotValueType.Vector3:
                    property = new Vector3GeometryProperty();
                    break;
                case ConcreteSlotValueType.Vector2:
                    property = new Vector2GeometryProperty();
                    break;
                case ConcreteSlotValueType.Vector1:
                    property = new Vector1GeometryProperty();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            property.overrideReferenceName = matOwner.GetVariableNameForSlot(id);
            property.generatePropertyBlock = false;
            properties.AddGeometryProperty(property);
        }

        public override void CopyValueFrom(GeometrySlot foundSlot)
        {
            var slot = foundSlot as DynamicVectorGeometrySlot;
            if (slot != null)
                value = slot.value;
        }
    }
}
