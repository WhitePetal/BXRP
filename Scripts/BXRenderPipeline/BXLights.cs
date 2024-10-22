using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace BXRenderPipeline
{
    public class BXLights
    {
        private ScriptableRenderContext context;
        private CullingResults cullingResults;

        public const int maxDirLightCount = 1;
        public const int maxClusterLightCount = 16;

        private const string BufferName = "Lights";
        private CommandBuffer commandBuffer = new CommandBuffer()
        {
            name = BufferName
        };

        private BXShadows shadows = new BXShadows();

        public int dirLightCount;
        public int clusterLightCount;

        private GlobalKeyword dirLightKeyword = GlobalKeyword.Create("DIRECTIONAL_LIGHT");
        private GlobalKeyword clusterLightKeyword = GlobalKeyword.Create("CLUSTER_LIGHT");

        private bool useShadowMask;

        public Vector4[]
            dirLightColors = new Vector4[maxDirLightCount],
            dirLightDirections = new Vector4[maxDirLightCount],
            dirShadowDatas = new Vector4[maxDirLightCount],
            clusterLightSpheres = new Vector4[maxClusterLightCount],
            clusterLightColors = new Vector4[maxClusterLightCount],
            clusterLightDirections = new Vector4[maxClusterLightCount],
            clusterLightSpotAngles = new Vector4[maxClusterLightCount],
            clusterLightMinBounds = new Vector4[maxClusterLightCount],
            clusterLightMaxBounds = new Vector4[maxClusterLightCount],
            clusterShadowDatas = new Vector4[maxClusterLightCount];

        public NativeArray<VisibleLight>
            dirLights = new NativeArray<VisibleLight>(maxDirLightCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory),
            clusterLights = new NativeArray<VisibleLight>(maxClusterLightCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        private void CollectLightDatas()
		{
            NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
            dirLightCount = 0;
            clusterLightCount = 0;
            useShadowMask = false;
            for(int visbileLightIndex = 0; visbileLightIndex < visibleLights.Length; ++visbileLightIndex)
			{
                if (dirLightCount >= maxDirLightCount && clusterLightCount >= maxClusterLightCount) break;
                VisibleLight visibleLight = visibleLights[visbileLightIndex];
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

        public void Setup(BXMainCameraRender mainCameraRender, List<BXRenderFeature> onDirShadowsRenderFeatures)
		{
            this.context = mainCameraRender.context;
            this.cullingResults = mainCameraRender.cullingResults;
            shadows.Setup(mainCameraRender);
            commandBuffer.BeginSample(BufferName);
            SetupLights();
            shadows.Render(useShadowMask, onDirShadowsRenderFeatures);
            commandBuffer.EndSample(BufferName);
            ExecuteCommandBuffer();
        }

        private void SetupDirectionalLight(int dirLightIndex, int visibleLightIndex, ref VisibleLight visibleLight)
		{
            dirLightColors[dirLightIndex] = visibleLight.finalColor.linear;
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
            lightSphere.w = 1f / (range * range);
            clusterLightSpheres[clusterLightIndex] = lightSphere;
            clusterLightMaxBounds[clusterLightIndex] = lightSphere + lightRange;
            clusterLightMinBounds[clusterLightIndex] = lightSphere - lightRange;
            clusterLightDirections[clusterLightIndex] = Vector4.zero;
            clusterLightSpotAngles[clusterLightIndex] = new Vector4(0f, 1f);
            clusterLightColors[clusterLightIndex] = visibleLight.finalColor.linear;
            clusterShadowDatas[clusterLightIndex] = shadows.SaveClusterShadows(visibleLight.light, visibleLightIndex, clusterLightIndex);
            clusterLights[clusterLightIndex] = visibleLight;
		}

        private void SetupSpotLight(int clusterLightIndex, int visibleLightIndex, ref VisibleLight visibleLight)
		{
            Matrix4x4 localToWorld = visibleLight.localToWorldMatrix;
            float range = visibleLight.range;
            Vector4 lightSphere = localToWorld.GetColumn(3);
            lightSphere.w = 1f / (range * range);
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

            clusterLightSpheres[clusterLightIndex] = lightSphere;
            clusterLightMaxBounds[clusterLightIndex] = maxBound;
            clusterLightMinBounds[clusterLightIndex] = minBound;
            clusterLightDirections[clusterLightIndex] = lightDir;
            clusterLightSpotAngles[clusterLightIndex] = new Vector4(angleRangeInv, -outerCos * angleRangeInv);
            clusterLightColors[clusterLightIndex] = visibleLight.finalColor.linear * 4 / (outerSin * outerSin);
            clusterShadowDatas[clusterLightIndex] = shadows.SaveClusterShadows(visibleLight.light, visibleLightIndex, clusterLightIndex);
            clusterLights[clusterLightIndex] = visibleLight;
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
                commandBuffer.SetGlobalVectorArray(BXShaderPropertyIDs._ClusterLightSpheres_ID, clusterLightSpheres);
                commandBuffer.SetGlobalVectorArray(BXShaderPropertyIDs._ClusterLightDirections_ID, clusterLightDirections);
                commandBuffer.SetGlobalVectorArray(BXShaderPropertyIDs._ClusterLightSpotAngles_ID, clusterLightSpotAngles);
                commandBuffer.SetGlobalVectorArray(BXShaderPropertyIDs._ClusterLightColors_ID, clusterLightColors);
                commandBuffer.SetGlobalVectorArray(BXShaderPropertyIDs._ClusterShadowDatas_ID, clusterShadowDatas);
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

        public void OnDispose()
		{
            shadows.OnDispose();
            shadows = null;

            commandBuffer.Dispose();
            commandBuffer = null;

            dirLightColors = null;
            dirLightDirections = null;
            dirShadowDatas = null;
            clusterLightSpheres = null;
            clusterLightColors = null;
            clusterLightDirections = null;
            clusterLightSpotAngles = null;
            clusterLightMinBounds = null;
            clusterLightMaxBounds = null;
            clusterShadowDatas = null;

            dirLights.Dispose();
            clusterLights.Dispose();
        }
    }
}