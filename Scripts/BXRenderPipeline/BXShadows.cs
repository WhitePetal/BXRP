using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BXRenderPipeline
{
    public class BXShadows
    {
        private struct ShadowedDirectionalLight
        {
            public int visibleLightIndex;
            public float slopeScaleBias;
            public float nearPlaneOffst;
        }
        private struct ShadowedClusterLight
        {
            public int lightIndex;
            public int tileIndex;
            public int visibleLightIndex;
            public float slopeScaleBias;
            public float normalBias;
            public bool isPoint;
        }

        private const int maxShadowedDirLightCount = 1;
        private const int maxCascadeCount = 4;

        private const int maxShadowedClusterLightCount = 8;

        private GlobalKeyword dirShadowKeyword = GlobalKeyword.Create("SHADOWS_DIR");
        private GlobalKeyword clusterShadowKeyword = GlobalKeyword.Create("SHADOWS_CLUSTER");

        private ScriptableRenderContext context;
        private CullingResults cullingResults;

        private ShadowedDirectionalLight[] shadowedDirLights = new ShadowedDirectionalLight[maxShadowedDirLightCount];
        private ShadowedClusterLight[] shadowedClusterLights = new ShadowedClusterLight[maxShadowedClusterLightCount];

        private const string BufferName = "Shadows";
        private CommandBuffer commandBuffer = new CommandBuffer
        {
            name = BufferName
        };

        private BXMainCameraRender mainCameraRender;
        private BXRenderCommonSettings commonSettings;

        private int shadowedDirLightCount;
        private int shadowedClusterLightCount;
        private int shadowedClusterLightTileCount;

        private Vector4 shadowMapSizes;

        private Vector4[] cascadeDatas = new Vector4[maxCascadeCount];
        private Vector4[] cascadeCullingSphere = new Vector4[maxCascadeCount];
        private Matrix4x4[] dirShadowMatrixs = new Matrix4x4[maxShadowedDirLightCount * maxCascadeCount];

        private Vector4[] clusterShadowTiles = new Vector4[maxShadowedClusterLightCount * 6];
        private Matrix4x4[] clusterShadowMatrixs = new Matrix4x4[maxShadowedClusterLightCount * 6];

        public void Setup(BXMainCameraRender mainCameraRender)
        {
            this.mainCameraRender = mainCameraRender;
            this.context = mainCameraRender.context;
            this.cullingResults = mainCameraRender.cullingResults;
            this.commonSettings = mainCameraRender.commonSettings;
            this.shadowedDirLightCount = 0;
            this.shadowedClusterLightCount = 0;
            this.shadowedClusterLightTileCount = 0;
        }

        public Vector4 SaveDirectionalShadows(Light light, int visibleLightIndex)
        {
            Vector4 shadowData;
            if (shadowedDirLightCount < maxShadowedDirLightCount && light.shadows != LightShadows.None &&
                light.shadowStrength > 0f && cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds bounds))
            {
                shadowedDirLights[shadowedDirLightCount] = new ShadowedDirectionalLight()
                {
                    visibleLightIndex = visibleLightIndex,
                    slopeScaleBias = light.shadowBias,
                    nearPlaneOffst = light.shadowNearPlane
                };
                shadowData = new Vector4(light.shadowStrength, commonSettings.cascadeCount * shadowedDirLightCount, light.shadowNormalBias);
                ++shadowedDirLightCount;
            }
            else
            {
                shadowData = Vector4.zero;
            }
            return shadowData;
        }

        public Vector4 SaveClusterShadows(Light light, int visibleLightIndex, int lightIndex)
        {
            if (light.shadows == LightShadows.None || light.shadowStrength <= 0f)
            {
                return new Vector4(0f, 0f, 0f, -1f);
            }
            float maskChannel = -1f;
            LightBakingOutput lightBaking = light.bakingOutput;
            if (lightBaking.lightmapBakeType == LightmapBakeType.Mixed && lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask)
            {
                maskChannel = lightBaking.occlusionMaskChannel;
            }
            if (shadowedClusterLightCount >= maxShadowedClusterLightCount ||
                !cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds bounds))
            {
                return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);
            }

            bool isPoint = light.type == LightType.Point;
            int newTileCount = shadowedClusterLightTileCount + (isPoint ? 6 : 1);

            shadowedClusterLights[shadowedClusterLightCount] = new ShadowedClusterLight
            {
                lightIndex = lightIndex,
                tileIndex = shadowedClusterLightTileCount,
                visibleLightIndex = visibleLightIndex,
                slopeScaleBias = light.shadowBias,
                normalBias = light.shadowNormalBias,
                isPoint = isPoint
            };
            ++shadowedClusterLightCount;
            Vector4 data = new Vector4(light.shadowStrength, shadowedClusterLightTileCount, isPoint ? 1 : 0, maskChannel);
            shadowedClusterLightTileCount = newTileCount;
            return data;
        }

        public void Render(bool useShadowMask, List<BXRenderFeature> onDirShadowsRenderFeatures)
        {
            if (!commonSettings.drawShadows)
            {
                bool needExcuteCMD = false;
                if (Shader.IsKeywordEnabled(in dirShadowKeyword))
                {
                    needExcuteCMD = true;
                    commandBuffer.DisableKeyword(dirShadowKeyword);
                }
                if (Shader.IsKeywordEnabled(in clusterShadowKeyword))
                {
                    needExcuteCMD = true;
                    commandBuffer.DisableKeyword(clusterShadowKeyword);
                }
                if (needExcuteCMD) ExecuteCommandBuffer();
                return;
            }

            if (shadowedDirLightCount > 0)
            {
                if (!Shader.IsKeywordEnabled(in dirShadowKeyword))
                    commandBuffer.EnableKeyword(in dirShadowKeyword);
                RenderDirectionalShadows(onDirShadowsRenderFeatures);
            }
            if (shadowedClusterLightCount > 0)
            {
                if (!Shader.IsKeywordEnabled(in clusterShadowKeyword))
                    commandBuffer.EnableKeyword(in clusterShadowKeyword);
                RenderClusterShadows();
            }

            commandBuffer.BeginSample(BufferName);
            commandBuffer.SetGlobalVector(BXShaderPropertyIDs._ShadowMapSize_ID, shadowMapSizes);
            commandBuffer.EndSample(BufferName);
            ExecuteCommandBuffer();
        }

        public void Cleanup()
        {
            commandBuffer.ReleaseTemporaryRT(BXShaderPropertyIDs._DirectionalShadowMap_ID);
            commandBuffer.ReleaseTemporaryRT(BXShaderPropertyIDs._ClusterShadowMap_ID);
            ExecuteCommandBuffer();
        }

        private void ExecuteCommandBuffer()
        {
            context.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
        }

        private void ExecuteRenderFeatures(List<BXRenderFeature> renderFeatures)
		{
            for(int i = 0; i < renderFeatures.Count; ++i)
			{
                renderFeatures[i].Render(commandBuffer, mainCameraRender);
			}
		}

        private void RenderDirectionalShadows(List<BXRenderFeature> onDirShadowsRenderFeatures)
        {
            int shadowMapSize = commonSettings.shadowMapSize;
            commandBuffer.GetTemporaryRT(BXShaderPropertyIDs._DirectionalShadowMap_ID, shadowMapSize, shadowMapSize, commonSettings.shadowMapBits, FilterMode.Point, RenderTextureFormat.Shadowmap, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.Color);
            commandBuffer.SetRenderTarget(BXShaderPropertyIDs._DirectionalShadowMap_TargetID, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            commandBuffer.ClearRenderTarget(true, false, Color.clear);
            commandBuffer.SetGlobalInt(BXShaderPropertyIDs._ShadowPancaking_ID, 1);
            commandBuffer.BeginSample(BufferName);
            ExecuteCommandBuffer();
            shadowMapSizes.x = shadowMapSize;
            shadowMapSizes.y = 1.0f / shadowMapSize;

            int cascadeCount = commonSettings.cascadeCount;
            Vector3 cascadeRatios = new Vector3(commonSettings.cascadeRatio1, commonSettings.cascadeRatio2, commonSettings.cascadeRatio3);
            int tileCount = shadowedDirLightCount * cascadeCount;
            int split = tileCount <= 1 ? 1 : tileCount <= 4 ? 2 : 4;
            int tileSize = shadowMapSize / split;
            float cullingFactor = Mathf.Max(0f, 0.8f - commonSettings.cascadeFade);
            for (int i = 0; i < shadowedDirLightCount; ++i)
            {
                int tileIndexOffset = i * cascadeCount;
                ShadowedDirectionalLight light = shadowedDirLights[i];
                for (int cascadeIndex = 0; cascadeIndex < cascadeCount; ++cascadeIndex)
                {
                    ShadowDrawingSettings dirShadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex, BatchCullingProjectionType.Perspective);
                    cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.visibleLightIndex, cascadeIndex, cascadeCount, cascadeRatios, tileSize, light.nearPlaneOffst,
                        out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData shadowSplitData);
                    shadowSplitData.shadowCascadeBlendCullingFactor = cullingFactor;
                    dirShadowSettings.splitData = shadowSplitData;
                    // 所有方向光都使用同一个级联球体，因此只需要存储第一个光源的即可
                    if (i == 0)
                    {
                        SetCascadeData(cascadeIndex, shadowSplitData.cullingSphere, tileSize);
                    }
                    int tileIndex = cascadeIndex * tileIndexOffset;
                    dirShadowMatrixs[tileIndex] = ConvertToShadowMapTileMatrix(projMatrix * viewMatrix, SetTileViewport(tileIndex, split, tileSize), 1f / split);
                    commandBuffer.SetViewProjectionMatrices(viewMatrix, projMatrix);
                    commandBuffer.SetGlobalDepthBias(1f, 2.5f + light.slopeScaleBias);
                    ExecuteCommandBuffer();
                    context.DrawShadows(ref dirShadowSettings);
                    ExecuteRenderFeatures(onDirShadowsRenderFeatures);
                }
            }
            commandBuffer.SetGlobalDepthBias(0f, 1f);
            float f = 1f - commonSettings.cascadeFade;
            commandBuffer.SetGlobalVector(BXShaderPropertyIDs._ShadowsDistanceFade_ID, new Vector4(
                1f / mainCameraRender.maxShadowDistance,
                1f / commonSettings.distanceFade,
                1f / (1f - f * f))
            );
            commandBuffer.SetGlobalMatrixArray(BXShaderPropertyIDs._DirectionalShadowMatrixs_ID, dirShadowMatrixs);
            commandBuffer.SetGlobalInt(BXShaderPropertyIDs._CascadeCount_ID, commonSettings.cascadeCount);
            commandBuffer.SetGlobalVectorArray(BXShaderPropertyIDs._CascadeCullingSpheres_ID, cascadeCullingSphere);
            commandBuffer.SetGlobalVectorArray(BXShaderPropertyIDs._CascadeDatas_ID, cascadeDatas);
            commandBuffer.EndSample(BufferName);
            ExecuteCommandBuffer();
        }

        private void SetCascadeData(int cascadeIndex, Vector4 cullingSphere, int tileSize)
        {
            float texelSize = 2f * cullingSphere.w / tileSize;
            float filterSize = texelSize * (1 + 1f); // 1 means PCF3x3
            cullingSphere.w -= filterSize;
            cullingSphere.w *= cullingSphere.w;
            cascadeCullingSphere[cascadeIndex] = cullingSphere;
            cascadeDatas[cascadeIndex] = new Vector4(1f / cullingSphere.w, filterSize * 1.4142136f);
        }

        private Matrix4x4 ConvertToShadowMapTileMatrix(Matrix4x4 m, Vector2 offset, float scale)
        {
            if (SystemInfo.usesReversedZBuffer)
            {
                m.m20 = -m.m20;
                m.m21 = -m.m21;
                m.m22 = -m.m22;
                m.m23 = -m.m23;
            }

            m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
            m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
            m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
            m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
            m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
            m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
            m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
            m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
            m.m20 = 0.5f * (m.m20 + m.m30);
            m.m21 = 0.5f * (m.m21 + m.m31);
            m.m22 = 0.5f * (m.m22 + m.m32);
            m.m23 = 0.5f * (m.m23 + m.m33);
            return m;
        }

        private Vector2 SetTileViewport(int index, int split, int tileSize)
		{
            Vector2 offset = new Vector2(index % split, index / split);
            commandBuffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
            return offset;
		}

        private void RenderClusterShadows()
		{
            int shadowMapSize = commonSettings.clusterLightShadowMapSize;
            shadowMapSizes.z = shadowMapSize;
            shadowMapSizes.w = 1f / shadowMapSize;
            commandBuffer.GetTemporaryRT(BXShaderPropertyIDs._ClusterShadowMap_ID, shadowMapSize, shadowMapSize, commonSettings.clusterLightShadowMapBits, FilterMode.Point, RenderTextureFormat.Shadowmap, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.Color);
            commandBuffer.SetRenderTarget(BXShaderPropertyIDs._ClusterLightShadowMap_TargetID, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            commandBuffer.ClearRenderTarget(true, false, Color.clear);
            commandBuffer.SetGlobalInt(BXShaderPropertyIDs._ShadowPancaking_ID, 0);
            commandBuffer.BeginSample(BufferName);
            ExecuteCommandBuffer();

            int tiles = shadowedClusterLightTileCount;
            int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
            int tileSize = shadowMapSize / split;
            for(int i = 0; i < shadowedClusterLightCount; ++i)
			{
				if (shadowedClusterLights[i].isPoint)
				{
                    RenderPointShadow(i, split, tileSize);
				}
				else
				{
                    RenderSpotShadow(i, split, tileSize);
				}
			}
            commandBuffer.SetGlobalVectorArray(BXShaderPropertyIDs._ClusterShadowTiles_ID, clusterShadowTiles);
            commandBuffer.SetGlobalMatrixArray(BXShaderPropertyIDs._ClusterShadowMatrices_ID, clusterShadowMatrixs);
            commandBuffer.EndSample(BufferName);
            ExecuteCommandBuffer();
		}

        private void RenderPointShadow(int index, int split, int tileSize)
		{
            ShadowedClusterLight light = shadowedClusterLights[index];
            var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex, BatchCullingProjectionType.Perspective);
            float texelSize = 2f / tileSize;
            float filterSize = texelSize * (1 + 1f); // 1 means PCF3x3
            float bias = light.normalBias * filterSize * 1.4142136f;
            float tileScale = 1f / split;
            float fovBias = Mathf.Atan(1f + bias + filterSize) * Mathf.Rad2Deg * 2f - 90f;
            for(int i = 0; i < 6; ++i)
			{
                cullingResults.ComputePointShadowMatricesAndCullingPrimitives
                (
                    light.visibleLightIndex, (CubemapFace)i, fovBias,
                    out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix,
                    out ShadowSplitData splitData
                );
                viewMatrix.m11 = -viewMatrix.m11;
                viewMatrix.m12 = -viewMatrix.m12;
                viewMatrix.m13 = -viewMatrix.m13;
                shadowSettings.splitData = splitData;
                int tileIndex = light.tileIndex + i;
                Vector2 offset = SetTileViewport(tileIndex, split, tileSize);
                SetClusterTileData(tileIndex, offset, tileScale, bias);
                clusterShadowMatrixs[tileIndex] = ConvertToShadowMapTileMatrix(projMatrix * viewMatrix, offset, tileScale);
                commandBuffer.SetViewProjectionMatrices(viewMatrix, projMatrix);
                commandBuffer.SetGlobalDepthBias(1f, 2.5f + light.slopeScaleBias);
                ExecuteCommandBuffer();
                context.DrawShadows(ref shadowSettings);
                commandBuffer.SetGlobalDepthBias(0f, 0f);
			}
		}

        private void RenderSpotShadow(int index, int split, int tileSize)
        {
            ShadowedClusterLight light = shadowedClusterLights[index];
            var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex, BatchCullingProjectionType.Perspective);
            cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives
            (
                light.visibleLightIndex,
                out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix,
                out ShadowSplitData splitData
            );
            shadowSettings.splitData = splitData;
            float texelSize = 2f / (tileSize * projMatrix.m00);
            float filterSize = texelSize * (1 + 1f); // 1 means PCF3x3
            float bias = light.normalBias * filterSize * 1.4142136f;
            int tileIndex = light.tileIndex;
            Vector2 offset = SetTileViewport(tileIndex, split, tileSize);
            float tileScale = 1f / split;
            SetClusterTileData(tileIndex, offset, tileScale, bias);
            clusterShadowMatrixs[tileIndex] = ConvertToShadowMapTileMatrix(projMatrix * viewMatrix, offset, tileScale);
            commandBuffer.SetViewProjectionMatrices(viewMatrix, projMatrix);
            commandBuffer.SetGlobalDepthBias(0f, light.slopeScaleBias);
            ExecuteCommandBuffer();
            context.DrawShadows(ref shadowSettings);
            commandBuffer.SetGlobalDepthBias(0f, 0f);
        }

        private void SetClusterTileData(int index, Vector2 offset, float scale, float bias)
		{
            float border = shadowMapSizes.w * 0.5f;
            Vector4 data = new Vector4
            (
                offset.x * scale + border,
                offset.y * scale + border,
                scale - border - border,
                bias
            );
            clusterShadowTiles[index] = data;
		}

        public void OnDispose()
		{
            commandBuffer.Dispose();
            commandBuffer = null;

            mainCameraRender = null;
            commonSettings = null;

            shadowedDirLights = null;
            shadowedClusterLights = null;

            cascadeDatas = null;
            cascadeCullingSphere = null;
            dirShadowMatrixs = null;

            clusterShadowTiles = null;
            clusterShadowMatrixs = null;
        }
    }

}