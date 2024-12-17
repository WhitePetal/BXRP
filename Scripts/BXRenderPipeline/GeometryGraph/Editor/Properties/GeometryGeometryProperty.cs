using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    [Serializable]
    [FormerName("BXGeometryGraph.GeometryGeometryProperty")]
    [BlackboardInputInfo(5)]
    public class GeometryGeometryProperty : AbstractGeometryProperty
    {
        internal override bool isExposed => true;
        internal override bool isRenamable => true;

        public override PropertyType propertyType => throw new NotImplementedException();

        internal override bool isExposable => throw new NotImplementedException();

        internal override void ForeachHLSLProperty(Action<HLSLProperty> action)
        {
            return;
        }

        internal override PreviewProperty GetPreviewGeometryProperty()
        {
            return new PreviewProperty(propertyType)
            {
                name = referenceName,
            };
        }

        internal override string GetPropertyAsArgumentString(string precisionString)
        {
            return $"{concreteGeometryValueType.ToGeometryString(precisionString)} {referenceName}";
        }

        internal override AbstractGeometryNode ToConcreteNode()
        {
            throw new NotImplementedException();
        }

        internal override GeometryInput Copy()
        {
            return new GeometryGeometryProperty()
            {
                displayName = displayName
            };
        }
    }
}
