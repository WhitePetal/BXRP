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
        public static class BaseShaderProperties
        {
            public static readonly int _DirectionalLightCount_ID = Shader.PropertyToID("_DirectionalLightCount");
            public static readonly int _DirectionalLightDirections_ID = Shader.PropertyToID("_DirectionalLightDirections");
            public static readonly int _DirectionalLightColors_ID = Shader.PropertyToID("_DirectionalLightColors");
            public static readonly int _DirectionalShadowDatas_ID = Shader.PropertyToID("_DirectionalShadowDatas");

            public static readonly int _ClusterLightCount_ID = Shader.PropertyToID("_ClusterLightCount");
            public static readonly int _StencilLightCount_ID = Shader.PropertyToID("_StencilLightCount");

            public static readonly int _OtherLightSpheres_ID = Shader.PropertyToID("_OtherLightSpheres");
            public static readonly int _OtherLightDirections_ID = Shader.PropertyToID("_OtherLightDirections");
            public static readonly int _OtherLightThresholds_ID = Shader.PropertyToID("_OtherLightThresholds");
            public static readonly int _OtherLightColors_ID = Shader.PropertyToID("_OtherLightColors");
            public static readonly int _OtherShadowDatas_ID = Shader.PropertyToID("_OtherShadowDatas");
        }

        protected const string BufferName = "Lights";
        protected CommandBuffer commandBuffer = new CommandBuffer()
        {
            name = BufferName
        };

        public const int maxDirLightCount = 1;
        public const int maxClusterLightCount = 32;
        public const int maxStencilLightCount = 8;
        public readonly int maxOtherLightCount;
        public readonly int maxImportedOtherLightCount;

        protected Camera camera;
        protected BXRenderCommonSettings commonSettings;
        protected ScriptableRenderContext context;
        protected CullingResults cullingResults;

        protected int width, height;

        protected GlobalKeyword dirLightKeyword = GlobalKeyword.Create("DIRECTIONAL_LIGHT");
        protected GlobalKeyword clusterLightKeyword = GlobalKeyword.Create("CLUSTER_LIGHT");

        protected BXReflectionProbeManager reflectionProbe = BXReflectionProbeManager.Create();

        public int dirLightCount;
        public int clusterLightCount;
        public int stencilLightCount;
        public int importedOtherLightCount;
        public int reflectProneCount => reflectionProbe.probeCount;

        public Vector4[]
            clusterLightMinBounds = new Vector4[maxClusterLightCount],
            clusterLightMaxBounds = new Vector4[maxClusterLightCount];

        public NativeArray<VisibleReflectionProbe> reflectionProbes => reflectionProbe.m_Probes;
        //public Vector4[] reflectProbeMinBounds => reflectionProbe.m_BoxMin;
        //public Vector4[] reflectProbeMaxBounds => reflectionProbe.m_BoxMax;

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
            // clusterLights
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
