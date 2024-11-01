using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace BXRenderPipeline
{
    public abstract class BXLightsBase : IDisposable
    {
        public const int maxDirLightCount = 1;
        public const int maxClusterLightCount = 64;
        public const int maxStencilLightCount = 8;
        public readonly int maxOtherLightCount;
        public readonly int maxImportedOtherLightCount;

        public int dirLightCount;
        public int clusterLightCount;
        public int stencilLightCount;
        public int importedOtherLightCount;

        public Vector4[]
            clusterLightMinBounds = new Vector4[maxClusterLightCount],
            clusterLightMaxBounds = new Vector4[maxClusterLightCount];

        public Vector4[]
            dirLightColors = new Vector4[maxDirLightCount],
            dirLightDirections = new Vector4[maxDirLightCount],
            dirShadowDatas = new Vector4[maxDirLightCount],
            otherLightSpheres,
            otherLightColors,
            otherLightDirections,
            otherLightThresholds,
            otherShadowDatas;

        public NativeArray<VisibleLight>
            dirLights = new NativeArray<VisibleLight>(maxDirLightCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory),
            otherLights = new NativeArray<VisibleLight>(maxClusterLightCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        public BXLightsBase(int maxOtherLightCount, int maxImportedOtherLightCount)
        {
            this.maxOtherLightCount = maxOtherLightCount;
            this.maxImportedOtherLightCount = maxImportedOtherLightCount;

            otherLightSpheres = new Vector4[maxOtherLightCount];
            otherLightColors = new Vector4[maxOtherLightCount];
            otherLightDirections = new Vector4[maxOtherLightCount];
            otherLightThresholds = new Vector4[maxOtherLightCount];
            otherShadowDatas = new Vector4[maxImportedOtherLightCount];
        }

        public virtual void Dispose()
        {
            clusterLightMinBounds = null;
            clusterLightMaxBounds = null;
            dirLightColors = null;
            dirLightDirections = null;
            dirShadowDatas = null;
            otherLightColors = null;
            otherLightDirections = null;
            otherLightThresholds = null;
            otherShadowDatas = null;

            dirLights.Dispose();
            otherLights.Dispose();

        }
    }
}
