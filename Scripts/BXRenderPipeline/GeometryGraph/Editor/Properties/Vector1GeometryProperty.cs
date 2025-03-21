using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using UnityEngine;

namespace BXGeometryGraph
{
    public enum FloatType
    {
        Default,
        Slider,
        Integer,
        Enum
    }

    public enum EnumType { Enum, CSharpEnum, KeywordEnum}

    [System.Serializable]
    [FormerName("BXGeometryGraph.FloatGeometryProperty")]
    [FormerName("BXGeometryGraph.Vector1GeometryProperty")]
    [BlackboardInputInfo(0, "Float")]
    public sealed class Vector1GeometryProperty : AbstractGeometryProperty<float>
    {
        public Vector1GeometryProperty()
        {
            displayName = "Vector1";
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Float; }
        }

        internal override bool isExposable => true;
        internal override bool isRenamable => true;

        public override float value
        {
            get
            {
                if(floatType == FloatType.Integer)
                {
                    return (int)base.value;
                }
                return base.value;
            }
            set
            {
                base.value = value;
            }
        }

        [SerializeField]
        private FloatType m_FloatType = FloatType.Default;

        public FloatType floatType
        {
            get => m_FloatType;
            set => m_FloatType = value;
        }

        [SerializeField]
        Vector2 m_RangeValues = new Vector2(0, 1);

        public Vector2 rangeValues
        {
            get => m_RangeValues;
            set => m_RangeValues = value;
        }

        EnumType m_EnumType = EnumType.Enum;

        public EnumType enumType
        {
            get => m_EnumType;
            set => m_EnumType = value;
        }

        private Type m_CSharpEnumType;

        public Type cSharpEnumType
        {
            get => m_CSharpEnumType;
            set => m_CSharpEnumType = value;
        }

        List<string> m_EnumNames = new List<string>();

        public List<string> enumNames
        {
            get => m_EnumNames;
            set => m_EnumNames = value;
        }

        List<int> m_EnumValues = new List<int>();

        public List<int> enumValues
        {
            get => m_EnumValues;
            set => m_EnumValues = value;
        }

        private string enumTagString
        {
            get
            {
                switch (enumType)
                {
                    case EnumType.CSharpEnum:
                        return $"[Enum({m_CSharpEnumType.ToString()})]";
                    case EnumType.KeywordEnum:
                        return $"[KeywordEnum({string.Join(", ", enumNames)})]";
                    default:
                        string enumValuesString = "";
                        for (int i = 0; i < enumNames.Count; i++)
                        {
                            int value = (i < enumValues.Count) ? enumValues[i] : i;
                            enumValuesString += (enumNames[i] + ", " + value + ((i != enumNames.Count - 1) ? ", " : ""));
                        }
                        return $"[Enum({enumValuesString})]";
                }
            }
        }

        internal override string GetHLSLVariableName(bool isSubgraphProperty, GenerationMode mode)
        {
            HLSLDeclaration decl = GetDefaultHLSLDeclaration();
            if (decl == HLSLDeclaration.HybridPerInstance)
                return $"UNITY_ACCESS_HYBRID_INSTANCED_PROP({referenceName}, {concretePrecision.ToGeometryString()})";
            else
                return base.GetHLSLVariableName(isSubgraphProperty, mode);
        }

        internal override string GetPropertyBlockString()
        {
            string valueString = NodeUtils.FloatToShaderValueShaderLabSafe(value);

            switch (floatType)
            {
                case FloatType.Slider:
                    return $"{hideTagString}{referenceName}(\"{displayName}\", Range({NodeUtils.FloatToShaderValueShaderLabSafe(m_RangeValues.x)}, {NodeUtils.FloatToShaderValueShaderLabSafe(m_RangeValues.y)})) = {valueString}";
                case FloatType.Integer:
                    return $"{hideTagString}{referenceName}(\"{displayName}\", Int) = {((int)value).ToString(CultureInfo.InvariantCulture)}";
                case FloatType.Enum:
                    return $"{hideTagString}{enumTagString}{referenceName}(\"{displayName}\", Float) = {valueString}";
                default:
                    return $"{hideTagString}{referenceName}(\"{displayName}\", Float) = {valueString}";
            }
        }

        internal override string GetPropertyAsArgumentString(string precisionString)
        {
            return $"{concreteGeometryValueType.ToGeometryString(precisionString)} {referenceName}";
        }

        internal override void ForeachHLSLProperty(Action<HLSLProperty> action)
        {
            HLSLDeclaration decl = GetDefaultHLSLDeclaration();
            action(new HLSLProperty(HLSLType._float, referenceName, decl, concretePrecision));
        }

        internal override AbstractGeometryNode ToConcreteNode()
        {
            switch (m_FloatType)
            {
                case FloatType.Slider:
                    return new SliderNode { value = new Vector3(value, m_RangeValues.x, m_RangeValues.y) };
                case FloatType.Integer:
                    return new IntegerNode { value = (int)value };
                default:
                    var node = new Vector1Node();
                    node.FindInputSlot<Vector1GeometrySlot>(Vector1Node.InputSlotXId).value = value;
                    return node;
            }
        }

        internal override PreviewProperty GetPreviewGeometryProperty()
        {
            return new PreviewProperty(propertyType)
            {
                name = referenceName,
                floatValue = value
            };
        }

        internal override GeometryInput Copy()
        {
            return new Vector1GeometryProperty()
            {
                displayName = displayName,
                value = value,
                floatType = floatType,
                rangeValues = rangeValues,
                enumType = enumType,
                enumNames = enumNames,
                enumValues = enumValues,
            };
        }
    }
}
