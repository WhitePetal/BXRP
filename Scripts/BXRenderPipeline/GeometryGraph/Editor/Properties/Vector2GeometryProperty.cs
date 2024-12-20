using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace BXGeometryGraph
{
    [System.Serializable]
    [FormerName("BXGeometryGraph.Vector2GeometryProperty")]
    [BlackboardInputInfo(1)]
    public sealed class Vector2GeometryProperty : VectorGeometryProperty
    {
        public Vector2GeometryProperty()
        {
            displayName = "Vector2";
        }
        
        internal override int vectorDimension => 2;

        public override PropertyType propertyType => PropertyType.Vector2;

        internal override AbstractGeometryNode ToConcreteNode()
        {
            var node = new Vector2Node();
            node.FindInputSlot<Vector1GeometrySlot>(Vector2Node.InputSlotXId).value = value.x;
            node.FindInputSlot<Vector1GeometrySlot>(Vector2Node.InputSlotYId).value = value.y;
            return node;
        }

        internal override PreviewProperty GetPreviewGeometryProperty()
        {
            return new PreviewProperty(propertyType)
            {
                name = referenceName,
                vector4Value = value
            };
        }

        internal override GeometryInput Copy()
        {
            return new Vector2GeometryProperty()
            {
                displayName = displayName,
                value = value,
            };
        }

        internal override void ForeachHLSLProperty(Action<HLSLProperty> action)
        {
            HLSLDeclaration decl = GetDefaultHLSLDeclaration();
            action(new HLSLProperty(HLSLType._float2, referenceName, decl, concretePrecision));
        }

        public override int latestVersion => 1;
        public override void OnAfterDeserialize(string json)
        {
            //if (ggVersion == 0)
            //{
            //    LegacyShaderPropertyData.UpgradeToHLSLDeclarationOverride(json, this);
            //    ChangeVersion(1);
            //}
        }
    }
}
