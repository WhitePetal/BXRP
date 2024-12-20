using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    class ColorGeometrySlot : Vector4GeometrySlot
    {
        [SerializeField]
        ColorMode m_ColorMode = ColorMode.Default;

        public ColorGeometrySlot() { }

        public ColorGeometrySlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            Color value,
            ColorMode colorMode,
            GeometryStageCapability stageCapability = GeometryStageCapability.All,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, slotType, value, stageCapability, hidden: hidden)
        {
            m_ColorMode = colorMode;
        }

        public ColorMode colorMode
        {
            get { return m_ColorMode; }
            set { m_ColorMode = value; }
        }

        public Color defaultColor => m_DefaultValue;

        public override bool isDefaultValue => value.Equals((Vector4)defaultColor);

        public override VisualElement InstantiateControl()
        {
            return new ColorSlotControlView(this);
        }

        protected override string ConcreteSlotValueAsVariable()
        {
            return string.Format("IsGammaSpace() ? $precision4({0}, {1}, {2}, {3}) : $precision4 (SRGBToLinear($precision3({0}, {1}, {2})), {3})"
                , NodeUtils.FloatToGeometryValue(value.x)
                , NodeUtils.FloatToGeometryValue(value.y)
                , NodeUtils.FloatToGeometryValue(value.z)
                , NodeUtils.FloatToGeometryValue(value.w));
        }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            var matOwner = owner as AbstractGeometryNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractGeometryNode)));

            var property = new ColorGeometryProperty()
            {
                overrideReferenceName = matOwner.GetVariableNameForSlot(id),
                generatePropertyBlock = false,
                value = value
            };
            properties.AddGeometryProperty(property);
        }

        public override void GetPreviewProperties(List<PreviewProperty> properties, string name)
        {
            var pp = new PreviewProperty(PropertyType.Color)
            {
                name = name,
                colorValue = new Color(value.x, value.y, value.z, value.w),
            };
            properties.Add(pp);
        }
    }
}