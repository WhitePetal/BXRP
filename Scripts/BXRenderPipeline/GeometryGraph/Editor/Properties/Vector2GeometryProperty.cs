using System.Collections;
using System.Collections.Generic;
using BXGraphing;
using UnityEngine;

namespace BXGeometryGraph
{
    [System.Serializable]
    public class Vector2GeometryProperty : VectorGeometryProperty
    {
        public Vector2GeometryProperty()
        {
            displayName = "Vector2";
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Vector2; }
        }

        public override Vector4 defaultValue
        {
            get { return new Vector4(value.x, value.y, 0, 0); }
        }

        public override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return string.Format("float2 {0}{1}", referenceName, delimiter);
        }

        public override PreviewProperty GetPreviewGeometryProperty()
        {
            return new PreviewProperty(PropertyType.Vector2)
            {
                name = referenceName,
                vector4Value = value
            };
        }

        public override INode ToConcreteNode()
        {
            var node = new Vector2Node();
            node.FindInputSlot<Vector1GeometrySlot>(Vector2Node.InputSlotXId).value = value.x;
            node.FindInputSlot<Vector1GeometrySlot>(Vector2Node.InputSlotYId).value = value.y;
            return node;
        }

        public override IGeometryProperty Copy()
        {
            var copied = new Vector2GeometryProperty();
            copied.displayName = displayName;
            copied.value = value;
            return copied;
        }
    }
}
