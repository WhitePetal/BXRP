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

        public Matrix4x4 worldToViewMatrix = Matrix4x4.identity;
        public Matrix4x4 viewToWorldMatrix = Matrix4x4.identity;

        protected FilteringSettings filterSettings_opaue = new FilteringSettings(RenderQueueRange.opaque);
        protected FilteringSettings filterSettings_transparent = new FilteringSettings(RenderQueueRange.transparent);
        protected PerObjectData fullLightPerObjectFlags = PerObjectData.ReflectionProbes | PerObjectData.Lightmaps | PerObjectData.ShadowMask | PerObjectData.OcclusionProbe |
            PerObjectData.LightProbe /** | PerObjectData.LightProbeProxyVolume **/ | PerObjectData.OcclusionProbeProxyVolume | PerObjectData.None;
        // LightProbeProxyVolume is deprecated in the latest  srp

        protected Material postProcessMat;

        public abstract void Dispose();
    }
}
