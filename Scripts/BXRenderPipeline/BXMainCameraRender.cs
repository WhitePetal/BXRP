using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BXRenderPipeline
{
    public partial class BXMainCameraRender
    {
        public Camera camera;
        public ScriptableRenderContext context;
        public CullingResults cullingResults;

#if UNITY_EDITOR
        private static Material material_error = new Material(Shader.Find("Hidden/InternalErrorShader"));
#endif

        private const string commandBufferName = "BXCommondBufferRender";

#if !UNITY_EDITOR
        private string SampleName = commandBufferName;
#endif

        public CommandBuffer commandBuffer = new CommandBuffer
        {
            name = commandBufferName
        };

        public BXRenderCommonSettings commonSettings;

        public BXLights lights = new BXLights();

        public float maxShadowDistance;
        public int width, height, width_screen, height_screen;

        private FilteringSettings filterSettings_opaue = new FilteringSettings(RenderQueueRange.opaque);
        private FilteringSettings filterSettings_transparent = new FilteringSettings(RenderQueueRange.transparent);
        private PerObjectData fullLightPerObjectFlags = PerObjectData.ReflectionProbes | PerObjectData.Lightmaps | PerObjectData.ShadowMask | PerObjectData.OcclusionProbe |
            PerObjectData.LightProbe | PerObjectData.LightProbeProxyVolume | PerObjectData.OcclusionProbeProxyVolume | PerObjectData.None;

        public void Render(ScriptableRenderContext context, Camera camera, bool useDynamicBatching, bool useGPUInstancing, BXRenderCommonSettings commonSettings,
            List<BXRenderFeature> beforeRenderRenderFeatures, List<BXRenderFeature> onDirShadowsRenderFeatures,
            List<BXRenderFeature> beforeOpaqueRenderFeatures, List<BXRenderFeature> afterOpaqueRenderFeatures,
            List<BXRenderFeature> beforeTransparentRenderFeatures, List<BXRenderFeature> afterTransparentRenderFeatures,
            List<BXRenderFeature> onPostProcessRenderFeatures)
		{
            this.context = context;
            this.camera = camera;
            this.commonSettings = commonSettings;

            width_screen = camera.pixelWidth;
            height_screen = camera.pixelHeight;
#if UNITY_EDITOR
            if(camera.cameraType == CameraType.SceneView)
			{
                height = height_screen;
			}
			else
			{
                height = Mathf.Clamp(Mathf.RoundToInt(height_screen * commonSettings.downSample), commonSettings.minHeight, commonSettings.maxHeight);
			}
#else
            height = Mathf.Clamp(Mathf.RoundToInt(height_screen * commonSettings.downSample), commonSettings.minHeight, commonSettings.maxHeight);
#endif
            width = Mathf.RoundToInt(width_screen * ((float)height / height_screen));

            SetupRenderFeatures(beforeRenderRenderFeatures);
            SetupRenderFeatures(onDirShadowsRenderFeatures);
            SetupRenderFeatures(beforeOpaqueRenderFeatures);
            SetupRenderFeatures(afterOpaqueRenderFeatures);
            SetupRenderFeatures(beforeTransparentRenderFeatures);
            SetupRenderFeatures(afterTransparentRenderFeatures);
            SetupRenderFeatures(onPostProcessRenderFeatures);

#if UNITY_EDITOR
            PreparBuffer();
            PreparForSceneWindow();
#endif

            maxShadowDistance = Mathf.Min(camera.farClipPlane, commonSettings.maxShadowDistance);
            if (!Cull(maxShadowDistance)) return;

            commandBuffer.BeginSample(SampleName);
            lights.Setup(this, onDirShadowsRenderFeatures);
            commandBuffer.EndSample(SampleName);
            ExecuteCommand();

            commandBuffer.BeginSample(SampleName);
            context.SetupCameraProperties(camera);
            ExecuteRenderFeatures(beforeRenderRenderFeatures);
            GenerateGraphicsBuffe();
            DrawGeometry(useDynamicBatching, useGPUInstancing, beforeOpaqueRenderFeatures, afterOpaqueRenderFeatures, beforeTransparentRenderFeatures, afterTransparentRenderFeatures);
#if UNITY_EDITOR
            DrawUnsupportShader();
            DrawGizmosBeforePostProcess();
#endif
            DrawPostProcess(onPostProcessRenderFeatures);

#if UNITY_EDITOR
            DrawGizmosAfterPostProcess();
#endif
            CleanUp();
            Submit();
        }

        private void ExecuteCommand()
		{
            context.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
		}

        private void SetupRenderFeatures(List<BXRenderFeature> renderFeatures)
		{
            for(int i = 0; i < renderFeatures.Count; ++i)
			{
                renderFeatures[i].Setup(this);
			}
		}

        private void ExecuteRenderFeatures(List<BXRenderFeature> renderFeatures)
		{
            for (int i = 0; i < renderFeatures.Count; ++i)
            {
                renderFeatures[i].Render(commandBuffer, this);
            }
        }

        private bool Cull(float maxShadowDistance)
		{
            if(camera.TryGetCullingParameters(out ScriptableCullingParameters p))
			{
                p.shadowDistance = maxShadowDistance;
                cullingResults = context.Cull(ref p);
                return true;
			}
            return false;
		}

        private void GenerateGraphicsBuffe()
		{
            commandBuffer.GetTemporaryRT(BXShaderPropertyIDs._FrameBuffer_ID, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.RGB111110Float, RenderTextureReadWrite.Linear, commonSettings.msaa, false, RenderTextureMemoryless.MSAA);
            commandBuffer.GetTemporaryRT(BXShaderPropertyIDs._DepthBuffer_ID, width, height, 24, FilterMode.Point, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear, commonSettings.msaa, false, RenderTextureMemoryless.Depth);
		}

        private void DrawGeometry(bool useDynamicBatching, bool useGPUInstancing,
            List<BXRenderFeature> beforeOpaqueRenderFeatures, List<BXRenderFeature> afterOpaqueRenderFeatures,
            List<BXRenderFeature> beforeTransparentRenderFeatures, List<BXRenderFeature> afterTransparentRenderFeature)
		{
            commandBuffer.SetRenderTarget(BXShaderPropertyIDs._FrameBuffer_TargetID, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, BXShaderPropertyIDs._DepthBuffer_TargetID, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
            CameraClearFlags clearFlags = camera.clearFlags;
            commandBuffer.ClearRenderTarget(clearFlags <= CameraClearFlags.Depth, clearFlags <= CameraClearFlags.Color, clearFlags == CameraClearFlags.SolidColor ? camera.backgroundColor : Color.clear);
            ExecuteCommand();

            ExecuteRenderFeatures(beforeOpaqueRenderFeatures);

            // Draw Opaque
            SortingSettings sortingSettings = new SortingSettings(camera)
            {
                criteria = SortingCriteria.CommonOpaque
            };
            DrawingSettings opaqueDrawingSettings = new DrawingSettings()
            {
                enableDynamicBatching = useDynamicBatching,
                enableInstancing = useGPUInstancing
            };
            opaqueDrawingSettings.sortingSettings = sortingSettings;
            opaqueDrawingSettings.perObjectData = fullLightPerObjectFlags;
            opaqueDrawingSettings.SetShaderPassName(0, BXRenderPipeline.shaderTagIds[0]);
            opaqueDrawingSettings.SetShaderPassName(1, BXRenderPipeline.shaderTagIds[1]);
            context.DrawRenderers(cullingResults, ref opaqueDrawingSettings, ref filterSettings_opaue);

            ExecuteRenderFeatures(afterOpaqueRenderFeatures);

            // Draw Alpha Test
            DrawingSettings alphaTestDrawSettings = new DrawingSettings()
            {
                enableDynamicBatching = useDynamicBatching,
                enableInstancing = useGPUInstancing
            };
            alphaTestDrawSettings.perObjectData = fullLightPerObjectFlags;
            alphaTestDrawSettings.sortingSettings = sortingSettings;
            alphaTestDrawSettings.SetShaderPassName(0, BXRenderPipeline.shaderTagIds[2]);
            context.DrawRenderers(cullingResults, ref alphaTestDrawSettings, ref filterSettings_opaue);

            // Draw SkyBox
            context.DrawSkybox(camera);

            ExecuteRenderFeatures(beforeTransparentRenderFeatures);

            // Draw Transparent
            DrawingSettings alphaDrawSettings = new DrawingSettings()
            {
                enableDynamicBatching = useDynamicBatching,
                enableInstancing = useGPUInstancing
            };
            alphaDrawSettings.perObjectData = fullLightPerObjectFlags;
            alphaDrawSettings.sortingSettings = sortingSettings;
            alphaDrawSettings.SetShaderPassName(0, BXRenderPipeline.shaderTagIds[0]);
            alphaDrawSettings.SetShaderPassName(1, BXRenderPipeline.shaderTagIds[1]);
            context.DrawRenderers(cullingResults, ref alphaDrawSettings, ref filterSettings_transparent);

            ExecuteRenderFeatures(afterTransparentRenderFeature);

            ExecuteCommand();
        }

        private void DrawPostProcess(List<BXRenderFeature> onPostProcessRenderFeatures)
		{
            ExecuteRenderFeatures(onPostProcessRenderFeatures);
#if UNITY_EDITOR
            if(camera.cameraType == CameraType.SceneView)
			{
                DrawPostProcess(BXShaderPropertyIDs._FrameBuffer_TargetID, BuiltinRenderTextureType.CameraTarget, commonSettings.postProcessMaterial, 0, true, true, width_screen, height_screen);
			}
			else
			{
                DrawPostProcess(BXShaderPropertyIDs._FrameBuffer_TargetID, BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.None, commonSettings.postProcessMaterial, 0, true, true, width_screen, height_screen);
            }
#else
            DrawPostProcess(BXShaderPropertyIDs._FrameBuffer_TargetID, BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.None, commonSettings.postProcessMaterial, 0, true, true, width_screen, height_screen);
#endif
        }

        private void DrawPostProcess(RenderTargetIdentifier source, RenderTargetIdentifier destination, Material mat, int pass, bool clear = false, bool setViewPort = false, int vW = 0, int vH = 0)
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

        private void DrawPostProcess(RenderTargetIdentifier source, RenderTargetIdentifier destination_color, RenderTargetIdentifier destination_depth, Material mat, int pass, bool clear = false, bool setViewPort = false, int vW = 0, int vH = 0)
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

        private void CleanUp()
		{
            lights.CleanUp();
            commandBuffer.ReleaseTemporaryRT(BXShaderPropertyIDs._FrameBuffer_ID);
            commandBuffer.ReleaseTemporaryRT(BXShaderPropertyIDs._DepthBuffer_ID);
		}

        private void Submit()
		{
            commandBuffer.EndSample(SampleName);
            ExecuteCommand();
            context.Submit();
		}

        public void OnDispose()
		{
            camera = null;
#if UNITY_EDITOR
            material_error = null;
#endif
            SampleName = null;
            commandBuffer.Dispose();
            commandBuffer = null;
            commonSettings = null;
            lights.OnDispose();
            lights = null;
		}
    }
}
