using System.Collections;
using System.Collections.Generic;
using System.Text;
using BXGraphing;
using UnityEngine;

namespace BXGeometryGraph
{
    public enum FloatType
    {
        Default,
        Slider,
        Integer
    }

    [System.Serializable]
    [FormerName("BXGeometryGraph.FloatGeometryProperty")]
    [FormerName("BXGeometryGraph.Vector1GeometryProperty")]
    //[BlackboardInputInfo(0, "Float")]
    public sealed class Vector1GeometryProperty : AbstractGeometryProperty<float>
    {
        public Vector1GeometryProperty()
        {
            displayName = "Vector1";
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Vector1; }
        }

        public override Vector4 defaultValue
        {
            get { return new Vector4(value, value, value, value); }
        }

        [SerializeField]
        private FloatType m_FloatType = FloatType.Default;

        public FloatType floatType
        {
            get { return m_FloatType; }
            set
            {
                if (m_FloatType == value)
                    return;
                m_FloatType = value;
            }
        }

        [SerializeField]
        public Vector2 m_RangeValues = new Vector2(0, 1);

        public Vector2 rangeValues
        {
            get { return m_RangeValues; }
            set
            {
                if (m_RangeValues == value)
                    return;
                m_RangeValues = value;
            }
        }

        public override string GetPropertyBlockString()
        {
            var result = new StringBuilder();
            result.Append(referenceName);
            result.Append("(\"");
            result.Append(displayName);
            switch (floatType)
            {
                case FloatType.Slider:
                    result.Append("\", Range(");
                    result.Append(m_RangeValues.x + ", " + m_RangeValues.y);
                    result.Append(")) = ");
                    break;
                case FloatType.Integer:
                    result.Append("\", Int) = ");
                    break;
                default:
                    result.Append("\", Float) = ");
                    break;
            }
            result.Append(value);
            return result.ToString();
        }

        public override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return string.Format("float {0}{1}", referenceName, delimiter);
        }

        public override PreviewProperty GetPreviewGeometryProperty()
        {
            return new PreviewProperty(PropertyType.Vector1)
            {
                name = referenceName,
                floatValue = value
            };
        }

        public override INode ToConcreteNode()
        {
            switch (m_FloatType)
            {
                case FloatType.Slider:
                    return new SliderNode { value = new Vector3(value, m_RangeValues.x, m_RangeValues.y) };
                case FloatType.Integer:
                    return new IntegerNode { value = (int)value };
                default:
                    // TODO
                    //var node = new Vector1Node();
                    //node.FindInputSlot<Vector1GeometrySlot>(Vector1Node.InputSlotXId).value = value;
                    //return node;
                    return new SliderNode { value = new Vector3(value, m_RangeValues.x, m_RangeValues.y) };
            }
        }

        public override IGeometryProperty Copy()
        {
            var copied = new Vector1GeometryProperty();
            copied.displayName = displayName;
            copied.value = value;
            return copied;
        }
    }
}
