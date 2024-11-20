#ifndef CUSTOME_BAKED_LIGHTS_INCLUDE
#define CUSTOME_BAKED_LIGHTS_INCLUDE

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

TEXTURE2D(unity_Lightmap);
SAMPLER(samplerunity_Lightmap);

TEXTURECUBE(unity_SpecCube0);
SAMPLER(samplerunity_SpecCube0);

// Light Probe Proxy Volume is deprecated in the latest srp
// TEXTURE3D_FLOAT(unity_ProbeVolumeSH);
// SAMPLER(samplerunity_ProbeVolumeSH);

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

half3 SampleEnvironment(float3 pos_world, half3 v, half3 n, half roughness) {
	half3 uvw = reflect(-v, n);
	half4 environment = SAMPLE_TEXTURECUBE_LOD(
		unity_SpecCube0, samplerunity_SpecCube0, uvw, roughness * 16
	);
	// return uvw;
	return DecodeHDREnvironment(environment, unity_SpecCube0_HDR);
}

#endif