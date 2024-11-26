#ifndef CUSTOME_BAKED_LIGHTS_INCLUDE
#define CUSTOME_BAKED_LIGHTS_INCLUDE

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

#define MAX_REFLECTION_PROBE_COUNT 4

#if defined(UNITY_DOTS_INSTANCING_ENABLED)
	#define LIGHTMAP_NAME unity_Lightmaps
	#define LIGHTMAP_INDIRECTION_NAME unity_LightmapsInd
	#define LIGHTMAP_SAMPLER_NAME samplerunity_Lightmaps
	#define LIGHTMAP_SAMPLE_EXTRA_ARGS lightmapUV, unity_LightmapIndex.x
	#else
	#define LIGHTMAP_NAME unity_Lightmap
	#define LIGHTMAP_INDIRECTION_NAME unity_LightmapInd
	#define LIGHTMAP_SAMPLER_NAME samplerunity_Lightmap
	#define LIGHTMAP_SAMPLE_EXTRA_ARGS lightmapUV
#endif

TEXTURE2D(unity_Lightmap);
SAMPLER(samplerunity_Lightmap);
TEXTURE2D_ARRAY(unity_Lightmaps);
SAMPLER(samplerunity_Lightmaps);

// cube probe is deprecated, now use probe_atlas
// TEXTURECUBE(unity_SpecCube0);
// SAMPLER(samplerunity_SpecCube0);

TEXTURE2D(bx_ReflProbes_Atlas);
#define samplerurp_ReflProbes_Atlas sampler_LinearClamp
TEXTURECUBE(_GlossyEnvironmentCubeMap);
SAMPLER(sampler_GlossyEnvironmentCubeMap);

// Light Probe Proxy Volume is deprecated in the latest srp
// TEXTURE3D_FLOAT(unity_ProbeVolumeSH);
// SAMPLER(samplerunity_ProbeVolumeSH);

CBUFFER_START(bx_ReflectionProbeBuffer)
	int bx_ReflProbes_Count;
	float4 bx_ReflProbes_BoxMax[MAX_REFLECTION_PROBE_COUNT]; // w is the blend distance
    float4 bx_ReflProbes_BoxMin[MAX_REFLECTION_PROBE_COUNT]; // w is the importance
	float4 bx_ReflProbes_ProbePosition[MAX_REFLECTION_PROBE_COUNT]; // w is positive for box projection, |w| is max mip level
	half4 bx_ReflProbes_MipScaleOffset[MAX_REFLECTION_PROBE_COUNT * 7];
	half4 _GlossyEnvironmentCubeMap_HDR;
CBUFFER_END

half3 SampleLightMap (half2 lightmapUV) {
	#if defined(LIGHTMAP_ON)
		return SampleSingleLightmap(
            TEXTURE2D_LIGHTMAP_ARGS(LIGHTMAP_NAME, LIGHTMAP_SAMPLER_NAME), LIGHTMAP_SAMPLE_EXTRA_ARGS,
			half4(1.0, 1.0, 0.0, 0.0),
			#if defined(UNITY_LIGHTMAP_FULL_HDR)
				false,
			#else
				true,
			#endif
			half4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0, 0.0)
        );
	#else
		return half(0.0);
	#endif
}

half3 SampleSH(half3 normalWS /**, float3 pos_world **/)
{
	// Light Probe Proxy Volume is deprecated in the latest srp
	// if (unity_ProbeVolumeParams.x) 
	// {
	// 	return SampleProbeVolumeSH4(
	// 		TEXTURE3D_ARGS(unity_ProbeVolumeSH, samplerunity_ProbeVolumeSH),
	// 		pos_world, normalWS,
	// 		unity_ProbeVolumeWorldToObject,
	// 		unity_ProbeVolumeParams.y, unity_ProbeVolumeParams.z,
	// 		unity_ProbeVolumeMin.xyz, unity_ProbeVolumeSizeInv.xyz
	// 	);
	// }
	// else 
	// {
		// LPPV is not supported in Ligthweight Pipeline
		real4 SHCoefficients[7];
		SHCoefficients[0] = unity_SHAr;
		SHCoefficients[1] = unity_SHAg;
		SHCoefficients[2] = unity_SHAb;
		SHCoefficients[3] = unity_SHBr;
		SHCoefficients[4] = unity_SHBg;
		SHCoefficients[5] = unity_SHBb;
		SHCoefficients[6] = unity_SHC;
		// return 0.0;
		return max(half3(0, 0, 0), SampleSH9(SHCoefficients, normalWS));
	// }

}

#define _REFLECTION_PROBE_BOX_PROJECTION 1

half CalculateProbeWeight(float3 positionWS, float4 probeBoxMin, float4 probeBoxMax)
{
    half blendDistance = half(probeBoxMax.w);
    half3 weightDir = min(half3(positionWS - probeBoxMin.xyz), half3(probeBoxMax.xyz - positionWS)) / blendDistance;
    return saturate(min(weightDir.x, min(weightDir.y, weightDir.z)));
}

half3 BoxProjectedCubemapDirection(half3 reflectionWS, float3 positionWS, float4 cubemapPositionWS, float4 boxMin, float4 boxMax)
{
    // Is this probe using box projection?
    if (cubemapPositionWS.w > 0.0f)
    {
        float3 boxMinMax = (reflectionWS > 0.0f) ? boxMax.xyz : boxMin.xyz;
        half3 rbMinMax = half3(boxMinMax - positionWS) / reflectionWS;

        half fa = half(min(min(rbMinMax.x, rbMinMax.y), rbMinMax.z));

        half3 worldPos = half3(positionWS - cubemapPositionWS.xyz);

        half3 result = worldPos + reflectionWS * fa;
        return result;
    }
    else
    {
        return reflectionWS;
    }
}

half3 SampleEnvironment(float3 vertex, float depthEye, float3 pos_world, half3 v, half3 n, half roughness) 
{
	half3 reflectVector = reflect(-v, n);
	half3 realVec = reflectVector;
	half mip = roughness * half(16);
	half4 environment = half(0.0);

	half totalWeight = half(0.0);
	float clusterIndex = GetClusterIndex(vertex, depthEye);
	int reflectCount = _ClusterLightingDatas[int(clusterIndex)].y;
    reflectCount = min(reflectCount, bx_ReflProbes_Count);
	int reflectIndexStart = int(clusterIndex * float(MAX_CLUSTER_LIGHT_COUNT));
	[loop]
	for(int offset = 0; offset < reflectCount; ++offset)
	{
		int reflectIndex = _ClusterLightingIndices[reflectIndexStart + offset].y;
		half weight = CalculateProbeWeight(pos_world, bx_ReflProbes_BoxMin[reflectIndex], bx_ReflProbes_BoxMax[reflectIndex]);
		weight = min(weight, half(1.0) - totalWeight);
		if(weight <= half(0.0)) continue;

		#ifdef _REFLECTION_PROBE_BOX_PROJECTION
			realVec = BoxProjectedCubemapDirection(reflectVector, pos_world, bx_ReflProbes_ProbePosition[reflectIndex], bx_ReflProbes_BoxMin[reflectIndex], bx_ReflProbes_BoxMax[reflectIndex]);
		#endif

		half maxMip = abs(bx_ReflProbes_ProbePosition[reflectIndex].w) - half(1);
		mip = min(mip, maxMip);

		half2 uv = saturate(PackNormalOctQuadEncode(realVec));
		float4 scaleOffset = bx_ReflProbes_MipScaleOffset[reflectIndex * 7 + (int)mip];

		environment += SAMPLE_TEXTURE2D_LOD(bx_ReflProbes_Atlas, samplerurp_ReflProbes_Atlas, uv * scaleOffset.xy + scaleOffset.zw, 0) * weight;
	}

	if (totalWeight < half(0.99))
    {
		half4 defaultENV = SAMPLE_TEXTURECUBE_LOD(_GlossyEnvironmentCubeMap, sampler_GlossyEnvironmentCubeMap, reflectVector, mip);
        environment.rgb += DecodeHDREnvironment(defaultENV, _GlossyEnvironmentCubeMap_HDR) * (half(1.0) - totalWeight);
    }

	return environment.rgb;
	// return DecodeHDREnvironment(environment, unity_SpecCube0_HDR);
}

#endif