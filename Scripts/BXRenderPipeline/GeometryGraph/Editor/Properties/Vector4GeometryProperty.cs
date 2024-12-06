using System.Collections;
using System.Collections.Generic;
using BXGraphing;
using UnityEngine;

namespace BXGeometryGraph
{
    [System.Serializable]
    public class Vector4GeometryProperty : VectorGeometryProperty
    {
        public Vector4GeometryProperty()
        {
            displayName = "Vector4";
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Vector4; }
        }

        public override Vector4 defaultValue
        {
            get { return value; }
        }

        public override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return string.Format("float4 {0}{1}", referenceName, delimiter);
        }

        public override PreviewProperty GetPreviewGeometryProperty()
        {
            return new PreviewProperty(PropertyType.Vector4)
            {
                name = referenceName,
                vector4Value = value
            };
        }

        public override INode ToConcreteNode()
        {
            var node = new Vector4Node();
            //node.findin
            // TODO
            return default;
        }

        public override IGeometryProperty Copy()
        {
            throw new System.NotImplementedException();
        }
    }
}
