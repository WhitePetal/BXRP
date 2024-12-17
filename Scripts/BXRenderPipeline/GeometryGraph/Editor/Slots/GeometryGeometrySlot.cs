using System;
using System.Collections;
using System.Collections.Generic;
using BXGraphing;
using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    [Serializable]
    class GeometryGeometrySlot : GeometrySlot, IMayRequireGeometry
    {
        public GeometryGeometrySlot()
        {

        }

        public GeometryGeometrySlot(int slotId, string displayName, string geometryOuputName, SlotType slotType, GeometryStageCapability stageCapability = GeometryStageCapability.All, bool hidden = false)
            : base(slotId, displayName, geometryOuputName, slotType, stageCapability, hidden)
        {

        }

        public override VisualElement InstantiateControl()
        {
            return new LabelSlotControlView("Gepmetry");
        }

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            return "Geometry";
        }

        public override SlotValueType valueType { get { return SlotValueType.Geometry; } }

        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.Geometry; } }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            var geoOwner = owner as AbstractGeometryNode;
            if (geoOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractGeometryNode)));

            var property = new GeometryGeometryProperty();
            properties.AddGeometryProperty(property);
        }

        public override void CopyValuesFrom(GeometrySlot foundSlot)
        {
            return; 
        }

        public bool RequiresGeometry(GeometryStageCapability stageCapability = GeometryStageCapability.All)
        {
            return true;
        }
    }
}
