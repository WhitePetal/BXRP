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

        protected const int tileCountX = 8;
        protected const int tileCountY = 4;
        protected const int maxClusterCountZ = 64;
        protected int clusterCountZ = maxClusterCountZ;


        protected const string BufferName = "ClusterLightCulling";
        protected CommandBuffer commandBuffer = new CommandBuffer()
        {
            name = BufferName
        };

        public abstract void Render(Camera camera, BXLightsBase lights, BXRenderCommonSettings commonSettings, int width, int height);

        public abstract void Dispose();
    }
}
