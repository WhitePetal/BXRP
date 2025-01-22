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


#endif