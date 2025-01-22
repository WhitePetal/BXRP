#ifndef __PROBEVOLUME_HLSL__
#define __PROBEVOLUME_HLSL__

#if defined(SHADER_API_MOBILE) || defined(SHADER_API_SWITCH)
//#define USE_APV_TEXTURE_HALF
#endif // SHADER_API_MOBILE || SHADER_API_SWITCH

#include "Assets/Scripts/BXRenderPipeline/ProbeVolumes/ShaderVariablesProbeVolumes.cs.hlsl"
#include "SphericalHarmonics.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

// Unpack variables
#define _APVWorldOffset _Offset_LayerCount.xyz
#define _APVIndirectionEntryDim _MinLoadedCellInEntries_IndirectionEntryDim.w
#define _APVRcpIndirectionEntryDim _MaxLoadedCellInEntries_RcpIndirectionEntryDim.w
#define _APVMinBrickSize _PoolDim_MinBrickSize.w
#define _APVPoolDim _PoolDim_MinBrickSize.xyz
#define _APVRcpPoolDim _RcpPoolDim_XY.xyz
#define _APVMinEntryPosition _MinEntryPos_Noise.xyz;
#define _APVSamplingNoise _MinEntryPos_Noise.w
#define _APVEntryCount _EntryCount_X_XY_LeakReduction.xy
#define _APVLeakReductionMode _EntryCount_X_XY_LeakReduction.z
#define _APVNormalBias _Biasses_NormalizationClamp.x
#define _APVViewBias _Biases_NormalizationClamp.y
#define _APVMinLoadedCellInEntries _MinLoadedCellInEntries_IndirectionEntryDim.xyz
#define _APVMaxLoadedCellInEntries _MaxLoadedCellInEntries_RcpIndirectionEntryDim.xyz
#define _APVLayerCount (uint)(_Offset_LayerCount.w)
#define _APVMinReflProbeNormalizationFactor _Biasses_NormalizationClamp.z
#define _APVMaxReflProbeNormalizationFactor _Biasses_NormalizationClamp.w
#define _APVFrameIndex _FrameIndex_Weights.x
#define _APVWeight _FrameIndex_Weights.y
#define _APVSkyOcclusionWeight _FrameIndex_Weights.z
#define _APVSkyDirectionWeight _FrameIndex_Weights.w

#ifndef DECODE_SH
#include "DecodeSH.hlsl"
#endif

#ifndef __AMBIENTPROBE_HLSL__
float3 EvaluateAmbientProbe(float3 normalWS)
{
    return float3(0, 0, 0);
}
#endif

#ifndef UNITY_SHADER_VARIABLES_INCLUDED
SAMPLER(s_linear_clamp_sampler);
SAMPLER(s_point_clamp_sampler);
#endif

#ifdef USE_APV_TEXTURE_HALF
#define TEXTURE3D_APV TEXTURE3D_HALF
#else
#define TEXTURE3D_AP TEXTURE3D_FLOAT
#endif

struct APVResources
{
    StructuredBuffer<int> index;
    StructuredBuffer<float3> SkyPrecomputedDirections;

    TEXTURE3D_APV(L0_L1Rx);
    TEXTURE3D_APV(L1G_L1Ry);
    TEXTURE3D_APV(L1B_L1Rz);
    TEXTURE3D_APV(L2_0);
    TEXTURE3D_APV(L2_1);
    TEXTURE3D_APV(L2_2);
    TEXTURE3D_APV(L2_3);
    TEXTURE3D_FLOAT(Validity); // Validity stores indices and requires full float precision to be decoded properly

    TEXTURE3D_APV(ProbeOcclusion);

    TEXTURE3D_APV(SkyOcclusionL0L1);
    TEXTURE3D_FLOAT(SkyShadingDirectionIndices);
}

struct APVResourcesRW
{
    RWTexture3D<float4> L0_L1Rx;
    RWTexture3D<float4> L1G_L1Ry;
    RWTexture3D<float4> L1B_L1Rz;
    RWTexture3D<float4> L2_0;
    RWTexture3D<float4> L2_1;
    RWTexture3D<float4> L2_2;
    RWTexture3D<float4> L2_3;
    RWTexture3D<float4> ProbeOcclusion;
}

#ifndef USE_APV_PROBE_OCCLUSION
// If we are rendering a probe lit renderer, and we have APV enabled, and we are using subtractive or shadowmask mode, we sample occlusion from APV.
#if !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_1) || defined(PROBE_VOLUMES_2)) /** && (defined(LIGHTMAP_SHADOW_MIXING) || defined(SHADOWS_SHADOWMASK)) **/
#define USE_APV_PROBE_OCCLUSION 1
#endif
#endif

#define LOAD_APV_RES_L1(res, target) \
    res.L0_L1Rx = CALL_MERGE_NAME(target, _L0_L1Rx); \
    res.L1G_L1Ry = CALL_MERGE_NAME(target, _L1G_L1Ry); \
    res.L1B_L1Rz = CALL_MERGE_NAME(target, _L1B_L1Rz);

#define LOAD_APV_RES_L2(res, target) \
    res.L2_0 = CALL_MERGE_NAME(target, _L2_0); \
    res.L2_1 = CALL_MERGE_NAME(target, _L2_1); \
    res.L2_2 = CALL_MERGE_NAME(target, _L2_2); \
    res.L2_3 = CALL_MERGE_NAME(target, _L2_3);

#define LOAD_APV_RES_OCCLUSION(res, target) \
    res.ProbeOcclusion = CALL_MERGE_NAME(target, _ProbeOcclusion);

#ifndef PROBE_VOLUMES_L2
    #ifndef USE_APV_PROBE_OCCLUSION
        #define LOAD_APV_RES(res, target) LOAD_APV_RES_L1(res, target)
    #else
        #define LOAD_APV_RES(res, target) LOAD_AVP_RES_L1(res, target) \
            LOAD_APV_RES_OCCLUSION(res, target)
    #endif
#else
    #ifndef USE_APV_PROBE_OCCLUSION
        #define LOAD_APV_RES(res, target) \
            LOAD_APV_RES_L1(res, target) \
            LOAD_APV_RES_L2(res, target)
    #else
        #define LOAD_APV_RES(res, target) \
            LOAD_APV_RES_L1(res, target) \
            LOAD_APV_RES_L2(res, target) \
            LOAD_APV_RES_OCCLUSION(res, target)
    #endif
#endif

struct APVSample
{
    half3 L0;
    half3 L1_R;
    half3 L1_G;
    half3 L1_B;
    #ifdef PROBE_VOLUMES_L2
        half4 L2_R;
        half4 L2_G;
        half4 L2_B;
        half3 L2_C;
    #endif

    float4 skyOcclusionL0L1;
    float3 skyShadingDirection;

    #ifdef USE_APV_PROBE_OCCLUSION
        float4 probeOcclusion;
    #endif

    #define APV_SAMPLE_STATUS_INVALID -1
    #define APV_SAMPLE_STATUS_ENCODED 0
    #define APV_SAMPLE_STATUS_DECODED 1

    int status;

    // Note: at the moment this is called at the moment the struct is built, but it is kept as a separate step
    // as ideally should be called as far as possible from sample to allow for latency hiding.
    void Decode()
    {
        if(status == APV_SAMPLE_STATUS_ENCODED)
        {
            L1_R = DecodeSH(L0.r, L1_R);
            L1_G = DecodeSH(L0.g, L1_G);
            L1_B = DecodeSH(L0.b, L1_B);
            #ifdef PROBE_VOLUMES_L2
                DecodeSH_L2(L0, L2_R, L2_G, L2_B, L2_C);
            #endif

            status = APV_SAMPLE_STATUS_DECODED
        }
    }

    void Encode()
    {
        if(status == APV_SAMPLE_STATUS_DECODED)
        {
            L1_R = EncodeSH(L0.r, L1_R);
            L1_G = EncodeSH(L0.g, L1_G);
            L1_B = EncodeSH(L0.b, L1_B);
            #ifdef PROBE_VOLUMES_L2
                EncodeSH_L2(L0, L2_R, L2_G, L2_B, L2_C);
            #endif

            status = APV_SAMPLE_STATUS_ENCODED
        }
    }
};

// Resources required for APV
StructuredBuffer<int> _APVResIndex;
StructuredBuffer<uint3> _APVResCellIndices;
StructuredBuffer<float3> _SkyPrecomputedDirections;
StructuredBuffer<uint> _AntiLeakData;

TEXTURE3D_APV(_APVResL0_L1Rx);
TEXTURE3D_APV(_APVResL1G_L1Ry);
TEXTURE3D_APV(_APVResL1B_L1Rz);

TEXTURE3D_APV(_APVResL2_0);
TEXTURE3D_APV(_APVResL2_1);
TEXTURE3D_APV(_APVResL2_2);
TEXTURE3D_APV(_APVResL2_3);

TEXTURE3D_APV(_APVProbeOcclusion);

TEXTURE3D_APV(_APVResValidity);

TEXTURE3D_APV(_SkyOcclusionTexL0L1);
TEXTURE3D(_SkyShadingDirectionIndicesTex);

// -------------------------------------------------------------
// Various weighting functions for occlusion or helper functions.
// -------------------------------------------------------------
float3 AddNoiseToSamplingPosition(float3 posWS, float2 positionSS, float3 direction)
{
    #ifdef UNITY_SPACE_TRANSFORMS_INCLUDED
        float3 right = mul((float3x3)GetViewToWorldMatrix(), float3(1.0, 0.0, 0.0));
        float3 top = mul((float3x3)GetViewToWorldMatrix(), float3(0.0, 1.0, 0.0));
        float noise01 = InterleavedGradientNoise(positionSS, _APVFrameIndex);
        float noise02 = frac(noise01 * 100.0);
        float noise03 = frac(noise01 * 1000.0);
        direction += top * (noise02 - 0.5) + right * (noise03 - 0.5);
        return _APVSamplingNoise > 0 ? posWS + noise01 * _APVSamplingNoise * direction : posWS;
    #else
        return posWS;
    #endif
}

uint3 GetSampleOffset(uint i)
{
    return uint3(i, i >> 1, i >> 2) & 1;
}

// The validity mask is sampled once and contains a binary info on whether a probe neighbour (relevant for trilinear) is to be used
// or not. The entry in the mask uses the same mapping that GetSampleOffset above uses
half GetValidityWeight(uint offset, uint validityMask)
{
    uint mask = 1U << offset;
    return (validityMask & mask) > 0 ? 1 : 0;
}

float ProbeDistance(uint subdiv)
{
    return pow(3, subdiv) * _APVMinBrickSize / 3.0f;
}

half ProbeDistanceHalf(uint subdiv)
{
    return pow(half(3), half(subdiv)) * half(_APVMinBrickSize) / half(3.0);
}

float3 GetSnappedProbePosition(float3 posWS, uint subdiv)
{
    float3 distBetweenProbes = ProbeDistance(subdiv);
    float3 dividePos = posWS / distBetweenProbes;
    return (dividePos - frac(dividePos)) * distBetweenProbes;
}

// -------------------------------------------------------------
// Indexing functions
// -------------------------------------------------------------

bool LoadCellIndexMetaData(uint cellFlatIdx, out uint chunkIndex, out int stepSize, out int3 minRelativeIdx, out uint3 sizeOfValid)
{
    uint3 metaData = _APVResCellIndices[cellFlatIdx];

    // See ProbeIndexOfIndices.cs for packing
    chunkIndex = metaData.x & 0x1FFFFFFF;
}

#endif