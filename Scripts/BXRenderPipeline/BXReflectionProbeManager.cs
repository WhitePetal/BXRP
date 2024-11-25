using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace BXRenderPipeline
{
    public struct BXReflectionProbeManager : IDisposable
    {
        private const int k_MaxVisibleReflectionProbeCount = 4;
        private const int k_MaxAtlasTextureSize = 4096;
        private const int k_MaxMipCount = 7;
        private const string k_ReflectionProbeAtlasName = "BX Reflection Probe Atlas";

        private int2 m_Resolution;
        private RenderTexture m_AtlasTexture0;
        private RenderTexture m_AtlasTexture1;
        private RTHandle m_AtlasTexture0Handle;
        private BXBuddyAllocator m_AtlasAllocator;
        private Dictionary<int, CacheProbe> m_Cache;
        private Dictionary<int, int> m_WarningCache;
        private List<int> m_NeedsUpdate;
        public List<int> m_NeedsRemove;

        public int probeCount;
        public Vector4[] m_BoxMax;
        public Vector4[] m_BoxMin;
        public Vector4[] m_ProbePostion;
        public Vector4[] m_MipScaleOffset;

        private unsafe struct CacheProbe
        {
            public uint updateCount;
            public Hash128 imageContentsHash;
            public int size;
            public int mipCount;
            public fixed int dataIndices[k_MaxMipCount];
            public fixed int levels[k_MaxMipCount];
            public Texture texture;
            public int lastUsed;
            public Vector4 hdrData;
        }

        public static class ShaderProperties
        {
            public static readonly int bx_ReflProbes_BoxMin_ID = Shader.PropertyToID("bx_ReflProbes_BoxMin");
            public static readonly int bx_ReflProbes_BoxMax_ID = Shader.PropertyToID("bx_ReflProbes_BoxMax");
            public static readonly int bx_ReflProbes_ProbePosition_ID = Shader.PropertyToID("bx_ReflProbes_ProbePosition");
            public static readonly int bx_ReflProbes_MipScaleOffset_ID = Shader.PropertyToID("bx_ReflProbes_MipScaleOffset");
            public static readonly int bx_ReflProbes_Count_ID = Shader.PropertyToID("bx_ReflProbes_Count");
            public static readonly int bx_ReflProbes_Atlas_ID = Shader.PropertyToID("bx_ReflProbes_Atlas");
        }

        public RenderTexture atlasRT => m_AtlasTexture0;
        public RTHandle atlasRTHandle => m_AtlasTexture0Handle;

        public static BXReflectionProbeManager Create()
        {
            var instance = new BXReflectionProbeManager();
            instance.Init();
            return instance;
        }

        private void Init()
        {
            m_Resolution = 1;
            var format = GraphicsFormat.B10G11R11_UFloatPack32;
            if (!SystemInfo.IsFormatSupported(format, FormatUsage.Render))
                format = GraphicsFormat.R16G16B16A16_SFloat;
            m_AtlasTexture0 = new RenderTexture(new RenderTextureDescriptor
            {
                width = m_Resolution.x,
                height = m_Resolution.y,
                volumeDepth = 1,
                dimension = TextureDimension.Tex2D,
                graphicsFormat = format,
                useMipMap = false,
                msaaSamples = 1
            });
            m_AtlasTexture0.name = k_ReflectionProbeAtlasName;
            m_AtlasTexture0.filterMode = FilterMode.Bilinear;
            m_AtlasTexture0.hideFlags = HideFlags.HideAndDontSave;
            m_AtlasTexture0.Create();
            // in editor maybe memory leak, unity fixe it int unity6.0
            m_AtlasTexture0Handle = RTHandles.Alloc(m_AtlasTexture0/**, transferOwnership: true**/);

            m_AtlasTexture1 = new RenderTexture(m_AtlasTexture0.descriptor);
            m_AtlasTexture1.name = k_ReflectionProbeAtlasName;
            m_AtlasTexture1.filterMode = FilterMode.Bilinear;
            m_AtlasTexture1.hideFlags = HideFlags.HideAndDontSave;

            // The smallest allocatable resolution we want is 4x4. We calculate the number of levels as:
            // log2(max) - log2(4) = log2(max) - 2
            m_AtlasAllocator = new BXBuddyAllocator(math.floorlog2(k_MaxAtlasTextureSize) - 2, 2);
            m_Cache = new Dictionary<int, CacheProbe>(k_MaxVisibleReflectionProbeCount);
            m_WarningCache = new Dictionary<int, int>(k_MaxVisibleReflectionProbeCount);
            m_NeedsUpdate = new List<int>(k_MaxVisibleReflectionProbeCount);
            m_NeedsRemove = new List<int>(k_MaxVisibleReflectionProbeCount);

            m_BoxMax = new Vector4[k_MaxVisibleReflectionProbeCount];
            m_BoxMin = new Vector4[k_MaxVisibleReflectionProbeCount];
            m_ProbePostion = new Vector4[k_MaxVisibleReflectionProbeCount];
            m_MipScaleOffset = new Vector4[k_MaxVisibleReflectionProbeCount * 7];
        }

        public unsafe void UpdateGPUData(CommandBuffer cmd, ref CullingResults cullingResults)
        {
            var probes = cullingResults.visibleReflectionProbes;
            var probeCount = math.min(probes.Length, k_MaxVisibleReflectionProbeCount);
            var frameIndex = Time.renderedFrameCount;

            // Populate list of probes we need to remove to avoid modifying dictionary while iterating.
            foreach(var (id, cachedProbe) in m_Cache)
            {
                // Evict probe if not used for more than 1 frame, if the texture no longer exists, or if the size changed.
                if(Math.Abs(cachedProbe.lastUsed - frameIndex) > 1 || !cachedProbe.texture || cachedProbe.size != cachedProbe.texture.width)
                {
                    m_NeedsRemove.Add(id);
                    for(var i = 0; i < k_MaxMipCount; ++i)
                    {
                        if (cachedProbe.dataIndices[i] != -1) m_AtlasAllocator.Free(new BuddyAllocation(cachedProbe.levels[i], cachedProbe.dataIndices[i]));
                    }
                }
            }

            foreach(var probeIndex in m_NeedsRemove)
            {
                m_Cache.Remove(probeIndex);
            }
            m_NeedsRemove.Clear();

            foreach(var (id, lastUsed) in m_WarningCache)
            {
                if(Math.Abs(lastUsed - frameIndex) > 1)
                {
                    m_NeedsRemove.Add(id);
                }
            }

            foreach(var probeIndex in m_NeedsRemove)
            {
                m_WarningCache.Remove(probeIndex);
            }
            m_NeedsRemove.Clear();

            var showFullWarning = false;
            var requiredAtlasSize = math.int2(0, 0);

            for(var probeIndex = 0; probeIndex < probeCount; ++probeIndex)
            {
                var probe = probes[probeIndex];

                var texture = probe.texture;
                var id = probe.reflectionProbe.GetInstanceID();
                var wasCached = m_Cache.TryGetValue(id, out var cacheProbe);

                if (!texture) continue;

                if (!wasCached)
                {
                    cacheProbe.size = texture.width;
                    var mipCount = math.ceillog2(cacheProbe.size * 4) + 1; // * 4 means CubeMap width to 2D width
                    var level = m_AtlasAllocator.levelCount + 2 - mipCount;
                    cacheProbe.mipCount = math.min(mipCount, k_MaxMipCount);
                    cacheProbe.texture = texture;

                    var mip = 0;
                    for(; mip < cacheProbe.mipCount; ++mip)
                    {
                        // Clamp to maximum level. This is relevant for 64x64 and lower, which will have valid content
                        // in 1x1 mip. The octahedron size is double the face size, so that ends up at 2x2. Due to
                        // borders the final mip must be 4x4 as that leaves 2x2 texels for the octahedron.
                        var mipLevel = math.min(level + mip, m_AtlasAllocator.levelCount - 1);
                        if (!m_AtlasAllocator.TryAllocate(mipLevel, out var allocation)) break;
                        // We split up the allocation struct because C# cannot do struct fixed arrays :(
                        cacheProbe.levels[mip] = allocation.level;
                        cacheProbe.dataIndices[mip] = allocation.index;
                        var scaleOffset = (int4)(GetScaleOffset(mipLevel, allocation.index, true, false) * m_Resolution.xyxy);
                        requiredAtlasSize = math.max(requiredAtlasSize, scaleOffset.zw + scaleOffset.xy);
                    }

                    // Check if we ran out of space in the atlas.
                    if (mip < cacheProbe.mipCount)
                    {
                        if (!m_WarningCache.ContainsKey(id)) showFullWarning = true;
                        m_WarningCache[id] = frameIndex;
                        for (var i = 0; i < mip; ++i) m_AtlasAllocator.Free(new BuddyAllocation(cacheProbe.levels[i], cacheProbe.dataIndices[i]));
                        for (var i = 0; i < k_MaxMipCount; ++i) cacheProbe.dataIndices[i] = -1;
                        continue;
                    }

                    for (; mip < k_MaxMipCount; ++mip)
                    {
                        cacheProbe.dataIndices[mip] = -1;
                    }
                }

                var needsUpdate = !wasCached || cacheProbe.updateCount != texture.updateCount;
#if UNITY_EDITOR
                needsUpdate |= cacheProbe.imageContentsHash != texture.imageContentsHash;
#endif
                needsUpdate |= cacheProbe.hdrData != probe.hdrData; // The probe needs update if the runtime intensity multiplier changes
                //needsUpdate = true;

                if (needsUpdate)
                {
                    cacheProbe.updateCount = texture.updateCount;
#if UNITY_EDITOR
                    cacheProbe.imageContentsHash = texture.imageContentsHash;
#endif
                    m_NeedsUpdate.Add(id);
                }

                // If the probe is set to be updated every frame, we assign the last used frame to -1 so it's evicted in next frame.
                if (probe.reflectionProbe.mode == ReflectionProbeMode.Realtime && probe.reflectionProbe.refreshMode == ReflectionProbeRefreshMode.EveryFrame)
                    cacheProbe.lastUsed = -1;
                else
                    cacheProbe.lastUsed = frameIndex;

                cacheProbe.hdrData = probe.hdrData;
                m_Cache[id] = cacheProbe;
            }

            // Grow the atlas if it's not big enough to contain the current allocations.
            if(math.any(m_Resolution < requiredAtlasSize))
            {
                requiredAtlasSize = math.max(m_Resolution, math.ceilpow2(requiredAtlasSize));
                var desc = m_AtlasTexture0.descriptor;
                desc.width = requiredAtlasSize.x;
                desc.height = requiredAtlasSize.y;
                m_AtlasTexture1.width = requiredAtlasSize.x;
                m_AtlasTexture1.height = requiredAtlasSize.y;
                m_AtlasTexture1.Create();

                if(m_AtlasTexture0.width != 1)
                {
                    if(SystemInfo.copyTextureSupport != CopyTextureSupport.None)
                    {
                        Graphics.CopyTexture(m_AtlasTexture0, 0, 0, 0, 0, m_Resolution.x, m_Resolution.y, m_AtlasTexture1, 0, 0, 0, 0);
                    }
                    else
                    {
                        Graphics.Blit(m_AtlasTexture0, m_AtlasTexture1, (float2)m_Resolution / requiredAtlasSize, Vector2.zero);
                    }
                }

                m_AtlasTexture0.Release();
                (m_AtlasTexture0, m_AtlasTexture1) = (m_AtlasTexture1, m_AtlasTexture0);
                m_Resolution = requiredAtlasSize;
            }

            var skipCount = 0;
            for(var probeIndex = 0; probeIndex < probeCount; ++probeIndex)
            {
                var probe = probes[probeIndex];
                var id = probe.reflectionProbe.GetInstanceID();
                var dataIndex = probeIndex - skipCount;
                if(!m_Cache.TryGetValue(id, out var cacheProbe) || !probe.texture)
                {
                    skipCount++;
                    continue;
                }
                m_BoxMax[dataIndex] = new Vector4(probe.bounds.max.x, probe.bounds.max.y, probe.bounds.max.z, probe.blendDistance);
                m_BoxMin[dataIndex] = new Vector4(probe.bounds.min.x, probe.bounds.min.y, probe.bounds.min.z, probe.importance);
                m_ProbePostion[dataIndex] = new Vector4(probe.localToWorldMatrix.m03, probe.localToWorldMatrix.m13, probe.localToWorldMatrix.m23, (probe.isBoxProjection ? 1 : -1) * (cacheProbe.mipCount));
                for (var i = 0; i < cacheProbe.mipCount; ++i) m_MipScaleOffset[dataIndex * k_MaxMipCount + i] = GetScaleOffset(cacheProbe.levels[i], cacheProbe.dataIndices[i], false, false);
            }

            if (showFullWarning)
            {
                Debug.LogWarning("A numbter of reflection probes have been skipped due to the reflection probe atlas being full.\nTo ix this, you can decrease the number or resolution of probes.");
            }

            using(new ProfilingScope(cmd, ProfilingSampler.Get(BXProfileId.UpdateReflectionProbeAtlas)))
            {
                if (m_NeedsUpdate.Count > 0)
                {
                    cmd.SetRenderTarget(m_AtlasTexture0);
                    foreach (var probeId in m_NeedsUpdate)
                    {
                        var cacheProbe = m_Cache[probeId];
                        for (var mip = 0; mip < cacheProbe.mipCount; ++mip)
                        {
                            var level = cacheProbe.levels[mip];
                            var dataIndex = cacheProbe.dataIndices[mip];
                            // If we need to y-flip we will instead flip the atlas since that is updated less frequent and then the lookup should be correct.
                            // By doing this we won't have to y-flip the lookup in the shader code.
                            var scaleBias = GetScaleOffset(level, dataIndex, true, !SystemInfo.graphicsUVStartsAtTop);
                            var sizeWaithoutPadding = (1 << (m_AtlasAllocator.levelCount + 1 - level)) - 2;
                            Blitter.BlitCubeToOctahedral2DQuadWithPadding(cmd, cacheProbe.texture, new Vector2(sizeWaithoutPadding, sizeWaithoutPadding), scaleBias, mip, true, 2, cacheProbe.hdrData);
                        }
                    }
                }
                cmd.SetGlobalVectorArray(ShaderProperties.bx_ReflProbes_BoxMin_ID, m_BoxMin);
                cmd.SetGlobalVectorArray(ShaderProperties.bx_ReflProbes_BoxMax_ID, m_BoxMax);
                cmd.SetGlobalVectorArray(ShaderProperties.bx_ReflProbes_ProbePosition_ID, m_ProbePostion);
                cmd.SetGlobalVectorArray(ShaderProperties.bx_ReflProbes_MipScaleOffset_ID, m_MipScaleOffset);
                this.probeCount = probeCount - skipCount;
                cmd.SetGlobalInt(ShaderProperties.bx_ReflProbes_Count_ID, this.probeCount);
                cmd.SetGlobalTexture(ShaderProperties.bx_ReflProbes_Atlas_ID, m_AtlasTexture0);
                cmd.SetGlobalTexture("_GlossyEnvironmentCubeMap", ReflectionProbe.defaultTexture);
                cmd.SetGlobalVector("_GlossyEnvironmentCubeMap_HDR", ReflectionProbe.defaultTextureHDRDecodeValues);
            }

            m_NeedsUpdate.Clear();
        }

        public void Dispose()
        {
            if (m_AtlasTexture0)
            {
                m_AtlasTexture0.Release();
                m_AtlasTexture0Handle.Release();
                RTHandles.Release(m_AtlasTexture0Handle);
            }

            UnityEngine.Object.DestroyImmediate(m_AtlasTexture0);
            UnityEngine.Object.DestroyImmediate(m_AtlasTexture1);

            this = default;
        }

        private float4 GetScaleOffset(int level, int dataIndex, bool includePadding, bool yflip)
        {
            // level = m_AtlasAllocator.levelCount + 2 - (log2(size) + 1) <=>
            // log2(size) + 1 = m_AtlasAllocator.levelCount + 2 - level <=>
            // log2(size) = m_AtlasAllocator.levelCount + 1 - level <=>
            // size = 2^(m_AtlasAllocator.levelCount + 1 - level)
            var size = (1 << (m_AtlasAllocator.levelCount + 1 - level));
            var coordinate = BXSpaceFillingCurves.DecodeMorton2D((uint)dataIndex);
            var scale = (size - (includePadding ? 0 : 2)) / ((float2)m_Resolution);
            var bias = ((float2) coordinate * size + (includePadding ? 0 : 1)) / (m_Resolution);
            if (yflip) bias.y = 1f - bias.y - scale.y;
            return math.float4(scale, bias);
        }
    }
}
