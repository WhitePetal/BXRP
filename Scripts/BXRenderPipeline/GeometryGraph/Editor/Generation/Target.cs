using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BXGeometryGraph
{
    [SerializeField, GenerationAPI] // TODO: Public
    internal abstract class Target : JsonObject
    {
        public string displayName { get; set; }
        public bool isHidden { get; set; }
        internal virtual bool ignoreCustomInterpolators => true;
        internal virtual int padCustomInterpolatorLimit => 4;
        internal virtual bool prefersSpritePreview => false;
        public abstract bool IsActive();
        public abstract void Setup(ref TargetSetupContext context);
        public abstract void GetFields(ref TargetFieldContext context);
        public abstract void GetActiveBlocks(ref TargetActiveBlockContext context);
        public abstract void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<string> registerUndo);
        public virtual void CollectGeometryProperties(PropertyCollector collector, GenerationMode generationMode) { }
        public virtual void ProcessPreviewGeometry(Geometry geometry) { }
        public virtual object saveContext => null;
        public virtual bool IsNodeAllowedByTarget(Type nodeType)
        {
            NeverAllowedByTargetAttribute never = NodeClassCache.GetAttributeOnNodeType<NeverAllowedByTargetAttribute>(nodeType);
            return never == null;
        }

        public virtual bool DerivativeModificationCallback(
                out string dstGraphFunctions,
                out string dstGraphPixel,
                out bool[] adjustedUvDerivs,
                string primaryShaderName,
                string passName,
                string propStr,
                string surfaceDescStr,
                string graphFuncStr,
                string graphPixelStr,
                List<string> customFuncs,
                bool applyEmulatedDerivatives)
        {
            dstGraphFunctions = "";
            dstGraphPixel = "";
            adjustedUvDerivs = new bool[4];
            return false;
        }

        // think this is not called by anyone anymore, leaving it to avoid changing client code
        public abstract bool WorksWithSRP(RenderPipelineAsset scriptableRenderPipeline);
    }
}
