#ifndef CUSTOME_BAKED_LIGHTS_INCLUDE
#define CUSTOME_BAKED_LIGHTS_INCLUDE

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

TEXTURE2D(unity_Lightmap);
SAMPLER(samplerunity_Lightmap);

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

#endif