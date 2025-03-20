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

        public RenderTargetIdentifier postProcessInputTarget;
        protected List<int> needReleasePostProcessTempRTs = new List<int>();

        public void RegisterNeedReleasePostProcessTempRT(int rtID)
		{
            needReleasePostProcessTempRTs.Add(rtID);
        }

        public void RelasePostProcessTempRTs()
		{
            for(int i = 0; i < needReleasePostProcessTempRTs.Count; ++i)
			{
                commandBuffer.ReleaseTemporaryRT(needReleasePostProcessTempRTs[i]);
			}
		}

        public void DrawPostProcess(RenderTargetIdentifier source, RenderTargetIdentifier destination, Material mat, int pass, bool clear = false, bool setViewPort = false, int vW = 0, int vH = 0)
        {
            commandBuffer.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            if (setViewPort)
            {
                commandBuffer.SetViewport(new Rect(0, 0, vW, vH));
            }
            commandBuffer.SetGlobalTexture(BXShaderPropertyIDs._PostProcessInput_ID, source);
            if (clear)
            {
                commandBuffer.ClearRenderTarget(false, true, Color.clear);
            }
            commandBuffer.DrawProcedural(Matrix4x4.identity, mat, pass, MeshTopology.Triangles, 3);
        }

        public void DrawPostProcess(RenderTargetIdentifier source, RenderTargetIdentifier destination_color, RenderTargetIdentifier destination_depth, Material mat, int pass, bool clear = false, bool setViewPort = false, int vW = 0, int vH = 0)
        {
            commandBuffer.SetRenderTarget(destination_color, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, destination_depth, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
            if (setViewPort)
            {
                commandBuffer.SetViewport(new Rect(0, 0, vW, vH));
            }
            commandBuffer.SetGlobalTexture(BXShaderPropertyIDs._PostProcessInput_ID, source);
            if (clear)
            {
                commandBuffer.ClearRenderTarget(false, true, Color.clear);
            }
            commandBuffer.DrawProcedural(Matrix4x4.identity, mat, pass, MeshTopology.Triangles, 3);
        }

        public abstract void Dispose();
    }
}
