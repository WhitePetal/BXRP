using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BXRenderPipeline;
using UnityEngine.Rendering;
using Unity.Collections;

namespace BXRenderPipelineDeferred
{
    public partial class BXOtherCameraRenderDeferred : BXMainCameraRenderBase
    {
        private static readonly int _AlebodRoughnessRT_ID = Shader.PropertyToID("_AlebodRoughnessRT");
        private static readonly RenderTargetIdentifier _AlebodRoughnessRT_TargetID = new RenderTargetIdentifier(_AlebodRoughnessRT_ID);
        private static readonly int _NormalMetallicMaskRT_ID = Shader.PropertyToID("_NormalMetallicMaskRT");
        private static readonly RenderTargetIdentifier _NormalMetallicMaskRT_TargetID = new RenderTargetIdentifier(_NormalMetallicMaskRT_ID);
        private static readonly int _EncodeDepthRT_ID = Shader.PropertyToID("_EncodeDepthRT");
        private static readonly RenderTargetIdentifier _EncodeDepthRT_TargetID = new RenderTargetIdentifier(_EncodeDepthRT_ID);
        private static readonly int _LightingRT_ID = Shader.PropertyToID("_LightingRT");
        private static readonly RenderTargetIdentifier _LightingRT_TargetID = new RenderTargetIdentifier(_LightingRT_ID);
        private static readonly int _TempDepth_ID = Shader.PropertyToID("_TempDepth");
        private static readonly RenderTargetIdentifier _TempDepth_TargetID = new RenderTargetIdentifier(_TempDepth_ID);

        public BXLightsDeferred lights = new BXLightsDeferred();

        private GlobalKeyword framebufferfetch_msaa = GlobalKeyword.Create("FRAMEBUFFERFETCH_MSAA");

        private Material[] stencilLightMats = new Material[BXLightsDeferred.maxStencilLightCount];
        private List<BXRenderFeature> onDirShadowsRenderFeatures = new List<BXRenderFeature>();
        private RenderTargetIdentifier[] mrt = new RenderTargetIdentifier[4];
        private RenderBufferLoadAction[] loadActions = new RenderBufferLoadAction[] { RenderBufferLoadAction.DontCare, RenderBufferLoadAction.DontCare, RenderBufferLoadAction.DontCare, RenderBufferLoadAction.DontCare };
        private RenderBufferStoreAction[] storeActions = new RenderBufferStoreAction[] { RenderBufferStoreAction.Store, RenderBufferStoreAction.Store, RenderBufferStoreAction.Store, RenderBufferStoreAction.Store };

        private Material deferredMaterial = new Material(Shader.Find("Hidden/DeferredShadingEditor"));

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

        public void Render(ScriptableRenderContext context, Camera camera, bool useDynamicBatching, bool useGPUInstancing)
        {
            this.context = context;
            this.camera = camera;

            width_screen = camera.pixelWidth;
            height_screen = camera.pixelHeight;
            width = width_screen;
            height = height_screen;

            //BXHiZManager.instance.BeforeSRPCull(this);
            BXVolumeManager.instance.Update(camera.transform, 1 << camera.gameObject.layer);

#if UNITY_EDITOR
            PreparBuffer();
            PreparForSceneWindow();
#endif

            maxShadowDistance = Mathf.Min(camera.farClipPlane, commonSettings.maxShadowDistance);
            if (!Cull(maxShadowDistance)) return;
            //BXHiZManager.instance.AfterSRPCull();

            worldToViewMatrix = camera.worldToCameraMatrix;
            viewToWorldMatrix = camera.cameraToWorldMatrix;

            commandBuffer.BeginSample(SampleName);
            lights.Setup(this, onDirShadowsRenderFeatures);
            commandBuffer.EndSample(SampleName);
            ExecuteCommand();

            commandBuffer.BeginSample(SampleName);
            context.SetupCameraProperties(camera);
            GenerateGraphicsBuffe();
            DrawGeometry(useDynamicBatching, useGPUInstancing);

            //BXHiZManager.instance.AfterSRPRender(commandBuffer);

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

        private void GenerateGraphicsBuffe()
        {
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

        private void DrawGeometry(bool useDynamicBatching, bool useGPUInstancing)
        {
            bool isPreview = camera.cameraType == CameraType.Preview;
            RenderTextureDescriptor lightingDesc = new RenderTextureDescriptor
            {
                width = width,
                height = height,
                dimension = TextureDimension.Tex2D,
                depthBufferBits = 0,
                graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.B10G11R11_UFloatPack32,
                msaaSamples = 1,
                enableRandomWrite = false,
                sRGB = false,
                memoryless = RenderTextureMemoryless.Depth
            };
            RenderTextureDescriptor colorDesc = lightingDesc;
            colorDesc.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm;

            commandBuffer.GetTemporaryRT(_LightingRT_ID, lightingDesc, FilterMode.Bilinear);
            commandBuffer.GetTemporaryRT(_AlebodRoughnessRT_ID, colorDesc, FilterMode.Bilinear);
            commandBuffer.GetTemporaryRT(_NormalMetallicMaskRT_ID, colorDesc, FilterMode.Bilinear);
            commandBuffer.GetTemporaryRT(_EncodeDepthRT_ID, colorDesc, FilterMode.Bilinear);
            commandBuffer.GetTemporaryRT(_TempDepth_ID, width, height, 32, FilterMode.Point, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.Color);

            mrt[0] = isPreview ? _LightingRT_ID : BuiltinRenderTextureType.CameraTarget;
            mrt[1] = _AlebodRoughnessRT_TargetID;
            mrt[2] = _NormalMetallicMaskRT_TargetID;
            mrt[3] = _EncodeDepthRT_TargetID;

            RenderTargetBinding binding;
            binding = new RenderTargetBinding(mrt, loadActions, storeActions, _TempDepth_TargetID, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            commandBuffer.SetRenderTarget(binding);
            commandBuffer.ClearRenderTarget(true, true, Color.clear);
            ExecuteCommand();

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

            commandBuffer.SetRenderTarget(isPreview ? _LightingRT_TargetID : BuiltinRenderTextureType.CameraTarget, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, _TempDepth_TargetID, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
            commandBuffer.ClearRenderTarget(false, false, Color.clear);

            commandBuffer.SetGlobalTexture(_AlebodRoughnessRT_ID, _AlebodRoughnessRT_TargetID);
            commandBuffer.SetGlobalTexture(_NormalMetallicMaskRT_ID, _NormalMetallicMaskRT_TargetID);
            commandBuffer.SetGlobalTexture(_EncodeDepthRT_ID, _EncodeDepthRT_TargetID);

            // baked not render realtime light
            if(camera.cameraType != CameraType.Reflection)
            {
                // Deferred Base Lighting: Directional Lighting + Indirect Lighting
                if (lights.dirLightCount > 0)
                {
                    commandBuffer.DrawProcedural(Matrix4x4.identity, deferredMaterial, 0, MeshTopology.Triangles, 6);
                }

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
            }
            ExecuteCommand();

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
            alphaDrawSettings.SetShaderPassName(0, BXRenderPipeline.BXRenderPipeline.deferredShaderTagIds[0]);
            alphaDrawSettings.SetShaderPassName(1, BXRenderPipeline.BXRenderPipeline.deferredShaderTagIds[1]);
            context.DrawRenderers(cullingResults, ref alphaDrawSettings, ref filterSettings_transparent);

#if UNITY_EDITOR
            DrawUnsupportShader();
            DrawGizmosBeforePostProcess();
#endif
            //context.SubmitForRenderPassValidation();
            if (isPreview)
            {
                //Debug.Log("preview name: " + camera.targetTexture.);
                commandBuffer.SetGlobalFloat("_PrieviewFlip", camera.name == "Preview Camera" ? -1 : 1);
                DrawPostProcess();
            }

#if UNITY_EDITOR
            DrawGizmosAfterPostProcess();
#endif
        }

        private void DrawPostProcess()
        {
#if UNITY_EDITOR
            if (camera.cameraType == CameraType.SceneView)
            {
                DrawPostProcess(_LightingRT_TargetID, BuiltinRenderTextureType.CameraTarget, postProcessMat, 6, true, true, width_screen, height_screen);
            }
            else
            {
                DrawPostProcess(_LightingRT_TargetID, BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.None, postProcessMat, 6, true, true, width_screen, height_screen);
            }
#else
            DrawPostProcess(BXShaderPropertyIDs._FrameBuffer_TargetID, BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.None, commonSettings.postProcessMaterial, 0, true, true, width_screen, height_screen);
#endif
            ExecuteCommand();
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
            commandBuffer.ReleaseTemporaryRT(_LightingRT_ID);
            commandBuffer.ReleaseTemporaryRT(_AlebodRoughnessRT_ID);
            commandBuffer.ReleaseTemporaryRT(_NormalMetallicMaskRT_ID);
            commandBuffer.ReleaseTemporaryRT(_EncodeDepthRT_ID);
            commandBuffer.ReleaseTemporaryRT(_TempDepth_ID);
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
            stencilLightMats = null;
            onDirShadowsRenderFeatures = null;
        }
    }
}
