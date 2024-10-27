using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Assertions;
using Unity.Mathematics;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;

namespace BXRenderPipeline
{
    public class BXLightCookie : IDisposable
    {
        private enum LightCookieShaderFormat
		{
            None = -1,
            RGB = 0,
            Alpha = 1,
            Red = 2
		}

        public const int maxCookieClusterLightCount = 8;

        private struct LightCookieMapping
		{
            public int lightIndex;
            public Light light;

            public static Func<LightCookieMapping, LightCookieMapping, int> s_CompareByCookieSize = (LightCookieMapping a, LightCookieMapping b) =>
            {
                var alc = a.light.cookie;
                var blc = b.light.cookie;
                int a2 = alc.width * alc.height;
                int b2 = blc.width * blc.height;
                int d = b2 - a2;
                if (d == 0)
                {
                    // 若按尺寸无法排序则按纹理id排序
                    int ai = alc.GetInstanceID();
                    int bi = blc.GetInstanceID();
                    return ai - bi;
                }
                return d;
            };

            public static Func<LightCookieMapping, LightCookieMapping, int> s_CompareByBufferIndex = (LightCookieMapping a, LightCookieMapping b) =>
            {
                return a.lightIndex - b.lightIndex;
            };
		}

        private readonly struct WorkSlice<T>
		{
            private readonly T[] m_Data;
            private readonly int m_Start;
            private readonly int m_Length;

            public int length => m_Length;
            public int capacity => m_Data.Length;

            public WorkSlice(T[] src, int srcStart, int srcLen = -1)
			{
                m_Data = src;
                m_Start = srcStart;
                m_Length = (srcLen < 0) ? srcLen : Math.Min(srcLen, src.Length);
                Assert.IsTrue(m_Start + m_Length <= capacity);
			}

            public WorkSlice(T[] src, int srcLen = -1) : this(src, 0, srcLen) { }
		    
            public T this[int index]
			{
                get => m_Data[m_Start + index];
                set => m_Data[m_Start + index] = value;
			}

            public void Sort(Func<T, T, int> compare)
			{
                if (m_Length > 1)
                    Sorting.QuickSort(m_Data, m_Start, m_Start + m_Length - 1, compare);
			}
        }

        // [] data 的 持久 工作/临时 内存
        private class WorkMemory
		{
            public LightCookieMapping[] lightMappings;
            public Vector4[] uvRects;

            public void Resize(int size)
			{
                if (size <= lightMappings?.Length) return;

                // 避免每次微小的变化都会产生内存分配
                size = Math.Max(size, ((size + 15) / 16) * 16);

                lightMappings = new LightCookieMapping[size];
                uvRects = new Vector4[size];
			}
		}

		private class LightCookieShaderData : IDisposable
		{
            private int m_Size = 0;
            //private bool m_UseStructuredBuffer;

            private Matrix4x4[] m_WorldToLightCpuData;
            private Vector4[] m_AtlasUVRectCpuData;
            private float[] m_LightTypeCpuData;
            private BXShaderBitArray m_CookieEnableBitsCpuData;

            //ComputeBuffer m_WorldToLightBuffer;
            //ComputeBuffer m_AtlasUVRectBuffer;
            //ComputeBuffer m_LightTypeBuffer;

            public Matrix4x4[] worldToLights => m_WorldToLightCpuData;
            public BXShaderBitArray cookieEnableBits => m_CookieEnableBitsCpuData;

            public Vector4[] atlasUVRects => m_AtlasUVRectCpuData;
            public float[] lightTypes => m_LightTypeCpuData;

            public bool isUploaded { get; set; }

            //         public LightCookieShaderData(int size, bool useStructuredBuffer)
            //{
            //             m_UseStructuredBuffer = useStructuredBuffer;
            //             Resize(size);
            //}

            public LightCookieShaderData(int size)
            {
                Resize(size);
            }

            public void Resize(int size)
			{
                if (size < m_Size) return;

                if (m_Size > 0) Dispose();

                m_WorldToLightCpuData = new Matrix4x4[size];
                m_AtlasUVRectCpuData = new Vector4[size];
                m_LightTypeCpuData = new float[size];
                m_CookieEnableBitsCpuData.Resize(size);

                //if (m_UseStructuredBuffer)
                //{
                //    m_WorldToLightBuffer = new ComputeBuffer(size, Marshal.SizeOf<Matrix4x4>());
                //    m_AtlasUVRectBuffer = new ComputeBuffer(size, Marshal.SizeOf<Vector4>());
                //    m_LightTypeBuffer = new ComputeBuffer(size, Marshal.SizeOf<float>());
                //}

                m_Size = size;
            }

            public void Upload(CommandBuffer cmd)
			{
				//if (m_UseStructuredBuffer)
				//{
    //                m_WorldToLightBuffer.SetData(m_WorldToLightCpuData);
    //                m_AtlasUVRectBuffer.SetData(m_AtlasUVRectCpuData);
    //                m_LightTypeBuffer.SetData(m_LightTypeCpuData);

    //                cmd.SetGlobalBuffer(BXShaderPropertyIDs._ClusterLightWorldToLightsBuffer_ID, m_WorldToLightBuffer);
    //                cmd.SetGlobalBuffer(BXShaderPropertyIDs._ClusterLightCookieAltasUVRectsBuffer_ID, m_AtlasUVRectBuffer);
    //                cmd.SetGlobalBuffer(BXShaderPropertyIDs._ClusterLightLightTypesBuffer_ID, m_LightTypeBuffer);
				//}
				//else
				//{
                    cmd.SetGlobalMatrixArray(BXShaderPropertyIDs._ClusterLightWorldToLights_ID, m_WorldToLightCpuData);
                    cmd.SetGlobalVectorArray(BXShaderPropertyIDs._ClusterLightCookieAltasUVRects_ID, m_AtlasUVRectCpuData);
                    cmd.SetGlobalFloatArray(BXShaderPropertyIDs._ClusterLightLightTypes_ID, m_LightTypeCpuData);
                //}

                cmd.SetGlobalFloatArray(BXShaderPropertyIDs._ClusterLightCookieEnableBits_ID, m_CookieEnableBitsCpuData.data);
                isUploaded = true;
			}

            public void Clear(CommandBuffer cmd)
			{
				if (isUploaded)
				{
                    m_CookieEnableBitsCpuData.Clear();
                    cmd.SetGlobalFloatArray(BXShaderPropertyIDs._ClusterLightCookieEnableBits_ID, m_CookieEnableBitsCpuData.data);
                    isUploaded = false;
                }
			}

			public void Dispose()
			{
				//if (m_UseStructuredBuffer)
				//{
    //                m_WorldToLightBuffer.Dispose();
    //                m_AtlasUVRectBuffer.Dispose();
    //                m_LightTypeBuffer.Dispose();
    //            }

                m_WorldToLightCpuData = null;
                m_AtlasUVRectCpuData = null;
                m_LightTypeCpuData = null;
                m_CookieEnableBitsCpuData = default;
            }
		}

        // Unity defines directional light UVs over a unit box centered at light.
        // i.e. (0, 1) uv == (-0.5, 0.5) world area instead of the (0,1) world area.
        //private static readonly Matrix4x4 s_DirLightProj = Matrix4x4.Ortho(-0.5f, 0.5f, -0.5f, 0.5f, -0.5f, 0.5f);

        private Texture2DAtlas m_ClusterLightsCookieAtlas;
        private LightCookieShaderData m_ClusterLightsCookieShaderData;

        private BXRenderCommonSettings commonSettings;
        //private readonly Settings m_Settings;
        private WorkMemory m_WorkMem = new WorkMemory();

        //private int[] m_VisibleLightIndexToShaderDataIndex;

        // 用于重新缩放Cookie以适应图集参数
        private const int k_MaxCookieSizeDivisor = 16;
        private int m_CookieSizeDivisor = 1;
        private uint m_PrevCookieRequestPixelCount = 0xFFFFFFFF;

        private int m_PrevWarnFrame = -1;

        internal RTHandle ClusterLightsCookieAtlas => m_ClusterLightsCookieAtlas?.AtlasTexture;

        private GlobalKeyword cookieEnableKeyword = GlobalKeyword.Create("COOKIE");

        private void InitClusterLights(int size)
		{
            m_ClusterLightsCookieAtlas = new Texture2DAtlas(
                Math.Max(4, commonSettings.cookieAtlas.resolution.x),
                Math.Max(4, commonSettings.cookieAtlas.resolution.y),
                commonSettings.cookieAtlas.format,
                FilterMode.Bilinear,
                false,
                "BX Light Cookie Atlas",
                false);

            m_ClusterLightsCookieShaderData = new LightCookieShaderData(size);
            //const int mainLightCount = 1;
            //m_VisibleLightIndexToShaderDataIndex = new int[m_Settings.maxClusterLights + mainLightCount];

            m_CookieSizeDivisor = 1;
            m_PrevCookieRequestPixelCount = 0xFFFFFFFF;
        }

        public bool isInitialized() => m_ClusterLightsCookieAtlas != null && m_ClusterLightsCookieShaderData != null;
        
  //      public int GetLightCookieShaderDataIndex(int visibleLightIndex)
		//{
  //          if (!isInitialized()) return -1;

  //          return m_VisibleLightIndexToShaderDataIndex[visibleLightIndex];
		//}

        public void Setup(CommandBuffer cmd, BXLights lights, BXRenderCommonSettings commonSettings)
		{
            using var profScope = new ProfilingScope(cmd, ProfilingSampler.Get(BXProfileId.LightCookies));

            this.commonSettings = commonSettings;
   //         bool isMainLightAvailable = lights.dirLightCount > 0;
			//if (isMainLightAvailable)
			//{
   //             var mainLight = lights.dirLights[0];
   //             isMainLightAvailable = SetupMainLight(cmd, ref mainLight);
			//}

            bool isClusterLightsAvaliable = lights.clusterLightCount > 0;
			if (isClusterLightsAvaliable)
			{
                isClusterLightsAvaliable = SetupClusterLights(cmd, lights);
			}

			//if (!isClusterLightsAvaliable)
			//{
   //             if(m_VisibleLightIndexToShaderDataIndex != null &&
			//		m_ClusterLightsCookieShaderData.isUploaded)
			//	{
   //                 int len = m_VisibleLightIndexToShaderDataIndex.Length;
   //                 for(int i = 0; i < len; i++)
			//		{
   //                     m_VisibleLightIndexToShaderDataIndex[i] = -1;
			//		}
			//	}
			//}

            //bool isKeywordLightCookieEnable = isMainLightAvailable || isClusterLightsAvaliable;
            //cmd.SetKeyword(in cookieEnableKeyword, isKeywordLightCookieEnable);
            cmd.SetKeyword(in cookieEnableKeyword, isClusterLightsAvaliable);
        }

  //      private bool SetupMainLight(CommandBuffer cmd, ref VisibleLight visibleMainLight)
		//{
  //          var mainLight = visibleMainLight.light;
  //          var cookieTexture = mainLight.cookie;
  //          bool isMainLightCookieEnable = cookieTexture != null;

		//	if (isMainLightCookieEnable)
		//	{
  //              Matrix4x4 cookieUVTransform = Matrix4x4.identity;
  //              float cookieFormat = (float)GetLightCookieShaderFormat(cookieTexture.graphicsFormat);

  //              //if (mainLight.TryGetComponent(out UniversalAdditionalLightData additionalLightData))
  //              //    GetLightUVScaleOffset(ref additionalLightData, ref cookieUVTransform);

  //              Matrix4x4 cookieMatrix = s_DirLightProj * cookieUVTransform * visibleMainLight.localToWorldMatrix.inverse;

  //              cmd.SetGlobalTexture(BXShaderPropertyIDs._MainLightCookie_ID, cookieTexture);
  //              cmd.SetGlobalMatrix(BXShaderPropertyIDs._MainLightWorldToLight_ID, cookieMatrix);
  //              cmd.SetGlobalFloat(BXShaderPropertyIDs._MainLightCookieFormat_ID, cookieFormat);
  //          }
		//	else
		//	{
  //              cmd.SetGlobalTexture(BXShaderPropertyIDs._MainLightCookie_ID, Texture2D.whiteTexture);
  //              cmd.SetGlobalMatrix(BXShaderPropertyIDs._MainLightWorldToLight_ID, Matrix4x4.identity);
  //              cmd.SetGlobalFloat(BXShaderPropertyIDs._MainLightCookieFormat_ID, (float)LightCookieShaderFormat.None);
  //          }

  //          return isMainLightCookieEnable;
  //      }

        private LightCookieShaderFormat GetLightCookieShaderFormat(GraphicsFormat cookieFormat)
		{
			switch (cookieFormat)
			{
                default:
                    return LightCookieShaderFormat.RGB;
                // A8, A16 GraphicsFormat does not expose yet.
                case (GraphicsFormat)54:
                case (GraphicsFormat)55:
                    return LightCookieShaderFormat.Alpha;
                case GraphicsFormat.R8_SRGB:
                case GraphicsFormat.R8_UNorm:
                case GraphicsFormat.R8_UInt:
                case GraphicsFormat.R8_SNorm:
                case GraphicsFormat.R8_SInt:
                case GraphicsFormat.R16_UNorm:
                case GraphicsFormat.R16_UInt:
                case GraphicsFormat.R16_SNorm:
                case GraphicsFormat.R16_SInt:
                case GraphicsFormat.R16_SFloat:
                case GraphicsFormat.R32_UInt:
                case GraphicsFormat.R32_SInt:
                case GraphicsFormat.R32_SFloat:
                case GraphicsFormat.R_BC4_SNorm:
                case GraphicsFormat.R_BC4_UNorm:
                case GraphicsFormat.R_EAC_SNorm:
                case GraphicsFormat.R_EAC_UNorm:
                    return LightCookieShaderFormat.Red;
            }
		}

        //private void GetLightUVScaleOffset(ref UniversalAdditionalLightData additionalLightData, ref Matrix4x4 uvTransform)
        //{
        //    Vector2 uvScale = Vector2.one / additionalLightData.lightCookieSize;
        //    Vector2 uvOffset = additionalLightData.lightCookieOffset;

        //    if (Mathf.Abs(uvScale.x) < half.MinValue)
        //        uvScale.x = Mathf.Sign(uvScale.x) * half.MinValue;
        //    if (Mathf.Abs(uvScale.y) < half.MinValue)
        //        uvScale.y = Mathf.Sign(uvScale.y) * half.MinValue;

        //    uvTransform = Matrix4x4.Scale(new Vector3(uvScale.x, uvScale.y, 1));
        //    uvTransform.SetColumn(3, new Vector4(-uvOffset.x * uvScale.x, -uvOffset.y * uvScale.y, 0, 1));
        //}

#if UNITY_EDITOR
        private void CheckNeedReCreateAtlas(int size)
		{
            if(m_ClusterLightsCookieAtlas.AtlasTexture.referenceSize.x != commonSettings.cookieAtlas.resolution.x ||
                m_ClusterLightsCookieAtlas.AtlasTexture.referenceSize.y != commonSettings.cookieAtlas.resolution.y ||
                m_ClusterLightsCookieAtlas.AtlasTexture.rt.graphicsFormat != commonSettings.cookieAtlas.format)
			{
                Assert.IsTrue(SystemInfo.IsFormatSupported(commonSettings.cookieAtlas.format, FormatUsage.Sample), "Not Support the CookieAtlas Format");
                m_ClusterLightsCookieAtlas.Release();
                m_ClusterLightsCookieShaderData.Dispose();
                InitClusterLights(size);
            }
		}
#endif

        private bool SetupClusterLights(CommandBuffer cmd, BXLights lights)
		{
            m_WorkMem.Resize(Math.Min(maxCookieClusterLightCount, lights.clusterLightCount));
            int validLightCount = FilterAndValidateClusterLights(lights, m_WorkMem.lightMappings);

            if (validLightCount <= 0) return false;

            if (!isInitialized())
                InitClusterLights(validLightCount);

#if UNITY_EDITOR
            CheckNeedReCreateAtlas(validLightCount);
#endif

            var validLights = new WorkSlice<LightCookieMapping>(m_WorkMem.lightMappings, validLightCount);
            int validUVRectCount = UpdateClusterLightsAtlas(cmd, ref validLights, m_WorkMem.uvRects);

            var validUvRects = new WorkSlice<Vector4>(m_WorkMem.uvRects, validUVRectCount);
            UploadClusterLights(cmd, lights, ref validLights, ref validUvRects);

            bool isClusterLightsEnabled = validUvRects.length > 0;
            return isClusterLightsEnabled;
        }

        private int FilterAndValidateClusterLights(BXLights lights, LightCookieMapping[] validLightMappings)
		{
            int validLightCount = 0;

            int clusterLightCount = lights.clusterLightCount;
            for (int i = 0; i < clusterLightCount; ++i)
			{
				ref var visLight = ref lights.clusterLights.UnsafeElementAtMutable(i);
                Light light = visLight.light;

                if (light.cookie == null) continue;

                var lightType = visLight.lightType;
                if(!(lightType == LightType.Spot || lightType == LightType.Point))
				{
                    Debug.LogWarning($"Additional {lightType.ToString()} light called '{light.name}' has a light cookie which will not be visible.", light);
                    continue;
                }

                LightCookieMapping lp;
				lp.lightIndex = i;
                lp.light = light;

				if (lp.lightIndex >= validLightMappings.Length || validLightCount + 1 >= validLightMappings.Length)
				{
					// TODO: Better error system
					if (clusterLightCount > maxCookieClusterLightCount &&
						Time.frameCount - m_PrevWarnFrame > 60 * 60) // warn throttling: ~60 FPS * 60 secs ~= 1 min
					{
						m_PrevWarnFrame = Time.frameCount;
						Debug.LogWarning($"Max light cookies ({validLightMappings.Length.ToString()}) reached. Some visible lights ({(clusterLightCount - i - 1).ToString()}) might skip light cookie rendering.");
					}

					// Always break, buffer full.
					break;
				}

				validLightMappings[validLightCount++] = lp;
            }

            return validLightCount;
		}

        private int UpdateClusterLightsAtlas(CommandBuffer cmd, ref WorkSlice<LightCookieMapping> validLightMappings, Vector4[] textureAtlasUVRects)
		{
            // 按照cookied尺寸排序，可以提高图集分配效率 和 在下面的Cookie打包时的去重
            validLightMappings.Sort(LightCookieMapping.s_CompareByCookieSize);

            uint cookieRequestPixelCount = ComputeCookieRequestPixelCount(ref validLightMappings);
            var atlasSize = m_ClusterLightsCookieAtlas.AtlasTexture.referenceSize;
            float requestAtlasRatio = cookieRequestPixelCount / (float)(atlasSize.x * atlasSize.y);
            int cookieSizeDivisorApprox = ApproximateCookieSizeDivisor(requestAtlasRatio);

            // 尝试恢复分辨率 并把 cookie 缩放回原来大小
            // 如果 cookie 能够填满 并且
            // 如果我们请求的像素比上次少，我们就找到了正确的除数(防止每帧都重新尝试)
            if(cookieSizeDivisorApprox < m_CookieSizeDivisor &&
                cookieRequestPixelCount < m_PrevCookieRequestPixelCount)
			{
                m_ClusterLightsCookieAtlas.ResetAllocator();
                m_CookieSizeDivisor = cookieSizeDivisorApprox;
			}

            int uvRectCount = 0;
            while(uvRectCount <= 0)
			{
                uvRectCount = FetchUVRects(cmd, ref validLightMappings, textureAtlasUVRects, m_CookieSizeDivisor);
			
                if(uvRectCount <= 0)
				{
                    m_ClusterLightsCookieAtlas.ResetAllocator();

                    m_CookieSizeDivisor = Mathf.Max(m_CookieSizeDivisor + 1, cookieSizeDivisorApprox);
                    m_PrevCookieRequestPixelCount = cookieRequestPixelCount;
				}
            }

            return uvRectCount;
        }

        private uint ComputeCookieRequestPixelCount(ref WorkSlice<LightCookieMapping> validLightMappings)
		{
            uint requestPixelCount = 0;
            int prevCookieID = 0;
            for(int i = 0; i < validLightMappings.length; i++)
			{
                var lcm = validLightMappings[i];
                Texture cookie = lcm.light.cookie;
                int cookieID = cookie.GetInstanceID();

                // 去重Cookie
                // 依赖于排序方式
                if(cookieID == prevCookieID)
				{
                    continue;
				}
                prevCookieID = cookieID;

                int pixelCookieCount = cookie.width * cookie.height;
                requestPixelCount += (uint)pixelCookieCount;
			}

            return requestPixelCount;
        }

        private int ApproximateCookieSizeDivisor(float requestAtlasRatio)
		{
            // (Edge / N)^2 == 1/N^2 of area
            // Ratio/N^2 == 1, sqrt(Ratio) == N, for "1:1" ratio
            return (int)Mathf.Max(Mathf.Ceil(Mathf.Sqrt(requestAtlasRatio)), 1);
		}

        private int FetchUVRects(CommandBuffer cmd, ref WorkSlice<LightCookieMapping> validLightMappings, Vector4[] textureAtlasUVRects, int cookieSizeDivisor)
		{
            int uvRectCount = 0;
            for(int i = 0; i < validLightMappings.length; i++)
			{
                var lcm = validLightMappings[i];

                Light light = lcm.light;
                Texture cookie = light.cookie;

                Vector4 uvScaleOffset = Vector4.zero;
                if(cookie.dimension == TextureDimension.Cube)
				{
                    Assert.IsTrue(light.type == LightType.Point);
                    uvScaleOffset = FetchCube(cmd, cookie, cookieSizeDivisor);
				}
				else
				{
                    Assert.IsTrue(light.type == LightType.Spot, "Light type needs 2D texture!");
                    uvScaleOffset = Fetch2D(cmd, cookie, cookieSizeDivisor);
				}

                bool isCached = uvScaleOffset != Vector4.zero;
				if (!isCached)
				{
                    if(cookieSizeDivisor > k_MaxCookieSizeDivisor)
					{
                        Debug.LogWarning($"Light cookies atlas is extremely full! Some of the light cookies were discarded. Increase light cookie atlas space or reduce the amount of unique light cookies.");
                        // complete fail
                        return uvRectCount;
                    }

                    // 未能给每个cookie分配 uv rect
                    return 0;
				}

                if (!SystemInfo.graphicsUVStartsAtTop)
                    uvScaleOffset.w = 1f - uvScaleOffset.w - uvScaleOffset.y;

                textureAtlasUVRects[uvRectCount++] = uvScaleOffset;
			}

            return uvRectCount;
		}

        private Vector4 FetchCube(CommandBuffer cmd, Texture cookie, int cookieSizeDivisor = 1)
		{
            Assert.IsTrue(cookie != null);
            Assert.IsTrue(cookie.dimension == TextureDimension.Cube);

            Vector4 uvScaleOffset = Vector4.zero;

            int scaledOctCookieSize = Mathf.Max(ComputeOctahedralCookieSize(cookie) / cookieSizeDivisor, 4);

            bool isCached = m_ClusterLightsCookieAtlas.IsCached(out uvScaleOffset, cookie);
			if (isCached)
			{
                m_ClusterLightsCookieAtlas.UpdateTexture(cmd, cookie, ref uvScaleOffset);
			}
			else
			{
                m_ClusterLightsCookieAtlas.AllocateTexture(cmd, ref uvScaleOffset, cookie, scaledOctCookieSize, scaledOctCookieSize);
			}

            // 图集中的cookie大小可能与 cookie texture 的 尺寸不一致
            // 根据图集调整UVRect
            var scaledCookieSize = Vector2.one * scaledOctCookieSize;
            AdjustUVRect(ref uvScaleOffset, cookie, ref scaledCookieSize);
            return uvScaleOffset;
        }

        private int ComputeOctahedralCookieSize(Texture cookie)
		{
            // Map 6*W*H pixels into 2W*2H pixels, so 4/6 ratio or 66% of cube pixels
            int octCookieSize = Math.Max(cookie.width, cookie.height);
            if (commonSettings.cookieAtlas.isPow2)
                octCookieSize = octCookieSize * Mathf.NextPowerOfTwo((int)commonSettings.cubeOctahedraSizeScale);
            else
                octCookieSize = (int)(octCookieSize * commonSettings.cubeOctahedraSizeScale + 0.5f);
            return octCookieSize;
		}

        private void AdjustUVRect(ref Vector4 uvScaleOffset, Texture cookie, ref Vector2 cookieSize)
		{
            if(uvScaleOffset != Vector4.one)
			{
                // 缩小0.5px以夹紧双线性采样以排除图集邻居（no padding）
                ShrinkUVRect(ref uvScaleOffset, 0.5f, ref cookieSize);
            }
		}

        private void ShrinkUVRect(ref Vector4 uvScaleOffset, float amountPixels, ref Vector2 cookieSize)
		{
            var shrinkOffset = Vector2.one * amountPixels / cookieSize;
            var shrinkScale = (cookieSize - Vector2.one * (amountPixels * 2)) / cookieSize;
            uvScaleOffset.z += uvScaleOffset.x * shrinkOffset.x;
            uvScaleOffset.w += uvScaleOffset.y * shrinkOffset.y;
            uvScaleOffset.x *= shrinkScale.x;
            uvScaleOffset.y *= shrinkScale.y;
        }

        private Vector4 Fetch2D(CommandBuffer cmd, Texture cookie, int cookieSizeDivisor = 1)
		{
            Assert.IsTrue(cookie != null);
            Assert.IsTrue(cookie.dimension == TextureDimension.Tex2D);

            Vector4 uvScaleOffset = Vector4.zero;

            var scaledWidth = Mathf.Max(cookie.width / cookieSizeDivisor, 4);
            var scaledHeight = Mathf.Max(cookie.height / cookieSizeDivisor, 4);
            Vector2 scaledCookieSize = new Vector2(scaledWidth, scaledHeight);

            bool isCached = m_ClusterLightsCookieAtlas.IsCached(out uvScaleOffset, cookie);
			if (isCached)
			{
                m_ClusterLightsCookieAtlas.UpdateTexture(cmd, cookie, ref uvScaleOffset);
			}
			else
			{
                m_ClusterLightsCookieAtlas.AllocateTexture(cmd, ref uvScaleOffset, cookie, scaledWidth, scaledHeight);
			}

            AdjustUVRect(ref uvScaleOffset, cookie, ref scaledCookieSize);
            return uvScaleOffset;
		}

        private void UploadClusterLights(CommandBuffer cmd, BXLights lights, ref WorkSlice<LightCookieMapping> validLightMappings, ref WorkSlice<Vector4> validUvRects)
		{
            Assert.IsTrue(m_ClusterLightsCookieAtlas != null);
            Assert.IsTrue(m_ClusterLightsCookieShaderData != null);

            cmd.SetGlobalTexture(BXShaderPropertyIDs._ClusterLightCookieAltas_ID, m_ClusterLightsCookieAtlas.AtlasTexture);
            //cmd.SetGlobalFloat(BXShaderPropertyIDs._ClusterLightCookieAltasFormat_ID, (float)GetLightCookieShaderFormat(m_ClusterLightsCookieAtlas.AtlasTexture.rt.graphicsFormat));

   //         if (m_VisibleLightIndexToShaderDataIndex.Length < lights.clusterLightCount)
   //             m_VisibleLightIndexToShaderDataIndex = new int[lights.clusterLightCount];

   //         int len = Math.Min(m_VisibleLightIndexToShaderDataIndex.Length, lights.clusterLightCount);
   //         for(int i = 0; i < len; i++)
			//{
   //             m_VisibleLightIndexToShaderDataIndex[i] = -1;
			//}

            m_ClusterLightsCookieShaderData.Resize(BXLights.maxClusterLightCount);

            var worldToLights = m_ClusterLightsCookieShaderData.worldToLights;
            var cookieEnableBits = m_ClusterLightsCookieShaderData.cookieEnableBits;
            var atlasUVRects = m_ClusterLightsCookieShaderData.atlasUVRects;
            var lightTypes = m_ClusterLightsCookieShaderData.lightTypes;

            Array.Clear(atlasUVRects, 0, atlasUVRects.Length);
            cookieEnableBits.Clear();

            for(int i = 0; i < validUvRects.length; i++)
			{
                //int visIndex = validLightMappings[i].visibleLightIndex;
                //int bufIndex = validLightMappings[i].lightBufferIndex;
                int lightIndex = validLightMappings[i].lightIndex;

                //m_VisibleLightIndexToShaderDataIndex[visIndex] = bufIndex;

                //ref var visLight = ref lights.clusterLights.UnsafeElementAtMutable(visIndex);
                ref var visLight = ref lights.clusterLights.UnsafeElementAtMutable(lightIndex);

				//lightTypes[bufIndex] = (int)visLight.lightType;
				//worldToLights[bufIndex] = visLight.localToWorldMatrix.inverse;
				//atlasUVRects[bufIndex] = validUvRects[i];
				//cookieEnableBits[bufIndex] = true;
				lightTypes[lightIndex] = (int)visLight.lightType;
				worldToLights[lightIndex] = visLight.localToWorldMatrix.inverse;
				atlasUVRects[lightIndex] = validUvRects[i];
				cookieEnableBits[lightIndex] = true;

				if (visLight.lightType == LightType.Spot)
				{
                    float spotAngle = visLight.spotAngle;
                    float spotRange = visLight.range;
                    var perp = Matrix4x4.Perspective(spotAngle, 1, 0.001f, spotRange);

                    perp.SetColumn(2, perp.GetColumn(2) * -1);

                    //worldToLights[bufIndex] = perp * worldToLights[bufIndex];
                    worldToLights[lightIndex] = perp * worldToLights[lightIndex];
                }
				//else if(visLight.lightType == LightType.Directional)
				//{
    //                Light light = visLight.light;
    //                light.TryGetComponent<UniversalAdditionalLightData>(out var additionalLightData);
    //                {
    //                    Matrix4x4 cookieUVTransform = Matrix4x4.identity;
    //                    GetLightUVScaleOffset(ref additionalLightData, ref cookieUVTransform);

    //                    Matrix4x4 cookieMatrix = s_DirLightProj * cookieUVTransform *
    //                                             visLight.localToWorldMatrix.inverse;

    //                    worldToLights[bufIndex] = cookieMatrix;
    //                }
    //            }
			}

            m_ClusterLightsCookieShaderData.Upload(cmd);
		}

        public void Dispose()
		{
            m_ClusterLightsCookieAtlas?.Release();
            m_ClusterLightsCookieShaderData?.Dispose();
            m_WorkMem = null;
            commonSettings = null;
        }
    }
}
