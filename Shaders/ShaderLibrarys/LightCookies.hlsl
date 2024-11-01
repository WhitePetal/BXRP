#ifndef CUSTOME_LIGHT_COOKIES_INCLUDE
#define CUSTOME_LIGHT_COOKIES_INCLUDE

#define MAX_COOKIE_LIGHT_COUNT 8

Texture2D _OtherLightCookieAltas;
SamplerState sampler_OtherLightCookieAltas;

CBUFFER_START(LightCookies)
    float4x4 _OtherLightWorldToLights[MAX_IMPORTED_OTHER_LIGHT_COUNT];
    half4 _OtherLightCookieAltasUVRects[MAX_IMPORTED_OTHER_LIGHT_COUNT];
    float _OtherLightCookieEnableBits[(MAX_IMPORTED_OTHER_LIGHT_COUNT + 31) / 32];
    float _OtherLightLightTypes[MAX_IMPORTED_OTHER_LIGHT_COUNT];
CBUFFER_END

bool IsLightCookieEnable(int lightIndex)
{
    // 2^5 == 32, bit mask for a float/uint
    uint elemIndex = ((uint)lightIndex) >> 5;
    uint bitOffset = (uint)lightIndex & ((1 << 5) - 1);

    uint elem = asuint(_OtherLightCookieEnableBits[elemIndex]);

    return (elem & (1u << bitOffset)) != 0u;
}

int GetLightCookieLightType(int lightIndex)
{
    return _OtherLightLightTypes[lightIndex];
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

half3 SampleOtherLightCookie(int lightIndex, float3 pos_world)
{
    if(!IsLightCookieEnable(lightIndex))
    {
        return half3(half(1.0), half(1.0), half(1.0));
    }

    int lightType = GetLightCookieLightType(lightIndex);
    int isSpot = lightType == LIGHT_TYPE_SPOT;
    
    float4x4 worldToLight = _OtherLightWorldToLights[lightIndex];
    half4 uvRect = _OtherLightCookieAltasUVRects[lightIndex];

    half2 uv;
    if(isSpot)
    {
        uv = ComputeLightCookieUVSpot(worldToLight, pos_world, uvRect);
    }
    else
    {
        uv = ComputeLightCookieUVPoint(worldToLight, pos_world, uvRect);
    }

    half4 color = _OtherLightCookieAltas.SampleLevel(sampler_OtherLightCookieAltas, uv, 0);
    return color.rgb;
}

#endif