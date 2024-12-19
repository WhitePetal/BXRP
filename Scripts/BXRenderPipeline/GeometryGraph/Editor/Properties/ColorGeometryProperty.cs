using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BXGeometryGraph
{
    [Serializable]
    [FormerName("UnityEditor.ShaderGraph.ColorShaderProperty")]
    [BlackboardInputInfo(10)]
    sealed class ColorGeometryProperty : AbstractGeometryProperty<Color>
    {
        internal ColorGeometryProperty()
        {
            displayName = "Color";
        }

        internal ColorGeometryProperty(int version) : this()
        {
            this.ggVersion = version;
        }

        public override PropertyType propertyType => PropertyType.Color;

        internal override bool isExposable => true;
        internal override bool isRenamable => true;

        [SerializeField]
        internal bool isMainColor = false;

        internal string hdrTagString => colorMode == ColorMode.HDR ? "[HDR]" : "";

        internal string mainColorString => isMainColor ? "[MainColor]" : "";

        internal override string GetPropertyBlockString()
        {
            return $"{hideTagString}{hdrTagString}{mainColorString}{referenceName}(\"{displayName}\", Color) = ({NodeUtils.FloatToShaderValueShaderLabSafe(value.r)}, {NodeUtils.FloatToShaderValueShaderLabSafe(value.g)}, {NodeUtils.FloatToShaderValueShaderLabSafe(value.b)}, {NodeUtils.FloatToShaderValueShaderLabSafe(value.a)})";
        }

        internal override string GetPropertyAsArgumentString(string precisionString)
        {
            return $"{concreteGeometryValueType.ToGeometryString(precisionString)} {referenceName}";
        }

        internal override void ForeachHLSLProperty(Action<HLSLProperty> action)
        {
            HLSLDeclaration decl = GetDefaultHLSLDeclaration();
            action(new HLSLProperty(HLSLType._float4, referenceName, decl, concretePrecision));
        }

        public override string GetOldDefaultReferenceName()
        {
            return $"Color_{objectId}";
        }

        [SerializeField]
        ColorMode m_ColorMode;

        public ColorMode colorMode
        {
            get => m_ColorMode;
            set => m_ColorMode = value;
        }

        internal override AbstractGeometryNode ToConcreteNode()
        {
            return new ColorNode { color = new ColorNode.Color(value, colorMode) };
        }

        internal override PreviewProperty GetPreviewGeometryProperty()
        {
            UnityEngine.Color propColor = value;
            if (colorMode == ColorMode.Default)
            {
                if (PlayerSettings.colorSpace == ColorSpace.Linear)
                    propColor = propColor.linear;
            }
            else if (colorMode == ColorMode.HDR)
            {
                // conversion from linear to active color space is handled in the shader code (see PropertyNode.cs)
            }

            // we use Vector4 type to avoid all of the automatic color conversions of PropertyType.Color
            return new PreviewProperty(PropertyType.Vector4)
            {
                name = referenceName,
                vector4Value = propColor
            };
        }

        internal override string GetHLSLVariableName(bool isSubgraphProperty, GenerationMode mode)
        {
            HLSLDeclaration decl = GetDefaultHLSLDeclaration();
            if (decl == HLSLDeclaration.HybridPerInstance)
                return $"UNITY_ACCESS_HYBRID_INSTANCED_PROP({referenceName}, {concretePrecision.ToGeometryString()}4)";
            else
                return base.GetHLSLVariableName(isSubgraphProperty, mode);
        }

        internal override GeometryInput Copy()
        {
            return new ColorGeometryProperty()
            {
                ggVersion = ggVersion,
                displayName = displayName,
                value = value,
                colorMode = colorMode,
                isMainColor = isMainColor
            };
        }

        public override void OnAfterDeserialize(string json)
        {
            //if (ggVersion < 2)
            //{
            //    LegacyShaderPropertyData.UpgradeToHLSLDeclarationOverride(json, this);
            //    // version 0 upgrades to 2
            //    // version 1 upgrades to 3
            //    ChangeVersion((ggVersion == 0) ? 2 : 3);
            //}
        }

        internal override void OnBeforePasteIntoGraph(GraphData graph)
        {
            if (isMainColor)
            {
                ColorGeometryProperty existingMain = graph.GetMainColor();
                if (existingMain != null && existingMain != this)
                {
                    isMainColor = false;
                }
            }
            base.OnBeforePasteIntoGraph(graph);
        }
    }
}
