using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using BXRenderPipeline;

namespace BXRenderPipelineForward
{
    public class BXLights : BXLightsBase
    {
        private Camera camera;
        private BXRenderCommonSettings commonSettings;
        private ScriptableRenderContext context;
        private CullingResults cullingResults;

        private const string BufferName = "Lights";
        private CommandBuffer commandBuffer = new CommandBuffer()
        {
            name = BufferName
        };

        private int width, height;

        private BXShadows shadows = new BXShadows();
        private BXClusterLightCullCompute clusterLightCullCompute = new BXClusterLightCullCompute();
        private BXLightCookie lightCookie = new BXLightCookie();

        private GlobalKeyword dirLightKeyword = GlobalKeyword.Create("DIRECTIONAL_LIGHT");
        private GlobalKeyword clusterLightKeyword = GlobalKeyword.Create("CLUSTER_LIGHT");

        private bool useShadowMask;

        public BXLights() : base(maxClusterLightCount, maxClusterLightCount)
        {
            
        }

        private void CollectLightDatas()
		{
            NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
            dirLightCount = 0;
            clusterLightCount = 0;
            useShadowMask = false;
            for(int visbileLightIndex = 0; visbileLightIndex < visibleLights.Length; ++visbileLightIndex)
			{
                if (dirLightCount >= maxDirLightCount && clusterLightCount >= maxClusterLightCount) break;
                ref var visibleLight = ref visibleLights.UnsafeElementAtMutable(visbileLightIndex);
                LightBakingOutput lightBaking = visibleLight.light.bakingOutput;
                if(lightBaking.lightmapBakeType == LightmapBakeType.Mixed && lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask)
				{
                    useShadowMask = true;
				}
                if (lightBaking.lightmapBakeType == LightmapBakeType.Baked) continue;
				switch (visibleLight.lightType)
				{
                    case LightType.Directional:
                        if(dirLightCount < maxDirLightCount)
						{
                            SetupDirectionalLight(dirLightCount++, visbileLightIndex, ref visibleLight);
						}
                        break;
                    case LightType.Point:
                        if(clusterLightCount < maxClusterLightCount)
						{
                            SetupPointLight(clusterLightCount++, visbileLightIndex, ref visibleLight);
						}
                        break;
                    case LightType.Spot:
                        if(clusterLightCount < maxClusterLightCount)
						{
                            SetupSpotLight(clusterLightCount++, visbileLightIndex, ref visibleLight);
                        }
                        break;
				}
			}
		}

        public void Setup(BXMainCameraRenderBase mainCameraRender, List<BXRenderFeature> onDirShadowsRenderFeatures)
		{
            this.camera = mainCameraRender.camera;
            this.context = mainCameraRender.context;
            this.cullingResults = mainCameraRender.cullingResults;
            this.commonSettings = mainCameraRender.commonSettings;
            this.width = mainCameraRender.width;
            this.height = mainCameraRender.height;
            shadows.Setup(mainCameraRender);
            commandBuffer.BeginSample(BufferName);
            SetupLights();
            lightCookie.Setup(commandBuffer, this, mainCameraRender);
            shadows.Render(useShadowMask, onDirShadowsRenderFeatures);
            commandBuffer.EndSample(BufferName);
            ExecuteCommandBuffer();
        }

        private void SetupDirectionalLight(int dirLightIndex, int visibleLightIndex, ref VisibleLight visibleLight)
		{
            dirLightColors[dirLightIndex] = visibleLight.finalColor.gamma;
            dirLightDirections[dirLightIndex] = -visibleLight.localToWorldMatrix.GetColumn(2);
            dirShadowDatas[dirLightIndex] = shadows.SaveDirectionalShadows(visibleLight.light, visibleLightIndex);
            dirLights[dirLightIndex] = visibleLight;
		}

        private void SetupPointLight(int clusterLightIndex, int visibleLightIndex, ref VisibleLight visibleLight)
		{
            Matrix4x4 localToWorld = visibleLight.localToWorldMatrix;
            float range = visibleLight.range;
            Vector4 lightSphere = localToWorld.GetColumn(3);
            Vector4 lightRange = new Vector4(range, range, range);
            float threshold = range * range;
            lightSphere.w = 1f / threshold;
            threshold = lightSphere.w;
            otherLightSpheres[clusterLightIndex] = lightSphere;
            clusterLightMaxBounds[clusterLightIndex] = lightSphere + lightRange;
            clusterLightMinBounds[clusterLightIndex] = lightSphere - lightRange;
            otherLightDirections[clusterLightIndex] = Vector4.zero;
            otherLightThresholds[clusterLightIndex] = new Vector4(1f / (1f - threshold), threshold, 0f, 1f);
            otherLightColors[clusterLightIndex] = visibleLight.finalColor.gamma;
            otherShadowDatas[clusterLightIndex] = shadows.SaveOtherShadows(visibleLight.light, visibleLightIndex, clusterLightIndex);
            otherLights[clusterLightIndex] = visibleLight;
		}

        private void SetupSpotLight(int clusterLightIndex, int visibleLightIndex, ref VisibleLight visibleLight)
		{
            Matrix4x4 localToWorld = visibleLight.localToWorldMatrix;
            float range = visibleLight.range;
            Vector4 lightSphere = localToWorld.GetColumn(3);
            float threshold = range * range;
            lightSphere.w = 1f / threshold;
            threshold = lightSphere.w;

            Vector4 lightDir = -localToWorld.GetColumn(2);
            Vector4 lightRight = localToWorld.GetColumn(0);
            Vector4 lightUp = localToWorld.GetColumn(1);
            float outerRad = Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle;
            float outerCos = Mathf.Cos(outerRad);
            float outerSin = Mathf.Sin(outerRad);
            float angleRangeInv = 1f / Mathf.Max(1f - outerCos, 0.001f);

            float sinRange = outerSin * range;
            Vector4 upward = lightUp * sinRange;
            Vector4 rtward = lightRight * sinRange;
            Vector4 p0 = lightSphere;
            Vector4 pf = lightSphere - lightDir * range;
            Vector4 p1 = pf + upward + rtward;
            Vector4 p2 = pf + upward - rtward;
            Vector4 p3 = pf - upward + rtward;
            Vector4 p4 = pf - upward - rtward;

            Vector4 maxBound = Vector4.Max(p4, Vector4.Max(p3, Vector4.Max(p2, Vector4.Max(p1, p0))));
            Vector4 minBound = Vector4.Min(p4, Vector4.Min(p3, Vector4.Min(p2, Vector4.Min(p1, p0))));

            otherLightSpheres[clusterLightIndex] = lightSphere;
            clusterLightMaxBounds[clusterLightIndex] = maxBound;
            clusterLightMinBounds[clusterLightIndex] = minBound;
            otherLightDirections[clusterLightIndex] = lightDir;
            otherLightThresholds[clusterLightIndex] = new Vector4(1f / (1f - threshold), threshold, angleRangeInv, -outerCos * angleRangeInv);
            otherLightColors[clusterLightIndex] = visibleLight.finalColor.gamma;
            otherShadowDatas[clusterLightIndex] = shadows.SaveOtherShadows(visibleLight.light, visibleLightIndex, clusterLightIndex);
            otherLights[clusterLightIndex] = visibleLight;
        }

        private void SetupLights()
		{
            CollectLightDatas();
            if(dirLightCount > 0)
			{
                if (!Shader.IsKeywordEnabled(in dirLightKeyword))
                    commandBuffer.EnableKeyword(in dirLightKeyword);
                commandBuffer.SetGlobalInt(BXShaderPropertyIDs._DirectionalLightCount_ID, dirLightCount);
                commandBuffer.SetGlobalVectorArray(BXShaderPropertyIDs._DirectionalLightDirections_ID, dirLightDirections);
                commandBuffer.SetGlobalVectorArray(BXShaderPropertyIDs._DirectionalLightColors_ID, dirLightColors);
                commandBuffer.SetGlobalVectorArray(BXShaderPropertyIDs._DirectionalShadowDatas_ID, dirShadowDatas);
			}
			else
			{
                if (Shader.IsKeywordEnabled(in dirLightKeyword))
                    commandBuffer.DisableKeyword(in dirLightKeyword);
			}
            if(clusterLightCount > 0)
			{
                if (!Shader.IsKeywordEnabled(in clusterLightKeyword))
                    commandBuffer.EnableKeyword(in clusterLightKeyword);
                commandBuffer.SetGlobalInt(BXShaderPropertyIDs._ClusterLightCount_ID, clusterLightCount);
                commandBuffer.SetGlobalVectorArray(BXShaderPropertyIDs._OtherLightSpheres_ID, otherLightSpheres);
                commandBuffer.SetGlobalVectorArray(BXShaderPropertyIDs._OtherLightDirections_ID, otherLightDirections);
                commandBuffer.SetGlobalVectorArray(BXShaderPropertyIDs._OtherLightThresholds_ID, otherLightThresholds);
                commandBuffer.SetGlobalVectorArray(BXShaderPropertyIDs._OtherLightColors_ID, otherLightColors);
                commandBuffer.SetGlobalVectorArray(BXShaderPropertyIDs._OtherShadowDatas_ID, otherShadowDatas);
                clusterLightCullCompute.Render(camera, this, commonSettings, width, height);
			}
            else
            {
                if (Shader.IsKeywordEnabled(in clusterLightKeyword))
                    commandBuffer.DisableKeyword(in clusterLightKeyword);
            }
        }

        private void ExecuteCommandBuffer()
		{
            context.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
		}

        public void CleanUp()
		{
            shadows.Cleanup();
		}

        public override void Dispose()
		{
            base.Dispose();

            shadows.Dispose();
            clusterLightCullCompute.Dispose();
            lightCookie.Dispose();
            shadows = null;
            clusterLightCullCompute = null;
            lightCookie = null;

            commandBuffer.Dispose();
            commandBuffer = null;

            camera = null;
            commonSettings = null;
            dirLightColors = null;
            dirLightDirections = null;
            dirShadowDatas = null;
        }
    }
}