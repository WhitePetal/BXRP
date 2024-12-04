using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BXRenderPipeline
{
    public abstract class BXClusterCullBase : IDisposable
    {
        protected static partial class BaseShaderProperties
        {
            public static readonly int _ClusterSize_ID = Shader.PropertyToID("_ClusterSize");
            public static readonly int _ClusterLightingIndices_ID = Shader.PropertyToID("_ClusterLightingIndices");
            public static readonly int _ClusterLightingDatas_ID = Shader.PropertyToID("_ClusterLightingDatas");
        }

        protected static readonly GlobalKeyword _CLUSTER_GREATE_32 = GlobalKeyword.Create("_CLUSTER_GREATE_32");

        protected const int maxZBinWords = 1024 * 4;
        protected const int maxTileWords = 1024 * 4;

        public abstract void Setup(Camera camera, BXLightsBase lights, int width, int height);

        public abstract void Upload(CommandBuffer commandBuffer);

        public abstract void Dispose();
    }
}
