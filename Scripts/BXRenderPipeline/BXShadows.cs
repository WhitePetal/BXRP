using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BXRenderPipeline
{
    public class BXShadows : IDisposable
    {
        public static class ShaderProperties
        {
            public static readonly int _ShadowsDistanceFade_ID = Shader.PropertyToID("_ShadowsDistanceFade");
            public static readonly int _DirectionalShadowMatrixs_ID = Shader.PropertyToID("_DirectionalShadowMatrixs");
            public static readonly int _CascadeCount_ID = Shader.PropertyToID("_CascadeCount");
            public static readonly int _CascadeCullingSpheres_ID = Shader.PropertyToID("_CascadeCullingSpheres");
            public static readonly int _CascadeDatas_ID = Shader.PropertyToID("_CascadeDatas");

            public static readonly int _ShadowMapSize_ID = Shader.PropertyToID("_ShadowMapSize");
            public static readonly int _DirectionalShadowMap_ID = Shader.PropertyToID("_DirectionalShadowMap");
            public static readonly int _OtherShadowMap_ID = Shader.PropertyToID("_OtherShadowMap");
            public static readonly RenderTargetIdentifier _DirectionalShadowMap_TargetID = new RenderTargetIdentifier(_DirectionalShadowMap_ID);
            public static readonly RenderTargetIdentifier _OtherLightShadowMap_TargetID = new RenderTargetIdentifier(_OtherShadowMap_ID);

            public static readonly int _ShadowPancaking_ID = Shader.PropertyToID("_ShadowPancaking");
            public static readonly int _OtherShadowTiles_ID = Shader.PropertyToID("_OtherShadowTiles");
            public static readonly int _OtherShadowMatrices_ID = Shader.PropertyToID("_OtherShadowMatrices");
        }

        private struct ShadowedDirectionalLight
        {
            public int visibleLightIndex;
            public float slopeScaleBias;
            public float nearPlaneOffst;
        }
        private struct ShadowedOtherLight
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

        private const int maxShadowedOtherLightCount = 8;

        private Matrix4x4 viewToWorldMatrix;

        private GlobalKeyword dirShadowKeyword = GlobalKeyword.Create("SHADOWS_DIR");
        private GlobalKeyword otherShadowKeyword = GlobalKeyword.Create("SHADOWS_OTHER");

        private ScriptableRenderContext context;
        private CullingResults cullingResults;

        private ShadowedDirectionalLight[] shadowedDirLights = new ShadowedDirectionalLight[maxShadowedDirLightCount];
        private ShadowedOtherLight[] shadowedOtherLights = new ShadowedOtherLight[maxShadowedOtherLightCount];

        private const string BufferName = "Shadows";
        private CommandBuffer commandBuffer = new CommandBuffer
        {
            name = BufferName
        };

        private BXMainCameraRenderBase mainCameraRender;
        private BXRenderCommonSettings commonSettings;

        private int shadowedDirLightCount;
        private int shadowedOtherLightCount;
        private int shadowedOtherLightTileCount;

        private Vector4 shadowMapSizes;

        private Vector4[] cascadeDatas = new Vector4[maxCascadeCount];
        private Vector4[] cascadeCullingSphere = new Vector4[maxCascadeCount];
        private Matrix4x4[] dirShadowMatrixs = new Matrix4x4[maxShadowedDirLightCount * maxCascadeCount];

        private Vector4[] otherShadowTiles = new Vector4[maxShadowedOtherLightCount * 6];
        private Matrix4x4[] otherShadowMatrixs = new Matrix4x4[maxShadowedOtherLightCount * 6];

        public void Setup(BXMainCameraRenderBase mainCameraRender)
        {
            this.mainCameraRender = mainCameraRender;
            this.context = mainCameraRender.context;
            this.cullingResults = mainCameraRender.cullingResults;
            this.commonSettings = mainCameraRender.commonSettings;
            this.shadowedDirLightCount = 0;
            this.shadowedOtherLightCount = 0;
            this.shadowedOtherLightTileCount = 0;
            this.viewToWorldMatrix = mainCameraRender.viewToWorldMatrix;
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
                shadowData = new Vector4(light.shadowStrength, commonSettings.cascadeCount * shadowedDirLightCount++, light.shadowNormalBias);
            }
            else
            {
                shadowData = Vector4.zero;
            }
            return shadowData;
        }

        public Vector4 SaveOtherShadows(Light light, int visibleLightIndex, int lightIndex)
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
            if (shadowedOtherLightCount >= maxShadowedOtherLightCount ||
                !cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds bounds))
            {
                return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);
            }

            bool isPoint = light.type == LightType.Point;
            int newTileCount = shadowedOtherLightTileCount + (isPoint ? 6 : 1);

            shadowedOtherLights[shadowedOtherLightCount] = new ShadowedOtherLight
            {
                lightIndex = lightIndex,
                tileIndex = shadowedOtherLightTileCount,
                visibleLightIndex = visibleLightIndex,
                slopeScaleBias = light.shadowBias,
                normalBias = light.shadowNormalBias,
                isPoint = isPoint
            };
            ++shadowedOtherLightCount;
            Vector4 data = new Vector4(light.shadowStrength, shadowedOtherLightTileCount, isPoint ? 1 : 0, maskChannel);
            shadowedOtherLightTileCount = newTileCount;
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
                if (Shader.IsKeywordEnabled(in otherShadowKeyword))
                {
                    needExcuteCMD = true;
                    commandBuffer.DisableKeyword(otherShadowKeyword);
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
            if (shadowedOtherLightCount > 0)
            {
                if (!Shader.IsKeywordEnabled(in otherShadowKeyword))
                    commandBuffer.EnableKeyword(in otherShadowKeyword);
                RenderOtherShadows();
            }

            commandBuffer.BeginSample(BufferName);
            commandBuffer.SetGlobalVector(ShaderProperties._ShadowMapSize_ID, shadowMapSizes);
            commandBuffer.EndSample(BufferName);
            ExecuteCommandBuffer();
        }

        public void Cleanup()
        {
            commandBuffer.ReleaseTemporaryRT(ShaderProperties._DirectionalShadowMap_ID);
            commandBuffer.ReleaseTemporaryRT(ShaderProperties._OtherShadowMap_ID);
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
            commandBuffer.GetTemporaryRT(ShaderProperties._DirectionalShadowMap_ID, shadowMapSize, shadowMapSize, commonSettings.shadowMapBits, FilterMode.Bilinear, RenderTextureFormat.Shadowmap, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.Color);
            commandBuffer.SetRenderTarget(ShaderProperties._DirectionalShadowMap_TargetID, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            commandBuffer.ClearRenderTarget(true, false, Color.clear);
            commandBuffer.SetGlobalInt(ShaderProperties._ShadowPancaking_ID, 1);
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
                    ShadowDrawingSettings dirShadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex, BatchCullingProjectionType.Orthographic);
                    cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.visibleLightIndex, cascadeIndex, cascadeCount, cascadeRatios, tileSize, 0f,
                        out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData shadowSplitData);
                    shadowSplitData.shadowCascadeBlendCullingFactor = cullingFactor;
                    dirShadowSettings.splitData = shadowSplitData;
                    // 所有方向光都使用同一个级联球体，因此只需要存储第一个光源的即可
                    if (i == 0)
                    {
                        SetCascadeData(cascadeIndex, shadowSplitData.cullingSphere, tileSize);
                    }
                    int tileIndex = cascadeIndex + tileIndexOffset;
                    dirShadowMatrixs[tileIndex] = ConvertToShadowMapTileMatrix(projMatrix * viewMatrix, SetTileViewport(tileIndex, split, tileSize), 1f / split) * viewToWorldMatrix;
                    commandBuffer.SetViewProjectionMatrices(viewMatrix, projMatrix);
                    commandBuffer.SetGlobalDepthBias(1f, 2.5f + light.slopeScaleBias);
                    ExecuteCommandBuffer();
                    context.DrawShadows(ref dirShadowSettings);
                    ExecuteRenderFeatures(onDirShadowsRenderFeatures);
                }
            }
            commandBuffer.SetGlobalDepthBias(0f, 0f);
            float f = 1f - commonSettings.cascadeFade;
            commandBuffer.SetGlobalVector(ShaderProperties._ShadowsDistanceFade_ID, new Vector4(
                1f / mainCameraRender.maxShadowDistance,
                1f / commonSettings.distanceFade,
                1f / (1f - f * f))
            );
            commandBuffer.SetGlobalMatrixArray(ShaderProperties._DirectionalShadowMatrixs_ID, dirShadowMatrixs);
            commandBuffer.SetGlobalInt(ShaderProperties._CascadeCount_ID, commonSettings.cascadeCount);
            commandBuffer.SetGlobalVectorArray(ShaderProperties._CascadeCullingSpheres_ID, cascadeCullingSphere);
            commandBuffer.SetGlobalVectorArray(ShaderProperties._CascadeDatas_ID, cascadeDatas);
            commandBuffer.EndSample(BufferName);
            ExecuteCommandBuffer();
        }

        private void SetCascadeData(int cascadeIndex, Vector4 cullingSphere, int tileSize)
        {
            float texelSize = 2f * cullingSphere.w / tileSize;
            float filterSize = texelSize * (1 + 1f); // 1 means PCF3x3
            cullingSphere.w -= filterSize;
            cullingSphere.w *= cullingSphere.w;
            Vector3 spherePos = mainCameraRender.worldToViewMatrix * new Vector4(cullingSphere.x, cullingSphere.y, cullingSphere.z, 1f);
            cullingSphere = new Vector4(spherePos.x, spherePos.y, spherePos.z, cullingSphere.w);
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

        private void RenderOtherShadows()
		{
            int shadowMapSize = commonSettings.otherLightShadowMapSize;
            shadowMapSizes.z = shadowMapSize;
            shadowMapSizes.w = 1f / shadowMapSize;
            commandBuffer.GetTemporaryRT(ShaderProperties._OtherShadowMap_ID, shadowMapSize, shadowMapSize, commonSettings.otherLightShadowMapBits, FilterMode.Bilinear, RenderTextureFormat.Shadowmap, RenderTextureReadWrite.Linear, 1, false, RenderTextureMemoryless.Color);
            commandBuffer.SetRenderTarget(ShaderProperties._OtherLightShadowMap_TargetID, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            commandBuffer.ClearRenderTarget(true, false, Color.clear);
            commandBuffer.SetGlobalInt(ShaderProperties._ShadowPancaking_ID, 0);
            commandBuffer.BeginSample(BufferName);
            ExecuteCommandBuffer();

            int tiles = shadowedOtherLightTileCount;
            int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : tiles <= 16 ? 4 : 8;
            int tileSize = shadowMapSize / split;
            for(int i = 0; i < shadowedOtherLightCount; ++i)
			{
				if (shadowedOtherLights[i].isPoint)
				{
                    RenderPointShadow(i, split, tileSize);
				}
				else
				{
                    RenderSpotShadow(i, split, tileSize);
				}
			}
            commandBuffer.SetGlobalVectorArray(ShaderProperties._OtherShadowTiles_ID, otherShadowTiles);
            commandBuffer.SetGlobalMatrixArray(ShaderProperties._OtherShadowMatrices_ID, otherShadowMatrixs);
            commandBuffer.EndSample(BufferName);
            ExecuteCommandBuffer();
		}

        private void RenderPointShadow(int index, int split, int tileSize)
		{
            ShadowedOtherLight light = shadowedOtherLights[index];
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
                SetOtherTileData(tileIndex, offset, tileScale, bias);
                // viewToWorldMatrix is for deferred shading => view space lighting
                // in forward shading, is Matrix.identity
                otherShadowMatrixs[tileIndex] = ConvertToShadowMapTileMatrix(projMatrix * viewMatrix, offset, tileScale) * viewToWorldMatrix;
                commandBuffer.SetViewProjectionMatrices(viewMatrix, projMatrix);
                commandBuffer.SetGlobalDepthBias(1f, 2.5f + light.slopeScaleBias);
                ExecuteCommandBuffer();
                context.DrawShadows(ref shadowSettings);
                commandBuffer.SetGlobalDepthBias(0f, 0f);
			}
		}

        private void RenderSpotShadow(int index, int split, int tileSize)
        {
            ShadowedOtherLight light = shadowedOtherLights[index];
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
            SetOtherTileData(tileIndex, offset, tileScale, bias);
            otherShadowMatrixs[tileIndex] = ConvertToShadowMapTileMatrix(projMatrix * viewMatrix, offset, tileScale) * viewToWorldMatrix;
            commandBuffer.SetViewProjectionMatrices(viewMatrix, projMatrix);
            commandBuffer.SetGlobalDepthBias(1f, 2.5f + light.slopeScaleBias);
            ExecuteCommandBuffer();
            context.DrawShadows(ref shadowSettings);
            commandBuffer.SetGlobalDepthBias(0f, 0f);
        }

        private void SetOtherTileData(int index, Vector2 offset, float scale, float bias)
		{
            float border = shadowMapSizes.w * 0.5f;
            Vector4 data = new Vector4
            (
                offset.x * scale + border,
                offset.y * scale + border,
                scale - border - border,
                bias
            );
            otherShadowTiles[index] = data;
		}

        public void Dispose()
		{
            commandBuffer.Dispose();
            commandBuffer = null;

            mainCameraRender = null;
            commonSettings = null;

            shadowedDirLights = null;
            shadowedOtherLights = null;

            cascadeDatas = null;
            cascadeCullingSphere = null;
            dirShadowMatrixs = null;

            otherShadowTiles = null;
            otherShadowMatrixs = null;
        }
    }

}