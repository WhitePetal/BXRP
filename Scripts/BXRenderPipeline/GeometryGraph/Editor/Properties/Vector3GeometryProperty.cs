using System;
using System.Collections;
using System.Collections.Generic;
using BXGraphing;
using UnityEngine;

namespace BXGeometryGraph
{
    [Serializable]
    [FormerName("BXGeometryGraph.Vector3GeometryProperty")]
    [BlackboardInputInfo(3)]
    public sealed class Vector3GeometryProperty : VectorGeometryProperty
    {
        internal Vector3GeometryProperty()
        {
            displayName = "Vector3";
        }

        internal override int vectorDimension => 3;

        public override PropertyType propertyType => PropertyType.Vector3;

        internal override AbstractGeometryNode ToConcreteNode()
        {
            var node = new Vector3Node();
            node.FindInputSlot<Vector1GeometrySlot>(Vector3Node.InputSlotXId).value = value.x;
            node.FindInputSlot<Vector1GeometrySlot>(Vector3Node.InputSlotYId).value = value.y;
            node.FindInputSlot<Vector1GeometrySlot>(Vector3Node.InputSlotZId).value = value.z;
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
            return new Vector3GeometryProperty()
            {
                displayName = displayName,
                value = value,
            };
        }

        internal override void ForeachHLSLProperty(Action<HLSLProperty> action)
        {
            HLSLDeclaration decl = GetDefaultHLSLDeclaration();
            action(new HLSLProperty(HLSLType._float3, referenceName, decl, concretePrecision));
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
