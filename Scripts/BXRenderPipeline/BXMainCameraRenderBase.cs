using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BXRenderPipeline
{
    public abstract class BXMainCameraRenderBase : IDisposable
    {
        public Camera camera;
        public ScriptableRenderContext context;
        public CullingResults cullingResults;

#if UNITY_EDITOR
        protected static Material material_error = new Material(Shader.Find("Hidden/InternalErrorShader"));
#endif

        protected const string commandBufferName = "BXCommondBufferRender";

#if !UNITY_EDITOR
        protected string SampleName = commandBufferName;
#endif

        public CommandBuffer commandBuffer = new CommandBuffer
        {
            name = commandBufferName
        };

        public BXRenderCommonSettings commonSettings;

        public float maxShadowDistance;
        public int width, height, width_screen, height_screen;

        protected FilteringSettings filterSettings_opaue = new FilteringSettings(RenderQueueRange.opaque);
        protected FilteringSettings filterSettings_transparent = new FilteringSettings(RenderQueueRange.transparent);
        protected PerObjectData fullLightPerObjectFlags = PerObjectData.ReflectionProbes | PerObjectData.Lightmaps | PerObjectData.ShadowMask | PerObjectData.OcclusionProbe |
            PerObjectData.LightProbe | PerObjectData.LightProbeProxyVolume | PerObjectData.OcclusionProbeProxyVolume | PerObjectData.None;

        protected Material postProcessMat;

        public abstract void Dispose();
    }
}
