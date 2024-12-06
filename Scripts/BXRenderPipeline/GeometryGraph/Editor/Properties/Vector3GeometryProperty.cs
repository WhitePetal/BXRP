using System.Collections;
using System.Collections.Generic;
using BXGraphing;
using UnityEngine;

namespace BXGeometryGraph
{
    public class Vector3GeometryProperty : VectorGeometryProperty
    {
        public Vector3GeometryProperty()
        {
            displayName = "Vector3";
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Vector3; }
        }

        public override Vector4 defaultValue
        {
            get { return new Vector4(value.x, value.y, value.z, 0); }
        }

        public override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return string.Format("float3 {0}{1}", referenceName, delimiter);
        }

        public override PreviewProperty GetPreviewGeometryProperty()
        {
            return new PreviewProperty(PropertyType.Vector3)
            {
                name = referenceName,
                vector4Value = value
            };
        }

        public override INode ToConcreteNode()
        {
            var node = new Vector3Node();
            node.FindInputSlot<Vector1GeometrySlot>(Vector3Node.InputSlotXId).value = value.x;
            node.FindInputSlot<Vector1GeometrySlot>(Vector3Node.InputSlotYId).value = value.y;
            node.FindInputSlot<Vector1GeometrySlot>(Vector3Node.InputSlotZId).value = value.z;
            return node;
        }

        public override IGeometryProperty Copy()
        {
            var copied = new Vector3GeometryProperty();
            copied.displayName = displayName;
            copied.value = value;
            return copied;
        }
    }
}
