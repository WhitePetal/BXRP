using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    [SerializeField, GenerationAPI] // TODO: Public
    internal abstract class SubTarget : JsonObject
    {
        internal abstract Type targetType { get; }
        internal Target target { get; set; }
        public string displayName { get; set; }
        public bool isHidden { get; set; }
        public abstract bool IsActive();
        public abstract void Setup(ref TargetSetupContext context);
        public abstract void GetFields(ref TargetFieldContext context);
        public abstract void GetActiveBlocks(ref TargetActiveBlockContext context);
        public abstract void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo);

        public virtual void CollectGeometryProperties(PropertyCollector collector, GenerationMode generationMode) { }
        public virtual void ProcessPreviewGeometry(Geometry geometry) { }
        public virtual object saveContext => null;
        public virtual bool IsNodeAllowedBySubTarget(Type nodeType) => true;

        // Call after SubTarget parent Target has been deserialized and Subtarget.target has been set to a non-null value.
        internal virtual void OnAfterParentTargetDeserialized() { }
    }

    [GenerationAPI] // TODO: Public
    internal abstract class SubTarget<T> : SubTarget where T : Target
    {
        internal override Type targetType => typeof(T);

        public new T target
        {
            get => base.target as T;
            set => base.target = value;
        }
    }
}
