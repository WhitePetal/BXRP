#ifndef CUSTOME_LIGHT_COOKIES_INCLUDE
#define CUSTOME_LIGHT_COOKIES_INCLUDE

#define MAX_COOKIE_LIGHT_COUNT 8

Texture2D _ClusterLightCookieAltas;
SamplerState sampler_ClusterLightCookieAltas;

CBUFFER_START(LightCookies)
    float4x4 _ClusterLightWorldToLights[MAX_CLUSTER_LIGHT_COUNT];
    half4 _ClusterLightCookieAltasUVRects[MAX_CLUSTER_LIGHT_COUNT];
    float _ClusterLightCookieEnableBits[(MAX_CLUSTER_LIGHT_COUNT + 31) / 32];
    float _ClusterLightLightTypes[MAX_CLUSTER_LIGHT_COUNT];
CBUFFER_END

bool IsLightCookieEnable(int lightIndex)
{
    // 2^5 == 32, bit mask for a float/uint
    uint elemIndex = ((uint)lightIndex) >> 5;
    uint bitOffset = (uint)lightIndex & ((1 << 5) - 1);

    uint elem = asuint(_ClusterLightCookieEnableBits[elemIndex]);

    return (elem & (1u << bitOffset)) != 0u;
}

int GetLightCookieLightType(int lightIndex)
{
    return _ClusterLightLightTypes[lightIndex];
}

half2 ComputeLightCookieUVSpot(float4x4 worldToLight, float3 pos_world, half4 atlasUVRect)
{
    half4 posCS = half4(mul(worldToLight, float4(pos_world, 1.0)));
    half2 posNDC = posCS.xy / posCS.w;
    
    half2 posUV = saturate(posNDC * half(0.5) + half(0.5));

    half2 posAtlasUV = atlasUVRect.xy * posUV + atlasUVRect.zw;

    return posAtlasUV;
}



half2 ComputeLightCookieUVPoint(float4x4 worldToLight, float3 pos_world, float4 atlasUVRect)
{
    half4 posLS = half4(mul(worldToLight, float4(pos_world, 1.0)));

    half3 dirLS = normalize(posLS.xyz / posLS.w);

    half2 posUV = saturate(PackNormalOctQuadEncode(dirLS) * half(0.5) + half(0.5));

    half2 posAtlasUV = atlasUVRect.xy * posUV + atlasUVRect.zw;

    return posAtlasUV;
}

half3 SampleClusterLightCookie(int lightIndex, float3 pos_world)
{
    if(!IsLightCookieEnable(lightIndex))
    {
        return half3(half(1.0), half(1.0), half(1.0));
    }

    int lightType = GetLightCookieLightType(lightIndex);
    int isSpot = lightType == LIGHT_TYPE_SPOT;
    
    float4x4 worldToLight = _ClusterLightWorldToLights[lightIndex];
    half4 uvRect = _ClusterLightCookieAltasUVRects[lightIndex];

    half2 uv;
    if(isSpot)
    {
        uv = ComputeLightCookieUVSpot(worldToLight, pos_world, uvRect);
    }
    else
    {
        uv = ComputeLightCookieUVPoint(worldToLight, pos_world, uvRect);
    }

    half4 color = _ClusterLightCookieAltas.SampleLevel(sampler_ClusterLightCookieAltas, uv, 0);
    return color.rgb;
}

#endif