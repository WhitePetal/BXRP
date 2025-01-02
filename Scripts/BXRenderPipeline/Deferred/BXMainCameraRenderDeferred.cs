using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using BXRenderPipeline;
namespace BXRenderPipelineDeferred
{
    public partial class BXMainCameraRenderDeferred : BXMainCameraRenderBase
    {
        public BXLightsDeferred lights = new BXLightsDeferred();

        private GlobalKeyword framebufferfetch_msaa = GlobalKeyword.Create("FRAMEBUFFERFETCH_MSAA");

        private Material[] stencilLightMats = new Material[BXLightsDeferred.maxStencilLightCount];

        public void Init(BXRenderCommonSettings commonSettings)
        {
            this.commonSettings = commonSettings;
            this.postProcessMat = commonSettings.postProcessMaterial;
            for (int i = 0; i < BXLightsDeferred.maxStencilLightCount; ++i)
            {
                stencilLightMats[i] = new Material(commonSettings.deferredOtherLightMaterial);
                stencilLightMats[i].hideFlags = HideFlags.DontSave;
                GameObject.DontDestroyOnLoad(stencilLightMats[i]);
            }
        }

        public void Render(ScriptableRenderContext context, Camera camera, bool useDynamicBatching, bool useGPUInstancing,
            List<BXRenderFeature> beforeRenderRenderFeatures, List<BXRenderFeature> onDirShadowsRenderFeatures,
            List<BXRenderFeature> beforeOpaqueRenderFeatures, List<BXRenderFeature> afterOpaqueRenderFeatures,
            List<BXRenderFeature> beforeTransparentRenderFeatures, List<BXRenderFeature> afterTransparentRenderFeatures,
            List<BXRenderFeature> onPostProcessRenderFeatures)
		{
            this.context = context;
            this.camera = camera;

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

            BXHiZManager.instance.BeforeSRPCull(this);
            BXVolumeManager.instance.Update(camera.transform, 1 << camera.gameObject.layer);

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
            BXHiZManager.instance.AfterSRPCull();

            worldToViewMatrix = camera.worldToCameraMatrix;
            viewToWorldMatrix = camera.cameraToWorldMatrix;

            commandBuffer.BeginSample(SampleName);
            lights.Setup(this, onDirShadowsRenderFeatures);
            commandBuffer.EndSample(SampleName);
            ExecuteCommand();

            commandBuffer.BeginSample(SampleName);
            context.SetupCameraProperties(camera);
            ExecuteRenderFeatures(beforeRenderRenderFeatures);
            GenerateGraphicsBuffe();
            DrawGeometry(useDynamicBatching, useGPUInstancing, beforeOpaqueRenderFeatures, afterOpaqueRenderFeatures, beforeTransparentRenderFeatures, afterTransparentRenderFeatures, onPostProcessRenderFeatures);

            BXHiZManager.instance.AfterSRPRender(commandBuffer);

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
            if(renderFeatures != null && renderFeatures.Count > 0)
            {
                for (int i = 0; i < renderFeatures.Count; ++i)
                {
                    renderFeatures[i].Render(commandBuffer, this);
                }
                ExecuteCommand();
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
            commandBuffer.SetKeyword(framebufferfetch_msaa, commonSettings.msaa > 1);
            commandBuffer.GetTemporaryRT(BXShaderPropertyIDs._FrameBuffer_ID, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.RGB111110Float, RenderTextureReadWrite.Linear, commonSettings.msaa, false, RenderTextureMemoryless.MSAA, false);
            commandBuffer.GetTemporaryRT(BXShaderPropertyIDs._EncodeDepthBuffer_ID, width, height, 0, FilterMode.Point, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm, commonSettings.msaa, false, RenderTextureMemoryless.MSAA, false);

            var renderSettings = BXVolumeManager.instance.renderSettings;
            var expourseComponent = renderSettings.GetComponent<BXExpourseComponent>();

            // EV-Expourse
            commandBuffer.SetGlobalFloat(BXShaderPropertyIDs._ReleateExpourse_ID, expourseComponent.expourseRuntime / renderSettings.standard_expourse);

            float aspec = camera.aspect;
            float h_half;
            if (camera.orthographic || camera.fieldOfView == 0f)
            {
                h_half = camera.orthographicSize;
            }
            else
            {
                float fov = camera.fieldOfView;
                h_half = Mathf.Tan(0.5f * fov * Mathf.Deg2Rad);
            }
            float w_half = h_half * aspec;

            Matrix4x4 viewPortRays = Matrix4x4.identity;
            Vector4 forward = new Vector4(0f, 0f, -1f);
            Vector4 up = new Vector4(0f, h_half, 0f);
            Vector4 right = new Vector4(w_half, 0f, 0f);

            Vector4 lb = forward - right - up;
            Vector4 lu = forward - right + up;
            Vector4 rb = forward + right - up;
            Vector4 ru = forward + right + up;
            viewPortRays.SetRow(0, lb);
            viewPortRays.SetRow(1, lu);
            viewPortRays.SetRow(2, rb);
            viewPortRays.SetRow(3, ru);

            commandBuffer.SetGlobalMatrix(BXShaderPropertyIDs._ViewPortRaysID, viewPortRays);

            ExecuteCommand();
        }

        private void DrawGeometry(bool useDynamicBatching, bool useGPUInstancing,
            List<BXRenderFeature> beforeOpaqueRenderFeatures, List<BXRenderFeature> afterOpaqueRenderFeatures,
            List<BXRenderFeature> beforeTransparentRenderFeatures, List<BXRenderFeature> afterTransparentRenderFeature, List<BXRenderFeature> onPostProcessRenderFeatures)
		{
            var lighting = new AttachmentDescriptor(UnityEngine.Experimental.Rendering.GraphicsFormat.B10G11R11_UFloatPack32);
            var albeod_roughness = new AttachmentDescriptor(UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm);
            var normal_metallic_mask = new AttachmentDescriptor(UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm);
            var depth = new AttachmentDescriptor(UnityEngine.Experimental.Rendering.GraphicsFormat.D24_UNorm_S8_UInt);
            // for metal can't framefetch depth buffer, so Encode Depth to a external attachment
//#if UNITY_STANDALONE_OSX || UNITY_IOS || UNITY_EDITOR_OSX
            var depth_metal = new AttachmentDescriptor(UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm);
//#endif
            albeod_roughness.ConfigureClear(Color.clear);
			normal_metallic_mask.ConfigureClear(Color.clear);
            lighting.ConfigureClear(Color.clear);
            depth.ConfigureClear(Color.clear, 1f, 0);
//#if UNITY_STANDALONE_OSX || UNITY_IOS || UNITY_EDITOR_OSX
            depth_metal.ConfigureClear(Color.clear);
            depth_metal.ConfigureTarget(BXShaderPropertyIDs._EncodeDepthBuffer_TargetID, false, true);
            if (commonSettings.msaa > 1)
            {
                depth_metal.ConfigureResolveTarget(BXShaderPropertyIDs._EncodeDepthBuffer_TargetID);

            }
            depth_metal.loadAction = RenderBufferLoadAction.DontCare;
//#endif

            lighting.ConfigureTarget(BXShaderPropertyIDs._FrameBuffer_TargetID, false, true);
            if(commonSettings.msaa > 1)
            {
                lighting.ConfigureResolveTarget(BXShaderPropertyIDs._FrameBuffer_TargetID);
                
            }
            // loadExisitingContents = false may be useless in subpass if not do this
            lighting.loadAction = RenderBufferLoadAction.DontCare;

//#if UNITY_STANDALONE_OSX || UNITY_IOS || UNITY_EDITOR_OSX
            var attachments = new NativeArray<AttachmentDescriptor>(5, Allocator.Temp);
//#else
            //var attachments = new NativeArray<AttachmentDescriptor>(4, Allocator.Temp);
//#endif
            const int depthIndex = 0, lightingIndex = 1, albedo_roughnessIndex = 2, normal_metallic_maskIndex = 3;
//#if UNITY_STANDALONE_OSX || UNITY_IOS || UNITY_EDITOR_OSX
            const int depth_metalIndex = 4;
//#endif
            attachments[depthIndex] = depth;
            attachments[lightingIndex] = lighting;
            attachments[albedo_roughnessIndex] = albeod_roughness;
            attachments[normal_metallic_maskIndex] = normal_metallic_mask;
//#if UNITY_STANDALONE_OSX || UNITY_IOS || UNITY_EDITOR_OSX
            attachments[depth_metalIndex] = depth_metal;
//#endif
            context.BeginRenderPass(width, height, 1, commonSettings.msaa, attachments, depthIndex);
            attachments.Dispose();

            // RenderGbuffer Sub Pass
//#if UNITY_STANDALONE_OSX || UNITY_IOS || UNITY_EDITOR_OSX
            var gbuffers = new NativeArray<int>(4, Allocator.Temp);
//#else
            //var gbuffers = new NativeArray<int>(3, Allocator.Temp);
//#endif
            gbuffers[0] = lightingIndex;
            gbuffers[1] = albedo_roughnessIndex;
            gbuffers[2] = normal_metallic_maskIndex;
//#if UNITY_STANDALONE_OSX || UNITY_IOS || UNITY_EDITOR_OSX
            gbuffers[3] = depth_metalIndex;
//#endif
            context.BeginSubPass(gbuffers);
            gbuffers.Dispose();

            ExecuteRenderFeatures(beforeOpaqueRenderFeatures);

			// Draw Opaque GBuffers
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
            opaqueDrawingSettings.SetShaderPassName(0, BXRenderPipeline.BXRenderPipeline.deferredShaderTagIds[0]);
            opaqueDrawingSettings.SetShaderPassName(1, BXRenderPipeline.BXRenderPipeline.deferredShaderTagIds[1]);
            context.DrawRenderers(cullingResults, ref opaqueDrawingSettings, ref filterSettings_opaue);
            var geoRenderers = GameObject.FindObjectsByType<BXGeometryGraph.Runtime.GeometryRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if(geoRenderers != null && geoRenderers.Length > 0)
            {
                for(int i = 0; i < geoRenderers.Length; ++i)
                {
                    geoRenderers[i].Render(commandBuffer);
                }
                ExecuteCommand();
            }

            ExecuteRenderFeatures(afterOpaqueRenderFeatures);

            // Draw Alpha Test GBuffers
            DrawingSettings alphaTestDrawSettings = new DrawingSettings()
            {
                enableDynamicBatching = useDynamicBatching,
                enableInstancing = useGPUInstancing
            };
            alphaTestDrawSettings.perObjectData = fullLightPerObjectFlags;
            alphaTestDrawSettings.sortingSettings = sortingSettings;
            alphaTestDrawSettings.SetShaderPassName(0, BXRenderPipeline.BXRenderPipeline.deferredShaderTagIds[2]);
            context.DrawRenderers(cullingResults, ref alphaTestDrawSettings, ref filterSettings_opaue);

            context.EndSubPass();

            // Render Lighting Sub Pass
            var lightingBuffers = new NativeArray<int>(1, Allocator.Temp);
            lightingBuffers[0] = lightingIndex;
            var lightingInputs = new NativeArray<int>(3, Allocator.Temp);
            lightingInputs[0] = albedo_roughnessIndex;
            lightingInputs[1] = normal_metallic_maskIndex;
//#if UNITY_STANDALONE_OSX || UNITY_IOS || UNITY_EDITOR_OSX
            lightingInputs[2] = depth_metalIndex;
            //#else
            //lightingInputs[2] = depthIndex;
            //#endif
//#if UNITY_STANDALONE_OSX || UNITY_IOS || UNITY_EDITOR_OSX
            context.BeginSubPass(lightingBuffers, lightingInputs);
//#else
            //context.BeginSubPass(lightingBuffers, lightingInputs, true, false);
//#endif
            lightingBuffers.Dispose();
            lightingInputs.Dispose();

			// Deferred Base Lighting: Directional Lighting + Indirect Lighting
            if(lights.dirLightCount > 0)
            {
                commandBuffer.DrawProcedural(Matrix4x4.identity, commonSettings.deferredMaterial, 0, MeshTopology.Triangles, 6);
            }

            Material deferredMaterial = commonSettings.deferredMaterial;
            // Deferred Other Light Lighting
            for (int i = 0; i < lights.stencilLightCount; ++i)
			{
                Material otherLightMat = stencilLightMats[i];
                commandBuffer.SetGlobalInteger(BXShaderPropertyIDs._OtherLightIndex_ID, i);
                var visibleLight = lights.otherLights[i];
                switch (visibleLight.lightType)
                {
                    case LightType.Point:
                        {
                            float range = visibleLight.range;
                            Vector3 lightSphere = lights.otherLightSpheres[i];
                            Vector3 lightPos = visibleLight.light.transform.position;
                            Matrix4x4 localToWorld = Matrix4x4.TRS(lightPos, Quaternion.identity, Vector3.one * range);
                            if (Vector3.SqrMagnitude(lightSphere) <= (range * range - camera.nearClipPlane))
                            {
                                otherLightMat.SetInt(BXShaderPropertyIDs._StencilComp_ID, (int)CompareFunction.Always);
                                otherLightMat.SetInt(BXShaderPropertyIDs._StencilOp_ID, (int)StencilOp.Replace);
                                commandBuffer.DrawMesh(commonSettings.pointLightMesh, localToWorld, otherLightMat, 0, 1);
                            }
                            else
                            {
                                otherLightMat.SetInt(BXShaderPropertyIDs._StencilComp_ID, (int)CompareFunction.Equal);
                                otherLightMat.SetInt(BXShaderPropertyIDs._StencilOp_ID, (int)StencilOp.Keep);
                                commandBuffer.DrawMesh(commonSettings.pointLightMesh, localToWorld, otherLightMat, 0, 0);
                                commandBuffer.DrawMesh(commonSettings.pointLightMesh, localToWorld, otherLightMat, 0, 1);
                            }
                            commandBuffer.DrawProcedural(Matrix4x4.identity, deferredMaterial, 1, MeshTopology.Triangles, 6);
                        }
                        break;
                    case LightType.Spot:
                        {
                            float range = visibleLight.range;
                            Vector3 lightSphere = lights.otherLightSpheres[i];
                            float angleRangeInv = lights.otherLightThresholds[i].z;
                            Vector3 lightPos = visibleLight.light.transform.position;
                            Vector3 toLight = (camera.transform.position - lightPos).normalized;
                            float cosAngle = Vector3.Dot(toLight, visibleLight.light.transform.forward);
                            float angleDst = (1 - cosAngle) * angleRangeInv;
                            Vector3 scale = Vector3.one * range;
                            scale.x *= visibleLight.spotAngle / 30f;
                            scale.y *= visibleLight.spotAngle / 30f;
                            Matrix4x4 localToWorld = Matrix4x4.TRS(lightPos, visibleLight.light.transform.rotation, scale);
                            if (Vector3.SqrMagnitude(lightSphere) <= (range * range - camera.nearClipPlane) && angleDst < 1f)
                            {
                                otherLightMat.SetInt(BXShaderPropertyIDs._StencilComp_ID, (int)CompareFunction.Always);
                                otherLightMat.SetInt(BXShaderPropertyIDs._StencilOp_ID, (int)StencilOp.Replace);
                                commandBuffer.DrawMesh(commonSettings.spotLightMesh, localToWorld, otherLightMat, 0, 1);
                            }
                            else
                            {
                                otherLightMat.SetInt(BXShaderPropertyIDs._StencilComp_ID, (int)CompareFunction.Equal);
                                otherLightMat.SetInt(BXShaderPropertyIDs._StencilOp_ID, (int)StencilOp.Keep);
                                commandBuffer.DrawMesh(commonSettings.spotLightMesh, localToWorld, otherLightMat, 0, 0);
                                commandBuffer.DrawMesh(commonSettings.spotLightMesh, localToWorld, otherLightMat, 0, 1);
                            }
                            commandBuffer.DrawProcedural(Matrix4x4.identity, deferredMaterial, 1, MeshTopology.Triangles, 6);
                        }
                        break;
                }
			}
            ExecuteCommand();
            
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
            alphaDrawSettings.SetShaderPassName(0, BXRenderPipeline.BXRenderPipeline.deferredShaderTagIds[0]);
            alphaDrawSettings.SetShaderPassName(1, BXRenderPipeline.BXRenderPipeline.deferredShaderTagIds[1]);
            context.DrawRenderers(cullingResults, ref alphaDrawSettings, ref filterSettings_transparent);

            ExecuteRenderFeatures(afterTransparentRenderFeature);

#if UNITY_EDITOR
            DrawUnsupportShader();
            DrawGizmosBeforePostProcess();
#endif

            context.EndSubPass();

            context.EndRenderPass();
            //context.SubmitForRenderPassValidation();

			DrawPostProcess(onPostProcessRenderFeatures);

#if UNITY_EDITOR
            DrawGizmosAfterPostProcess();
#endif
        }

        private void DrawPostProcess(List<BXRenderFeature> onPostProcessRenderFeatures)
		{
            ExecuteRenderFeatures(onPostProcessRenderFeatures);

            DrawBloom();

#if UNITY_EDITOR
            if (camera.cameraType == CameraType.SceneView)
			{
                DrawPostProcess(BXShaderPropertyIDs._FrameBuffer_TargetID, BuiltinRenderTextureType.CameraTarget, postProcessMat, 0, true, true, width_screen, height_screen);
			}
			else
			{
                DrawPostProcess(BXShaderPropertyIDs._FrameBuffer_TargetID, BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.None, postProcessMat, 0, true, true, width_screen, height_screen);
            }
#else
            DrawPostProcess(BXShaderPropertyIDs._FrameBuffer_TargetID, BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.None, commonSettings.postProcessMaterial, 0, true, true, width_screen, height_screen);
#endif
            ReleaseBloom();
            ExecuteCommand();
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
            commandBuffer.ReleaseTemporaryRT(BXShaderPropertyIDs._EncodeDepthBuffer_ID);
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
            for (int i = 0; i < BXLightsDeferred.maxStencilLightCount; ++i)
            {
                GameObject.DestroyImmediate(stencilLightMats[i]);
            }
            stencilLightMats = null;
        }
    }
}
