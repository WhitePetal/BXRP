#ifndef CUSTOME_BAKED_LIGHTS_INCLUDE
#define CUSTOME_BAKED_LIGHTS_INCLUDE

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

#define MAX_REFLECTION_PROBE_COUNT 4

TEXTURE2D(unity_Lightmap);
SAMPLER(samplerunity_Lightmap);

// cube probe is deprecated, now use probe_atlas
// TEXTURECUBE(unity_SpecCube0);
// SAMPLER(samplerunity_SpecCube0);

TEXTURE2D(bx_ReflProbes_Atlas);
#define samplerurp_ReflProbes_Atlas sampler_LinearClamp

// Light Probe Proxy Volume is deprecated in the latest srp
// TEXTURE3D_FLOAT(unity_ProbeVolumeSH);
// SAMPLER(samplerunity_ProbeVolumeSH);

CBUFFER_START(bx_ReflectionProbeBuffer)
	float bx_ReflProbes_Count;
	float4 bx_ReflProbes_BoxMax[MAX_REFLECTION_PROBE_COUNT]; // w is the blend distance
    float4 bx_ReflProbes_BoxMin[MAX_REFLECTION_PROBE_COUNT]; // w is the importance
	float4 bx_ReflProbes_ProbePosition[MAX_REFLECTION_PROBE_COUNT]; // w is positive for box projection, |w| is max mip level
	float4 bx_ReflProbes_MipScaleOffset[MAX_REFLECTION_PROBE_COUNT * 7];
CBUFFER_END

half3 SampleLightMap (half2 lightMapUV) {
	#if defined(LIGHTMAP_ON)
		return SampleSingleLightmap(
            TEXTURE2D_ARGS(unity_Lightmap, samplerunity_Lightmap), lightMapUV,
			float4(1.0, 1.0, 0.0, 0.0),
			#if defined(UNITY_LIGHTMAP_FULL_HDR)
				false,
			#else
				true,
			#endif
			float4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0, 0.0)
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

half3 SampleEnvironment(float3 pos_world, half3 v, half3 n, half roughness) 
{
	half weight = CalculateProbeWeight(pos_world, bx_ReflProbes_BoxMin[0], bx_ReflProbes_BoxMax[0]);
	half3 reflectVector = reflect(-v, n);

	#ifdef _REFLECTION_PROBE_BOX_PROJECTION
		reflectVector = BoxProjectedCubemapDirection(reflectVector, pos_world, bx_ReflProbes_ProbePosition[0], bx_ReflProbes_BoxMin[0], bx_ReflProbes_BoxMax[0]);
	#endif

	half mip = roughness * half(16);
	half maxMip = abs(bx_ReflProbes_ProbePosition[0].w) - half(1);
	mip = min(mip, maxMip);
	half2 uv = saturate(PackNormalOctQuadEncode(reflectVector) * half(0.5) + half(0.5));
	float4 scaleOffset = bx_ReflProbes_MipScaleOffset[0 * 7 + (int)mip];

	half4 environment = SAMPLE_TEXTURE2D_LOD(
		bx_ReflProbes_Atlas, samplerurp_ReflProbes_Atlas, uv * scaleOffset.xy + scaleOffset.zw, 0 
	);
	// // return uvw;
	return environment.rgb * weight;
	// return DecodeHDREnvironment(environment, unity_SpecCube0_HDR);
}

#endif