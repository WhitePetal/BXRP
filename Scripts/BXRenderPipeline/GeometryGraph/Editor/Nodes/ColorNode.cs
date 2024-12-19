using System;
using System.Collections;
using System.Collections.Generic;
using BXGraphing;
using UnityEditor;
using UnityEngine;

namespace BXGeometryGraph
{
    enum ColorMode
    {
        Default,
        HDR
    }

    [Title("Input", "Basic", "Color")]
    class ColorNode : AbstractGeometryNode, IGeneratesShaderBodyCode, IPropertyFromNode
    {
        public const int OutputSlotId = 0;
        private const string kOutputSlotName = "Out";

        public override int latestVersion => 1;

        public ColorNode()
        {
            name = "Color";
            synonyms = new string[] { "rgba" };
            UpdateNodeAfterDeserialization();
        }

        [SerializeField]
        Color m_Color = new Color(UnityEngine.Color.clear, ColorMode.Default);

        [Serializable]
        public struct Color
        {
            public UnityEngine.Color color;
            public ColorMode mode;

            public Color(UnityEngine.Color color, ColorMode mode)
            {
                this.color = color;
                this.mode = mode;
            }
        }

        [ColorControl("")]
        public Color color
        {
            get { return m_Color; }
            set
            {
                if ((value.color == m_Color.color) && (value.mode == m_Color.mode))
                    return;

                if ((value.mode != m_Color.mode) && (value.mode == ColorMode.Default))
                {
                    float r = Mathf.Clamp(value.color.r, 0, 1);
                    float g = Mathf.Clamp(value.color.g, 0, 1);
                    float b = Mathf.Clamp(value.color.b, 0, 1);
                    float a = Mathf.Clamp(value.color.a, 0, 1);
                    value.color = new UnityEngine.Color(r, g, b, a);
                }

                m_Color = value;
                Dirty(ModificationScope.Graph);
            }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4GeometrySlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        public override void CollectGeometryProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            properties.AddGeometryProperty(new ColorGeometryProperty()
            {
                overrideReferenceName = GetVariableNameForNode(),
                generatePropertyBlock = false,
                value = color.color,
                colorMode = color.mode
            });
        }

        public void GenerateNodeShaderCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (generationMode.IsPreview())
                return;


            //HDR color picker assumes Linear space, regular color picker assumes SRGB. Handle both cases
            if (color.mode == ColorMode.Default)
            {
                sb.AppendLine(@"$precision4 {0} = IsGammaSpace() ? $precision4({1}, {2}, {3}, {4}) : $precision4(SRGBToLinear($precision3({1}, {2}, {3})), {4});"
                    , GetVariableNameForNode()
                    , NodeUtils.FloatToGeometryValue(color.color.r)
                    , NodeUtils.FloatToGeometryValue(color.color.g)
                    , NodeUtils.FloatToGeometryValue(color.color.b)
                    , NodeUtils.FloatToGeometryValue(color.color.a));
            }
            else
            {
                sb.AppendLine(@"$precision4 {0} = IsGammaSpace() ? LinearToSRGB($precision4({1}, {2}, {3}, {4})) : $precision4({1}, {2}, {3}, {4});"
                    , GetVariableNameForNode()
                    , NodeUtils.FloatToGeometryValue(color.color.r)
                    , NodeUtils.FloatToGeometryValue(color.color.g)
                    , NodeUtils.FloatToGeometryValue(color.color.b)
                    , NodeUtils.FloatToGeometryValue(color.color.a));
            }
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return GetVariableNameForNode();
        }

        public override void CollectPreviewGeometryProperties(List<PreviewProperty> properties)
        {
            UnityEngine.Color propColor = color.color;
            if (color.mode == ColorMode.Default)
            {
                if (PlayerSettings.colorSpace == ColorSpace.Linear)
                    propColor = propColor.linear;
            }
            if (color.mode == ColorMode.HDR)
            {
                if (PlayerSettings.colorSpace == ColorSpace.Gamma)
                    propColor = propColor.gamma;
            }

            // we use Vector4 type to avoid all of the automatic color conversions of PropertyType.Color
            properties.Add(new PreviewProperty(PropertyType.Vector4)
            {
                name = GetVariableNameForNode(),
                vector4Value = propColor
            });
        }

        public AbstractGeometryProperty AsGeometryProperty()
        {
            return new ColorGeometryProperty() { value = color.color, colorMode = color.mode };
        }

        public int outputSlotID { get { return OutputSlotId; } }
    }
}
