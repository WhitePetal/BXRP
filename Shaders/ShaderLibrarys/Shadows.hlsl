#ifndef CUSTOME_SHADOWS_INCLUDE
#define CUSTOME_SHADOWS_INCLUDE

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"

#define _DIRECTIONAL_PCF3 1
#define _CLUSTER_PCF3 1

#if defined(_DIRECTIONAL_PCF3)
	#define DIRECTIONAL_FILTER_SAMPLES 4
	#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_DIRECTIONAL_PCF5)
	#define DIRECTIONAL_FILTER_SAMPLES 9
	#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_DIRECTIONAL_PCF7)
	#define DIRECTIONAL_FILTER_SAMPLES 16
	#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif

#if defined(_CLUSTER_PCF3)
	#define CLUSTER_FILTER_SAMPLES 4
	#define CLUSTER_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_CLUSTER_PCF5)
	#define CLUSTER_FILTER_SAMPLES 9
	#define CLUSTER_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_CLUSTER_PCF7)
	#define CLUSTER_FILTER_SAMPLES 16
	#define CLUSTER_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif

#define MAX_DIRECTIONAL_SHADOW_COUNT 1
#define MAX_CASCADE_COUNT 4

#define MAX_CLUSTER_SHADOW_COUNT 8

TEXTURE2D_SHADOW(_DirectionalShadowMap);
TEXTURE2D_SHADOW(_ClusterShadowMap);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

TEXTURE2D(unity_ShadowMask);
SAMPLER(samplerunity_ShadowMask);

CBUFFER_START(UnityShadows)
    // half4 _BXShadowsColor;
    int _CascadeCount;
    float4x4 _DirectionalShadowMatrixs[MAX_DIRECTIONAL_SHADOW_COUNT * MAX_CASCADE_COUNT];
    float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT];
    half4 _CascadeDatas[MAX_CASCADE_COUNT];
    float4 _ShadowsDistanceFade;
    float4 _ShadowMapSize;

    float4x4 _ClusterShadowMatrices[MAX_CLUSTER_SHADOW_COUNT * 6];
	half4 _ClusterShadowTiles[MAX_CLUSTER_SHADOW_COUNT * 6];
CBUFFER_END

half SampleBakedShadows (float3 pos_world, half2 lightmapUV) 
{
    half4 mask;
	#if defined(LIGHTMAP_ON)
		mask = SAMPLE_TEXTURE2D(
			unity_ShadowMask, samplerunity_ShadowMask, lightmapUV
		);
	#else
        // #if UNITY_LIGHT_PROBE_PROXY_VOLUME
        //     if (unity_ProbeVolumeParams.x) 
        //     {
        //         mask = SampleProbeOcclusion(
        //             TEXTURE3D_ARGS(unity_ProbeVolumeSH, samplerunity_ProbeVolumeSH),
        //             pos_world, unity_ProbeVolumeWorldToObject,
        //             unity_ProbeVolumeParams.y, unity_ProbeVolumeParams.z,
        //             unity_ProbeVolumeMin.xyz, unity_ProbeVolumeSizeInv.xyz
        //         );
        //     }
        //     else 
        //     {
        //         mask = unity_ProbesOcclusion;
        //     }
        // #else
            mask = unity_ProbesOcclusion;
        // #endif
	#endif
    return min(mask.r, min(mask.g, min(mask.b, mask.a)));
}

half SampleDirectionalShadowMap(half3 pos_shadow)
{
    return SAMPLE_TEXTURE2D_SHADOW(
		_DirectionalShadowMap, SHADOW_SAMPLER, pos_shadow
	);
}

half FilterDirectionalShadow (float3 shadowCoord) {
	#if defined(DIRECTIONAL_FILTER_SETUP)
        // 在桌面端 weights 和 positions 需要为 float  OpenGLES3.x 则要求必须是 half
        real weights[DIRECTIONAL_FILTER_SAMPLES];
        real2 positions[DIRECTIONAL_FILTER_SAMPLES];
		float4 size = _ShadowMapSize.yyxx;
		DIRECTIONAL_FILTER_SETUP(size, shadowCoord.xy, weights, positions);
		half shadow = half(0.0);
		for (int i = 0; i < int(DIRECTIONAL_FILTER_SAMPLES); ++i) 
        {
			shadow += weights[i] * SampleDirectionalShadowMap(
				half3(positions[i].xy, half(shadowCoord.z))
			);
		}
        return max(half(0.0), shadow);
	#else
		return SampleDirectionalShadowMap(shadowCoord);
	#endif
}

half FadeShadowsStrength(float depth, float scale, float fade)
{
    half strength = (1.0 - depth * scale) * fade;
    strength = saturate(strength);
    return strength;
}

half GetShadowDistanceStrength(float depthView)
{
    return FadeShadowsStrength(depthView, _ShadowsDistanceFade.x, _ShadowsDistanceFade.y);
}

half GetDirectionalShadow(int lightIndex, float2 pos_clip, float3 pos_world, half3 normal_world, half shadowDistanceStrength)
{
    #ifndef SHADOWS_DIR
        return half(1.0);
    #endif

    half4 shadowData = _DirectionalShadowDatas[lightIndex];
    half shadowStrength = shadowData.x * shadowDistanceStrength;
    if(shadowStrength <= half(0.0)) return half(1.0);
    int cascadeIndex;
    half cascadeBlend = half(1.0);
    int cascadeCount = _CascadeCount;
    for(cascadeIndex = 0; cascadeIndex < cascadeCount; ++cascadeIndex)
    {
        float4 sphere = _CascadeCullingSpheres[cascadeIndex];
        float3 dir = pos_world - sphere.xyz;
        float dstSqr = dot(dir, dir);
        if(dstSqr < sphere.w)
        {
            half fade = FadeShadowsStrength(dstSqr, _CascadeDatas[cascadeIndex].x, _ShadowsDistanceFade.z);
            if(cascadeIndex == (cascadeCount - half(1)))
            {
                // shadowStrength *= fade;
            }
            else
            {
                cascadeBlend = fade;
            }
            break;
        }
    }
    #if defined(_CASCADE_BLEND_DITHER)
        half dither = InterleavedGradientNoise(pos_clip.xy, half(0.0));
        if (cascadeBlend < dither) 
        {
            cascadeIndex += 1;
        }
    #endif

    int shadowIndex = shadowData.y + cascadeIndex;
    float3 normalBias = normal_world * _CascadeDatas[cascadeIndex].y * shadowData.z;
    float4 shadowCoord = mul(_DirectionalShadowMatrixs[shadowIndex], float4(pos_world + normalBias , 1.0));
    half shadow = FilterDirectionalShadow(shadowCoord.xyz);
    #if defined(_CASCADE_BLEND_SOFT)
        if (cascadeBlend < half(1.0)) 
        {
            cascadeIndex = cascadeIndex + 1;
            shadowIndex = shadowIndex + 1;
            normalBias = normal_world * (shadowData.z * _CascadeDatas[cascadeIndex].y);
            shadowCoord = mul(_DirectionalShadowMatrixs[shadowIndex], float4(pos_world + normalBias, 1.0));
            shadow = lerp(FilterDirectionalShadow(shadowCoord.xyz), shadow, cascadeBlend);
        }
    #endif

    return lerp(half(1.0), shadow, shadowStrength);
}

half SampleClusterShadowMap(half3 pos_shadow, half3 bounds)
{
    pos_shadow.xy = clamp(pos_shadow.xy, bounds.xy, bounds.xy + bounds.z);
	return SAMPLE_TEXTURE2D_SHADOW(
		_ClusterShadowMap, SHADOW_SAMPLER, pos_shadow
	);
}

half FilterClusterShadow (float3 pos_shadow, half3 bounds)
{
	#if defined(CLUSTER_FILTER_SETUP)
		real weights[CLUSTER_FILTER_SAMPLES];
		real2 positions[CLUSTER_FILTER_SAMPLES];
		float4 size = _ShadowMapSize.wwzz;
		CLUSTER_FILTER_SETUP(size, pos_shadow.xy, weights, positions);
		half shadow = half(0.0);
		for (int i = 0; i < int(CLUSTER_FILTER_SAMPLES); ++i) {
			shadow += weights[i] * SampleClusterShadowMap(
				half3(positions[i].xy, half(pos_shadow.z)),
                bounds
			);
		}
		return max(half(0.0), shadow);
	#else
		return SampleClusterShadowMap(pos_shadow, bounds);
	#endif
}

static const half4 pointShadowPlanes[6] = 
{
	half4(-1.0, 0.0, 0.0, 0.0),
	half4(1.0, 0.0, 0.0, 0.0),
	half4(0.0, -1.0, 0.0, 0.0),
	half4(0.0, 1.0, 0.0, 0.0),
	half4(0.0, 0.0, -1.0, 0.0),
	half4(0.0, 0.0, 1.0, 0.0)
};

int BXCubeMapFaceID(float3 dir)
{
    int faceID;

    if (abs(dir.z) >= abs(dir.x) && abs(dir.z) >= abs(dir.y))
    {
        faceID = (dir.z < 0.0) ? int(CUBEMAPFACE_NEGATIVE_Z) : int(CUBEMAPFACE_POSITIVE_Z);
    }
    else if (abs(dir.y) >= abs(dir.x))
    {
        faceID = (dir.y < 0.0) ? int(CUBEMAPFACE_NEGATIVE_Y) : int(CUBEMAPFACE_POSITIVE_Y);
    }
    else
    {
        faceID = (dir.x < 0.0) ? int(CUBEMAPFACE_NEGATIVE_X) : int(CUBEMAPFACE_POSITIVE_X);
    }

    return faceID;
}

half GetClusterShadow(int lightIndex, half3 lightFwd, float3 pos_world, half3 normal_world)
{
    #ifndef SHADOWS_CLUSTER
    return half(1.0);
    #endif
    half4 shadowData = _ClusterShadowDatas[lightIndex];
    half strength = shadowData.x;
    if(strength <= half(0.0)) return half(1.0);
    
    int tileIndex = shadowData.y;
    // int maskChanel = shadowData.w;
    float3 lightPos = _ClusterLightSpheres[lightIndex].xyz;
    half3 toLightDir = half3(lightPos - pos_world);

    half3 lightPlane;
    if(shadowData.z == half(1.0))
    {
        int faceOffset = BXCubeMapFaceID(-toLightDir);
        tileIndex += faceOffset;
        lightPlane = half3(pointShadowPlanes[faceOffset].xyz);
    }
    else
    {
        lightPlane = lightFwd;
    }
    
    half4 tileData = _ClusterShadowTiles[tileIndex].xyzw;
    half distanceToLightPlane = dot(toLightDir, lightPlane);
    float3 normalBias = normal_world * distanceToLightPlane * tileData.w;
    float4 pos_shadow = mul(_ClusterShadowMatrices[tileIndex], float4(pos_world + normalBias, 1.0));
    return FilterClusterShadow(pos_shadow.xyz / pos_shadow.w, tileData.xyz);
}

void DitherClipShadow(float2 pos_clip, half alpha)
{
    half dither = InterleavedGradientNoise(pos_clip, half(0.0));
    clip(alpha - dither);
}

#endif