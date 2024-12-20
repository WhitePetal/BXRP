using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace BXGeometryGraph
{
    [System.Serializable]
    [FormerName("BXGeometryGraph.Vector4GeometryProperty")]
    [BlackboardInputInfo(4)]
    public class Vector4GeometryProperty : VectorGeometryProperty
    {
        internal Vector4GeometryProperty()
        {
            displayName = "Vector4";
        }

        internal override int vectorDimension => 4;

        public override PropertyType propertyType => PropertyType.Vector4;

        internal override AbstractGeometryNode ToConcreteNode()
        {
            var node = new Vector4Node();
            node.FindInputSlot<Vector1GeometrySlot>(Vector4Node.InputSlotXId).value = value.x;
            node.FindInputSlot<Vector1GeometrySlot>(Vector4Node.InputSlotYId).value = value.y;
            node.FindInputSlot<Vector1GeometrySlot>(Vector4Node.InputSlotZId).value = value.z;
            node.FindInputSlot<Vector1GeometrySlot>(Vector4Node.InputSlotWId).value = value.w;
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
            return new Vector4GeometryProperty()
            {
                displayName = displayName,
                value = value,
            };
        }

        internal override void ForeachHLSLProperty(Action<HLSLProperty> action)
        {
            HLSLDeclaration decl = GetDefaultHLSLDeclaration();
            action(new HLSLProperty(HLSLType._float4, referenceName, decl, concretePrecision));
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
