#ifndef BX_CLUSTERING_INCLUDED
#define BX_CLUSTERING_INCLUDED

#define BX_FP_ZBIN_SCALE (_ClusterParams0.x)
#define BX_FP_ZBIN_OFFSET (_ClusterParams0.y)

// Scale from screen-space UV [0, 1] to tile coordinates [0, tile resolution].
#define BX_FP_TILE_SCALE (_ClusterParams1.xy)
#define BX_FP_TILE_COUNT_X (uint(_ClusterParams1.z))
#define BX_FP_WORDS_PER_TILE (uint(_ClusterParams1.w))

#define BX_FP_ZBIN_COUNT (uint(_ClusterParams2.x))
#define BX_FP_TILE_COUNT (uint(_ClusterParams2.y))

#define MAX_ZBIN_VEC4S 1024
#define MAX_TILE_VEC4S 1024

CBUFFER_START(BX_ClusterParams)
    float4 _ClusterParams0; // ZBinScale, ZBinOffset, ClusterLightCount, 0
    float4 _ClusterParams1; // TileScale.x, TileScale.y, TileResolution.x, WordsPerTile 
    float4 _ClusterParams2; // BinCount, TileCount, 0, 0
CBUFFER_END
CBUFFER_START(bx_ZBinBuffer)
    float4 bx_ZBins[MAX_ZBIN_VEC4S];
CBUFFER_END
CBUFFER_START(bx_TileBuffer)
    float4 bx_Tiles[MAX_TILE_VEC4S];
CBUFFER_END

bool IsPerspectiveProjection()
{
    return (unity_OrthoParams.w == 0);
}

// Returns the forward (central) direction of the current view in the world space.
float3 GetViewForwardDir()
{
    return -UNITY_MATRIX_V[2].xyz;
}

// Select uint4 component by index.
// Helper to improve codegen for 2d indexing (data[x][y])
// Replace:
// data[i / 4][i % 4];
// with:
// select4(data[i / 4], i % 4);
uint Select4(uint4 v, uint i)
{
    // x = 0 = 00
    // y = 1 = 01
    // z = 2 = 10
    // w = 3 = 11
    uint mask0 = uint(int(i << 31) >> 31);
    uint mask1 = uint(int(i << 30) >> 31);
    return
        (((v.w & mask0) | (v.z & ~mask0)) & mask1) |
        (((v.y & mask0) | (v.x & ~mask0)) & ~mask1);
}

#if SHADER_TARGET < 45
uint BX_FirstBitLow(uint m)
{
    // http://graphics.stanford.edu/~seander/bithacks.html#ZerosOnRightFloatCast
    return (asuint((float)(m & asuint(-asint(m)))) >> 23) - 0x7F;
}
#define FIRST_BIT_LOW BX_FirstBitLow
#else
#define FIRST_BIT_LOW firstbitlow
#endif

// internal
struct ClusterIterator
{
    uint tileOffset;
    uint zBinOffset;
    uint tileMask;
    // Stores the next light index in first 16 bits, and the max light index in the last 16 bits.
    uint entityIndexNextMax;
};

// internal
ClusterIterator ClusterInit(float2 normalizedScreenSpaceUV, float3 vSource, int headerIndex)
{
    ClusterIterator state = (ClusterIterator)uint(0);

    // #if defined(SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    //     UNITY_BRANCH if (_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    //     {
    //         #if UNITY_UV_STARTS_AT_TOP
    //             // RemapFoveatedRenderingNonUniformToLinear expects the UV coordinate to be non-flipped, so we un-flip it before
    //             // the call, and then flip it back afterwards.
    //             normalizedScreenSpaceUV.y = 1.0 - normalizedScreenSpaceUV.y;
    //         #endif
    //         normalizedScreenSpaceUV = RemapFoveatedRenderingNonUniformToLinear(normalizedScreenSpaceUV);
    //         #if UNITY_UV_STARTS_AT_TOP
    //                 normalizedScreenSpaceUV.y = 1.0 - normalizedScreenSpaceUV.y;
    //         #endif
    // }
    // #endif // SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER

    uint2 tileId = uint2(normalizedScreenSpaceUV * BX_FP_TILE_SCALE);
    state.tileOffset = tileId.y * BX_FP_TILE_COUNT_X + tileId.x;
    // #if defined(USING_STEREO_MATRICES)
    //     state.tileOffset += BX_FP_TILE_COUNT * unity_StereoEyeIndex;
    // #endif
    state.tileOffset *= BX_FP_WORDS_PER_TILE;

    float viewZ = dot(GetViewForwardDir(), vSource);
    uint zBinBaseIndex = uint(((IsPerspectiveProjection() ? log2(viewZ) : viewZ) * BX_FP_ZBIN_SCALE + BX_FP_ZBIN_OFFSET));
    // #if defined(USING_STEREO_MATRICES)
    //     zBinBaseIndex += BX_FP_ZBIN_COUNT * unity_StereoEyeIndex;
    // #endif
    // The Zbin buffer is laid out in the following manner:
    //                          ZBin 0                                      ZBin 1
    //  .-------------------------^------------------------. .----------------^-------
    // | header0 | header1 | word 1 | word 2 | ... | word N | header0 | header 1 | ...
    //                     `----------------v--------------'
    //                            BX_FP_WORDS_PER_TILE
    //
    // The total length of this buffer is `4*MAX_ZBIN_VEC4S`. `zBinBaseIndex` should
    // always point to the `header 0` of a ZBin, so we clamp it accordingly, to
    // avoid out-of-bounds indexing of the ZBin buffer.
    // The heder layout:
    // max15 ... max1 max0 min16 min15 ... min1 min0
    zBinBaseIndex = zBinBaseIndex * (uint(2) + BX_FP_WORDS_PER_TILE);
    zBinBaseIndex = min(zBinBaseIndex, uint(4)*MAX_ZBIN_VEC4S - (uint(2) + BX_FP_WORDS_PER_TILE));

    uint zBinHeaderIndex = zBinBaseIndex + uint(headerIndex);
    state.zBinOffset = zBinBaseIndex + uint(2);

    uint header = Select4(asuint(bx_ZBins[zBinHeaderIndex / uint(4)]), zBinHeaderIndex % uint(4));

    // The Tiles buffer is laid out in the following manner:
    //                 Tiles 0                           Tiles 1
    //  .-----------------^-------------. .----------------^-------
    //  | word 1 | word 2 | ... | word N | word0 | word1 1 | ...
    //  `----------------v--------------'
    //           BX_FP_WORDS_PER_TILE
    //
    #ifdef _CLUSTER_GREATE_32
        state.entityIndexNextMax = header;
    #else
        uint tileIndex = state.tileOffset;
        uint zBinIndex = state.zBinOffset;
        if (BX_FP_WORDS_PER_TILE > uint(0))
        {
            state.tileMask =
                Select4(asuint(bx_Tiles[tileIndex / uint(4)]), tileIndex % uint(4)) &
                Select4(asuint(bx_ZBins[zBinIndex / uint(4)]), zBinIndex % uint(4)) &
                // light mask ^
                // while header == (min: 2, max: 5):
                // (11111111111111111111111111111111 << (header & 0x1F)) & (11111111111111111111111111111111 >> (31 - (header >> 16)))
                // (11111111111111111111111111111111 << min) & (11111111111111111111111111111111 >> (31 - max))
                // 11111111111111111111111111111100 &  00000111111111111111111111111111
                // 00000111111111111111111111111100
                // 这里选择 0x1F(5bit) 而不是 0xFFFF(16bit) 是因为
                // (header & 0x1F) 范围为 0~2^5-1 = 32
                // 及 header 中的 min 实际上不可能超过 5bit
                (uint(0xFFFFFFFFu) << (header & 0x1F)) & (uint(0xFFFFFFFFu) >> (31 - (header >> 16)));
        }
    #endif

    return state;
}

bool ClusterNext(inout ClusterIterator it, out uint entityIndex)
{
    #ifdef _CLUSTER_GREATE_32
        uint maxIndex = it.entityIndexNextMax >> 16;
        [loop] while (it.tileMask == 0 && (it.entityIndexNextMax & 0xFFFF) <= maxIndex)
        {
            // Extract the lower 16 bits and shift by 5 to divide by 32.
            uint wordIndex = ((it.entityIndexNextMax & 0xFFFF) >> 5);
            uint tileIndex = it.tileOffset + wordIndex;
            uint zBinIndex = it.zBinOffset + wordIndex;
            it.tileMask =
                Select4(asuint(bx_Tiles[tileIndex / 4]), tileIndex % 4) &
                Select4(asuint(bx_ZBins[zBinIndex / 4]), zBinIndex % 4) &
                // Mask out the beginning and end of the word.
                (0xFFFFFFFFu << (it.entityIndexNextMax & 0x1F)) & (0xFFFFFFFFu >> (31 - min(31, maxIndex - wordIndex * 32)));
            // The light index can start at a non-multiple of 32, but the following iterations should always be multiples of 32.
            // So we add 32 and mask out the lower bits.
            it.entityIndexNextMax = (it.entityIndexNextMax + 32) & ~31;
        }
    #endif

    bool hasNext = it.tileMask != uint(0);
    uint bitIndex = FIRST_BIT_LOW(it.tileMask);
    it.tileMask ^= (uint(1) << bitIndex);
    #ifdef _CLUSTER_GREATE_32
        // Subtract 32 because it stores the index of the _next_ word to fetch, but we want the current.
        // The upper 16 bits and bits representing values < 32 are masked out. The latter is due to the fact that it will be
        // included in what FIRST_BIT_LOW returns.
        entityIndex = (((it.entityIndexNextMax - 32) & (0xFFFF & ~31))) + bitIndex;
    #else
        entityIndex = bitIndex;
    #endif
    return hasNext;
}

#define LIGHT_LOOP_BEGIN(normalizedScreenSpaceUV, vSourceRev) { \
    uint lightIndex; \
    ClusterIterator _bx_internal_clusterIterator = ClusterInit(normalizedScreenSpaceUV, vSourceRev, 0); \
    [loop] while (ClusterNext(_bx_internal_clusterIterator, lightIndex)) { \
        // lightIndex += URP_FP_DIRECTIONAL_LIGHTS_COUNT; \
        // FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK

#define LIGHT_LOOP_END } }

#define RELFECTION_LOOP_BEGIN(normalizedScreenSpaceUV, vSourceRev) { \
    uint probeIndex; \
    ClusterIterator _bx_internal_clusterIterator = ClusterInit(normalizedScreenSpaceUV, vSourceRev, 1); \
    [loop] while (ClusterNext(_bx_internal_clusterIterator, probeIndex) && totalWeight < half(0.99)) { \
        // lightIndex += URP_FP_DIRECTIONAL_LIGHTS_COUNT; \
        // FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK

#define RELFECTION_LOOP_END } }

#endif