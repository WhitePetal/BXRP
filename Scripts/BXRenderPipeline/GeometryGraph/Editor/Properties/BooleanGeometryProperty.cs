using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BXGeometryGraph
{
    [Serializable]
    [FormerName("BXGeometryGraph.BooleanGeometryProperty")]
    [BlackboardInputInfo(20)]
    public class BooleanGeometryProperty : AbstractGeometryProperty<bool>
    {
        public BooleanGeometryProperty()
        {
            displayName = "Boolean";
        }

        public override PropertyType propertyType => PropertyType.Boolean;

        internal override bool isExposable => true;
        internal override bool isRenamable => true;

        internal override string GetPropertyAsArgumentString(string precisionString)
        {
            return $"{concreteGeometryValueType.ToGeometryString(precisionString)} {referenceName}";
        }

        internal override void ForeachHLSLProperty(Action<HLSLProperty> action)
        {
            HLSLDeclaration decl = GetDefaultHLSLDeclaration();
            action(new HLSLProperty(HLSLType._float, referenceName, decl, concretePrecision));
        }

        internal override string GetPropertyBlockString()
        {
            return $"{hideTagString}[ToggleUI]{referenceName}(\"{displayName}\", Float) = {(value == true ? 1 : 0)}";
        }

        internal override AbstractGeometryNode ToConcreteNode()
        {
            return new BooleanNode { value = new ToggleData(value) };
        }

        internal override PreviewProperty GetPreviewGeometryProperty()
        {
            return new PreviewProperty(propertyType)
            {
                name = referenceName,
                booleanValue = value
            };
        }

        internal override GeometryInput Copy()
        {
            return new BooleanGeometryProperty()
            {
                displayName = displayName,
                value = value,
            };
        }
    }
}
