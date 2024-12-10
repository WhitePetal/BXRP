using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    [GenerationAPI]
    public class TargetSetupContext
    {
        //public List<SubShaderDescriptor> subShaders { get; private set; }

        //public KernelCollection kernels { get; private set; }
        //public AssetCollection assetCollection { get; private set; }

        //// these are data that are now stored in the subshaders.
        //// but for backwards compatibility with the existing Targets,
        //// we store the values provided directly by the Target,
        //// and apply them to all of the subShaders provided by the Target (that don't have their own setting)
        //// the Targets are free to switch to specifying these values per SubShaderDescriptor instead,
        //// if they want to specify different values for each subshader.
        //private List<ShaderCustomEditor> customEditorForRenderPipelines;
        //private string defaultShaderGUI;

        //// assetCollection is used to gather asset dependencies
        //public TargetSetupContext(AssetCollection assetCollection = null)
        //{
        //    subShaders = new List<SubShaderDescriptor>();
        //    kernels = new KernelCollection();
        //    this.assetCollection = assetCollection;
        //}
    }
}
