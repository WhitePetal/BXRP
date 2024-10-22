#ifndef BXPIPELINE_COMMON_LIBRARY
#define BXPIPELINE_COMMON_LIBRARY

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "UnityInput.hlsl"

#define pi half(3.141592)
#define pi_inv half(0.318309)

#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_I_V unity_MatrixInvV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_MATRIX_P glstate_matrix_projection
#define UNITY_PREV_MATRIX_M   unity_MatrixPreviousM
#define UNITY_PREV_MATRIX_I_M unity_MatrixPreviousMI

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

#define GET_PROP(name) UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, name)

half3 DecodeHDR(half4 data, half4 decodeInstructions)
{
    half alpha = decodeInstructions.w * (data.a - half(1.0)) + half(1.0);
    #if defined(UNITY_COLORSPACE_GAMMA)
    return (decodeInstructions.x * alpha) * data.rgb;
    #else
        #if defined(UNITY_USE_NATIVE_HDR)
        return decodeInstructions.x * data.rgb; // Multiplier for future HDRI relative to absolute conversion.
        #else
        return (decodeInstructions.x * pow(alpha, decodeInstructions.y)) * data.rgb;
        #endif
    #endif
}

half3 SafeNormalize(half3 inVec)
{
    half3 dp3 = max(half(0.001), dot(inVec, inVec));
    return inVec * rsqrt(dp3);
}

half2 SafeNormalize(half2 inVec)
{
    half2 dp3 = max(half(0.001), dot(inVec, inVec));
    return inVec * rsqrt(dp3);
}

inline float Linear01Depth( float z )
{
    return Linear01Depth(z, _ZBufferParams);
}

inline float LinearEyeDepth( float z )
{
    return LinearEyeDepth(z, _ZBufferParams);
}

inline float4 ComputeScreenPos(float4 pos) {
    float4 o = pos * 0.5f;
    o.xy = float2(o.x, o.y*_ProjectionParams.x) + o.w;
    o.zw = pos.zw;
    return o;
}

#endif