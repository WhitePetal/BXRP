using System;
using System.Collections;
using System.Collections.Generic;
using BXRenderPipelineForward;
using UnityEngine;
using UnityEngine.Rendering;
using BXRenderPipeline;

namespace BXRenderPipelineForward
{
    public partial class BXOtherCameraRender : BXMainCameraRenderBase
    {
        public BXLights lights = new BXLights();

        private List<BXRenderFeature> onDirShadowsRenderFeatures = new List<BXRenderFeature>();

        public void Init(BXRenderCommonSettings commonSettings)
        {
            this.commonSettings = commonSettings;
            this.postProcessMat = commonSettings.postProcessMaterial;
            lights.Init(commonSettings);
        }

        public void Render(ScriptableRenderContext context, Camera camera, bool useDynamicBatching, bool useGPUInstancing, BXRenderCommonSettings commonSettings)
        {
            this.context = context;
            this.camera = camera;

            width_screen = camera.pixelWidth;
            height_screen = camera.pixelHeight;
#if UNITY_EDITOR
            if (camera.cameraType == CameraType.SceneView)
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

            //BXVolumeManager.instance.Update(camera.transform, 1 << camera.gameObject.layer);

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
            //GenerateGraphicsBuffe();
            commandBuffer.ClearRenderTarget(true, true, Color.clear);
            ExecuteCommand();
            DrawGeometry(useDynamicBatching, useGPUInstancing);
#if UNITY_EDITOR
            DrawUnsupportShader();
            DrawGizmosBeforePostProcess();
#endif
            //DrawPostProcess(onPostProcessRenderFeatures);

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

        private bool Cull(float maxShadowDistance)
        {
            if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
            {
                p.shadowDistance = maxShadowDistance;
                cullingResults = context.Cull(ref p);
                return true;
            }
            return false;
        }

        private void DrawGeometry(bool useDynamicBatching, bool useGPUInstancing)
        {
            //commandBuffer.SetRenderTarget(BXShaderPropertyIDs._FrameBuffer_TargetID, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, BXShaderPropertyIDs._DepthBuffer_TargetID, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
            //CameraClearFlags clearFlags = camera.clearFlags;
            //commandBuffer.ClearRenderTarget(clearFlags <= CameraClearFlags.Depth, clearFlags <= CameraClearFlags.Color, clearFlags == CameraClearFlags.SolidColor ? camera.backgroundColor : Color.clear);

            var renderSettings = BXVolumeManager.instance.renderSettings;
            var expourseComponent = renderSettings.GetComponent<BXExpourseComponent>();

            // EV-Expourse
            commandBuffer.SetGlobalFloat(BXShaderPropertyIDs._ReleateExpourse_ID, expourseComponent.expourseRuntime / renderSettings.standard_expourse);

            ExecuteCommand();

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

            ExecuteCommand();
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
            onDirShadowsRenderFeatures = null;
        }
    }
}