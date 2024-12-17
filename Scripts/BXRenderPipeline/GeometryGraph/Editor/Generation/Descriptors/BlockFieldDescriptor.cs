using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    internal class BlockFieldDescriptor : FieldDescriptor
    {
        public string displayName { get; }
        public IControl control { get; }
        public GeometryStage geometryStage { get; }
        public bool isHidden { get; }
        public bool isUnknown { get; }
        public bool isCustom { get; }

        internal string path { get; set; }

        public BlockFieldDescriptor(string tag, string referenceName, string define, IControl control, GeometryStage geometryStage, bool isHidden = false, bool isUnknown = false, bool isCustom = false)
            : base(tag, referenceName, define)
        {
            this.displayName = referenceName;
            this.control = control;
            this.geometryStage = geometryStage;
            this.isHidden = isHidden;
            this.isUnknown = isUnknown;
            this.isCustom = isCustom;
        }

        public BlockFieldDescriptor(string tag, string referenceName, string displayName, string define, IControl control, GeometryStage geometryStage, bool isHidden = false, bool isUnknown = false, bool isCustom = false)
            : base(tag, referenceName, define)
        {
            this.displayName = displayName;
            this.control = control;
            this.geometryStage = geometryStage;
            this.isHidden = isHidden;
            this.isUnknown = isUnknown;
            this.isCustom = isCustom;
        }
    }

    // TODO: This exposes the MaterialSlot API
    // TODO: This needs to be removed but is currently required by HDRP for DiffusionProfileInputMaterialSlot
    internal class CustomSlotBlockFieldDescriptor : BlockFieldDescriptor
    {
        public Func<GeometrySlot> createSlot;

        public CustomSlotBlockFieldDescriptor(string tag, string referenceName, string define, Func<GeometrySlot> createSlot) : base(tag, referenceName, define, null, GeometryStage.Geometry)
        {
            this.createSlot = createSlot;
        }

        public CustomSlotBlockFieldDescriptor(string tag, string referenceName, string displayName, string define, Func<GeometrySlot> createSlot)
            : base(tag, referenceName, displayName, define, null, GeometryStage.Geometry)
        {
            this.createSlot = createSlot;
        }
    }
}
