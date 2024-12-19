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
    class DynamicVectorGeometrySlot : GeometrySlot, IGeometrySlotHasValue<Vector4>
    {
        [SerializeField]
        private Vector4 m_Value;

        [SerializeField]
        private Vector4 m_DefaultValue = Vector4.zero;

        private static readonly string[] k_Labels = { "X", "Y", "Z", "W" };

        private ConcreteSlotValueType m_ConcreteValueType = ConcreteSlotValueType.Vector4;

        public DynamicVectorGeometrySlot()
        {

        }

        public DynamicVectorGeometrySlot(
                    int slotId,
                    string displayName,
                    string shaderOutputName,
                    SlotType slotType,
                    Vector4 value,
                    GeometryStageCapability stageCapability = GeometryStageCapability.All,
                    bool hidden = false)
                    : base(slotId, displayName, shaderOutputName, slotType, stageCapability, hidden)
        {
            m_Value = value;
        }

        public Vector4 defaultValue { get { return m_DefaultValue; } }

        public Vector4 value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        public override bool isDefaultValue => value.Equals(defaultValue);

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
            var propType = concreteValueType.ToPropertyType();
            var pp = new PreviewProperty(propType) { name = name };
            if (propType == PropertyType.Float)
                pp.floatValue = value.x;
            else
                pp.vector4Value = new Vector4(value.x, value.y, value.z, value.w);
            properties.Add(pp);
        }

        protected override string ConcreteSlotValueAsVariable()
        {
            var channelCount = SlotValueHelper.GetChannelCount(concreteValueType);
            string values = NodeUtils.FloatToGeometryValue(value.x);
            if (channelCount == 1)
                return string.Format("$precision({0})", values);
            for (var i = 1; i < channelCount; i++)
                values += ", " + NodeUtils.FloatToGeometryValue(value[i]);
            return string.Format("$precision{0}({1})", channelCount, values);
        }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            var matOwner = owner as AbstractGeometryNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractGeometryNode)));

            AbstractGeometryProperty property;
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
                    // This shouldn't happen due to edge validation. The generated shader will
                    // have errors.
                    Debug.LogError($"Invalid value type {concreteValueType} passed to Vector Slot {displayName}. Value will be ignored, please plug in an edge with a vector type.");
                    return;
            }

            property.overrideReferenceName = matOwner.GetVariableNameForSlot(id);
            property.generatePropertyBlock = false;
            properties.AddGeometryProperty(property);
        }

        public override void CopyValuesFrom(GeometrySlot foundSlot)
        {
            var slot = foundSlot as DynamicVectorGeometrySlot;
            if (slot != null)
                value = slot.value;
        }

        public override void CopyDefaultValue(GeometrySlot other)
        {
            base.CopyDefaultValue(other);
            if (other is IGeometrySlotHasValue<Vector4> ms)
            {
                m_DefaultValue = ms.defaultValue;
            }
        }
    }
}
