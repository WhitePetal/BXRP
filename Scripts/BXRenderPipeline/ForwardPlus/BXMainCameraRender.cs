using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using BXRenderPipeline;

namespace BXRenderPipelineForward
{
    public partial class BXMainCameraRender : BXMainCameraRenderBase
    {
        public BXLights lights = new BXLights();

        public void Init(BXRenderCommonSettings commonSettings)
        {
            this.commonSettings = commonSettings;
            this.postProcessMat = commonSettings.postProcessMaterial;
            lights.Init(commonSettings);
        }

        public void Render(ScriptableRenderContext context, Camera camera, bool useDynamicBatching, bool useGPUInstancing)
		{
            this.context = context;
            this.camera = camera;

            width_screen = Screen.width;
            height_screen = Screen.height;
#if UNITY_EDITOR
            if(camera.cameraType == CameraType.SceneView)
			{
                height = camera.pixelWidth;
                width = camera.pixelWidth;
            }
			else
			{
                height = Mathf.Clamp(Mathf.RoundToInt(height_screen * commonSettings.downSample), commonSettings.minHeight, commonSettings.maxHeight);
                width = Mathf.RoundToInt(width_screen * ((float)height / height_screen));
            }
#else
            height = Mathf.Clamp(Mathf.RoundToInt(height_screen * commonSettings.downSample), commonSettings.minHeight, commonSettings.maxHeight);
#endif
            width = Mathf.RoundToInt(width_screen * ((float)height / height_screen));

            // for ture camera.projectionMatrix
            //Rect originRect = camera.pixelRect;
            //camera.pixelRect = new Rect(originRect.position.x, originRect.position.y, width, height);

            BXVolumeManager.instance.Update(camera.transform, 1 << camera.gameObject.layer);

#if UNITY_EDITOR
            PreparBuffer();
            PreparForSceneWindow();
#endif

            maxShadowDistance = Mathf.Min(camera.farClipPlane, commonSettings.maxShadowDistance);
            if (!Cull(maxShadowDistance)) return;

            commandBuffer.BeginSample(SampleName);
            lights.Setup(this);
            commandBuffer.EndSample(SampleName);
            ExecuteCommand();

            commandBuffer.BeginSample(SampleName);
            context.SetupCameraProperties(camera);
            ExecuteVolumeRender(RenderFeatureStep.BeforeRender);
            GenerateGraphicsBuffe();
            DrawGeometry(useDynamicBatching, useGPUInstancing);
#if UNITY_EDITOR
            DrawUnsupportShader();
            DrawGizmosBeforePostProcess();
#endif
            DrawPostProcess();

#if UNITY_EDITOR
            ExecuteCommand();
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

        private bool ExecuteVolumeRender(RenderFeatureStep step)
		{
            if(BXVolumeManager.instance.Render(step, commandBuffer, this))
			{
                ExecuteCommand();
                return true;
			}
            return false;
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
            commandBuffer.SetGlobalVector("_ScreenParams", new Vector4(width, height, 1f + 1f / width, 1f + 1f / height));
        }

        private void DrawGeometry(bool useDynamicBatching, bool useGPUInstancing)
		{
            commandBuffer.SetRenderTarget(BXShaderPropertyIDs._FrameBuffer_TargetID, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, BXShaderPropertyIDs._DepthBuffer_TargetID, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
            CameraClearFlags clearFlags = camera.clearFlags;
            Color clearColor = (clearFlags == CameraClearFlags.SolidColor) ? camera.backgroundColor : Color.clear;
#if UNITY_EDITOR
            if (camera.cameraType == CameraType.SceneView && RenderSettings.skybox == null)
                clearColor = RenderSettings.ambientSkyColor;
#endif
            commandBuffer.ClearRenderTarget(clearFlags <= CameraClearFlags.Depth, clearFlags <= CameraClearFlags.Color, clearColor);

            ExecuteCommand();

            ExecuteVolumeRender(RenderFeatureStep.BeforeOpaque);

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
            opaqueDrawingSettings.SetShaderPassName(0, BXRenderPipeline.BXRenderPipeline.forwardShaderTagIds[0]);
            opaqueDrawingSettings.SetShaderPassName(1, BXRenderPipeline.BXRenderPipeline.forwardShaderTagIds[1]);
            context.DrawRenderers(cullingResults, ref opaqueDrawingSettings, ref filterSettings_opaue);

            ExecuteVolumeRender(RenderFeatureStep.AfterOpaque);

            // Draw Alpha Test
            DrawingSettings alphaTestDrawSettings = new DrawingSettings()
            {
                enableDynamicBatching = useDynamicBatching,
                enableInstancing = useGPUInstancing
            };
            alphaTestDrawSettings.perObjectData = fullLightPerObjectFlags;
            alphaTestDrawSettings.sortingSettings = sortingSettings;
            alphaTestDrawSettings.SetShaderPassName(0, BXRenderPipeline.BXRenderPipeline.forwardShaderTagIds[2]);
            context.DrawRenderers(cullingResults, ref alphaTestDrawSettings, ref filterSettings_opaue);

            // Draw SkyBox
            context.DrawSkybox(camera);

            ExecuteVolumeRender(RenderFeatureStep.BeforeTransparent);

            // Draw Transparent
            DrawingSettings alphaDrawSettings = new DrawingSettings()
            {
                enableDynamicBatching = useDynamicBatching,
                enableInstancing = useGPUInstancing
            };
            alphaDrawSettings.perObjectData = fullLightPerObjectFlags;
            alphaDrawSettings.sortingSettings = sortingSettings;
            alphaDrawSettings.SetShaderPassName(0, BXRenderPipeline.BXRenderPipeline.forwardShaderTagIds[0]);
            alphaDrawSettings.SetShaderPassName(1, BXRenderPipeline.BXRenderPipeline.forwardShaderTagIds[1]);
            context.DrawRenderers(cullingResults, ref alphaDrawSettings, ref filterSettings_transparent);

            if(!ExecuteVolumeRender(RenderFeatureStep.AfterTransparent))
                ExecuteCommand();
        }

        private void DrawPostProcess()
		{
            ExecuteVolumeRender(RenderFeatureStep.OnPostProcess);

            DrawBloom();

#if UNITY_EDITOR
            if (camera.cameraType == CameraType.SceneView)
			{
                DrawPostProcess(BXShaderPropertyIDs._FrameBuffer_TargetID, BuiltinRenderTextureType.CameraTarget, postProcessMat, 0, true);
			}
			else
			{
                DrawPostProcess(BXShaderPropertyIDs._FrameBuffer_TargetID, BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.None, postProcessMat, 0, true, true, width_screen, height_screen);
            }
#else
            DrawPostProcess(BXShaderPropertyIDs._FrameBuffer_TargetID, BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.None, commonSettings.postProcessMaterial, 0, true, true, width_screen, height_screen);
#endif
            ReleaseBloom();
        }

        private void DrawBloom()
        {
            if (!commonSettings.enablBloom)
            {
                postProcessMat.DisableKeyword("_BLOOM");
                return;
            }
            postProcessMat.EnableKeyword("_BLOOM");

            commandBuffer.GetTemporaryRT(BXShaderPropertyIDs._BloomTempRT_IDs[0], width >> 1, height >> 1, 0, FilterMode.Bilinear, RenderTextureFormat.RGB111110Float);

            commandBuffer.GetTemporaryRT(BXShaderPropertyIDs._BloomTempRT_IDs[1], width >> 2, height >> 2, 0, FilterMode.Bilinear, RenderTextureFormat.RGB111110Float);
            commandBuffer.GetTemporaryRT(BXShaderPropertyIDs._BloomTempRT_IDs[2], width >> 2, height >> 2, 0, FilterMode.Bilinear, RenderTextureFormat.RGB111110Float);

            commandBuffer.GetTemporaryRT(BXShaderPropertyIDs._BloomTempRT_IDs[3], width >> 3, height >> 3, 0, FilterMode.Bilinear, RenderTextureFormat.RGB111110Float);
            commandBuffer.GetTemporaryRT(BXShaderPropertyIDs._BloomTempRT_IDs[4], width >> 3, height >> 3, 0, FilterMode.Bilinear, RenderTextureFormat.RGB111110Float);

            commandBuffer.GetTemporaryRT(BXShaderPropertyIDs._BloomTempRT_IDs[5], width >> 4, height >> 4, 0, FilterMode.Bilinear, RenderTextureFormat.RGB111110Float);
            commandBuffer.GetTemporaryRT(BXShaderPropertyIDs._BloomTempRT_IDs[6], width >> 4, height >> 4, 0, FilterMode.Bilinear, RenderTextureFormat.RGB111110Float);

            var renderSettings = BXVolumeManager.instance.renderSettings.GetComponent<BXBloomComponent>();

            Vector4 threshold;
            threshold.x = renderSettings.threshold_runtime;
            threshold.y = threshold.x * renderSettings.threshold_knne_runtime;
            threshold.z = 2f * threshold.y;
            threshold.w = 0.25f / (threshold.y + 0.0001f);
            threshold.y -= threshold.x;
            commandBuffer.SetGlobalVector(BXShaderPropertyIDs._BloomFilters_ID, threshold);
            commandBuffer.SetGlobalVector(BXShaderPropertyIDs._BloomStrength_ID, new Vector4(renderSettings.bloom_strength_runtime, renderSettings.bright_clamp_runtime));

            DrawPostProcess(BXShaderPropertyIDs._FrameBuffer_TargetID, BXShaderPropertyIDs._BloomTempRT_RTIDs[0], postProcessMat, 1, false);

            DrawPostProcess(BXShaderPropertyIDs._BloomTempRT_RTIDs[0], BXShaderPropertyIDs._BloomTempRT_RTIDs[1], postProcessMat, 2, false);
            DrawPostProcess(BXShaderPropertyIDs._BloomTempRT_RTIDs[1], BXShaderPropertyIDs._BloomTempRT_RTIDs[2], postProcessMat, 3, false);

            DrawPostProcess(BXShaderPropertyIDs._BloomTempRT_RTIDs[2], BXShaderPropertyIDs._BloomTempRT_RTIDs[3], postProcessMat, 2, false);
            DrawPostProcess(BXShaderPropertyIDs._BloomTempRT_RTIDs[3], BXShaderPropertyIDs._BloomTempRT_RTIDs[4], postProcessMat, 3, false);

            DrawPostProcess(BXShaderPropertyIDs._BloomTempRT_RTIDs[4], BXShaderPropertyIDs._BloomTempRT_RTIDs[5], postProcessMat, 2, false);
            DrawPostProcess(BXShaderPropertyIDs._BloomTempRT_RTIDs[5], BXShaderPropertyIDs._BloomTempRT_RTIDs[6], postProcessMat, 3, false);

            commandBuffer.SetGlobalTexture(BXShaderPropertyIDs._BloomTex_ID, BXShaderPropertyIDs._BloomTempRT_RTIDs[4]);
            DrawPostProcess(BXShaderPropertyIDs._BloomTempRT_RTIDs[6], BXShaderPropertyIDs._BloomTempRT_RTIDs[3], postProcessMat, 4, false);
            commandBuffer.SetGlobalTexture(BXShaderPropertyIDs._BloomTex_ID, BXShaderPropertyIDs._BloomTempRT_RTIDs[2]);
            DrawPostProcess(BXShaderPropertyIDs._BloomTempRT_RTIDs[3], BXShaderPropertyIDs._BloomTempRT_RTIDs[1], postProcessMat, 4, false);
            commandBuffer.SetGlobalTexture(BXShaderPropertyIDs._BloomTex_ID, BXShaderPropertyIDs._BloomTempRT_RTIDs[1]);
        }

        public void ReleaseBloom()
        {
            if (!commonSettings.enablBloom) return;
            for (int i = 0; i < BXShaderPropertyIDs._BloomTempRT_IDs.Length; ++i)
            {
                commandBuffer.ReleaseTemporaryRT(BXShaderPropertyIDs._BloomTempRT_IDs[i]);
            }
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

        public override void Dispose()
		{
            camera = null;
#if UNITY_EDITOR
            material_error = null;
#endif
            postProcessMat = null;
            SampleName = null;
            commandBuffer.Dispose();
            commandBuffer = null;
            commonSettings = null;
            lights.Dispose();
            lights = null;
		}
    }
}
