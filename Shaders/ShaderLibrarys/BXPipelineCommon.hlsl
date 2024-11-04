#ifndef BXPIPELINE_COMMON_LIBRARY
#define BXPIPELINE_COMMON_LIBRARY

// #define BX_FORWARDPLUS 1
#define BX_DEFERRED 1

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Version.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"
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

#if UNITY_REVERSED_Z
    // TODO: workaround. There's a bug where SHADER_API_GL_CORE gets erroneously defined on switch.
    #if (defined(SHADER_API_GLCORE) && !defined(SHADER_API_SWITCH)) || defined(SHADER_API_GLES3)
        //GL with reversed z => z clip range is [near, -far] -> remapping to [0, far]
        #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) max((coord - _ProjectionParams.y)/(-_ProjectionParams.z-_ProjectionParams.y)*_ProjectionParams.z, 0)
    #else
        //D3d with reversed Z => z clip range is [near, 0] -> remapping to [0, far]
        //max is required to protect ourselves from near plane not being correct/meaningful in case of oblique matrices.
        #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) max(((1.0-(coord)/_ProjectionParams.y)*_ProjectionParams.z),0)
    #endif
#elif UNITY_UV_STARTS_AT_TOP
    //D3d without reversed z => z clip range is [0, far] -> nothing to do
    #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) (coord)
#else
    //Opengl => z clip range is [-near, far] -> remapping to [0, far]
    #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) max(((coord + _ProjectionParams.y)/(_ProjectionParams.z+_ProjectionParams.y))*_ProjectionParams.z, 0)
#endif

// Stereo-related bits
#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)

    #define SLICE_ARRAY_INDEX   unity_StereoEyeIndex

    #define TEXTURE2D_X(textureName)                                        TEXTURE2D_ARRAY(textureName)
    #define TYPED_TEXTURE2D_X(type, textureName)                            TYPED_TEXTURE2D_ARRAY(type, textureName)
    #define TEXTURE2D_X_PARAM(textureName, samplerName)                     TEXTURE2D_ARRAY_PARAM(textureName, samplerName)
    #define TEXTURE2D_X_ARGS(textureName, samplerName)                      TEXTURE2D_ARRAY_ARGS(textureName, samplerName)
    #define TEXTURE2D_X_HALF(textureName)                                   TEXTURE2D_ARRAY_HALF(textureName)
    #define TEXTURE2D_X_FLOAT(textureName)                                  TEXTURE2D_ARRAY_FLOAT(textureName)

    // We need to redeclare these macros for XR reasons to actually utilise the Texture2DArrays
    // TODO: add MSAA support, which is not being used anywhere in URP at the moment
    #undef FRAMEBUFFER_INPUT_X_HALF
    #undef FRAMEBUFFER_INPUT_X_FLOAT
    #undef FRAMEBUFFER_INPUT_X_INT
    #undef FRAMEBUFFER_INPUT_X_UINT
    #undef LOAD_FRAMEBUFFER_X_INPUT

    #if defined(SHADER_API_METAL) && defined(UNITY_NEEDS_RENDERPASS_FBFETCH_FALLBACK)

        #define RENDERPASS_DECLARE_FALLBACK_X(T, idx)                                                   \
        Texture2DArray<T> _UnityFBInput##idx; float4 _UnityFBInput##idx##_TexelSize;                    \
        inline T ReadFBInput_##idx(bool var, uint2 coord) {                                             \
        [branch]if(var) { return hlslcc_fbinput_##idx; }                                                \
        else { return _UnityFBInput##idx.Load(uint4(coord, SLICE_ARRAY_INDEX, 0)); }                    \
        }

        #define FRAMEBUFFER_INPUT_X_HALF(idx)                               cbuffer hlslcc_SubpassInput_f_##idx { half4 hlslcc_fbinput_##idx; bool hlslcc_fbfetch_##idx; };    \
                                                                            RENDERPASS_DECLARE_FALLBACK_X(half4, idx)

        #define FRAMEBUFFER_INPUT_X_FLOAT(idx)                              cbuffer hlslcc_SubpassInput_f_##idx { float4 hlslcc_fbinput_##idx; bool hlslcc_fbfetch_##idx; };   \
                                                                            RENDERPASS_DECLARE_FALLBACK_X(float4, idx)

        #define FRAMEBUFFER_INPUT_X_INT(idx)                                cbuffer hlslcc_SubpassInput_f_##idx { int4 hlslcc_fbinput_##idx; bool hlslcc_fbfetch_##idx; };    \
                                                                            RENDERPASS_DECLARE_FALLBACK_X(int4, idx)

        #define FRAMEBUFFER_INPUT_X_UINT(idx)                               cbuffer hlslcc_SubpassInput_f_##idx { uint4 hlslcc_fbinput_##idx; bool hlslcc_fbfetch_##idx; };   \
                                                                            RENDERPASS_DECLARE_FALLBACK_X(uint4, idx)

        #define LOAD_FRAMEBUFFER_X_INPUT(idx, v2fname)                      ReadFBInput_##idx(hlslcc_fbfetch_##idx, uint2(v2fname.xy))

    #elif !defined(PLATFORM_SUPPORTS_NATIVE_RENDERPASS)
        #define FRAMEBUFFER_INPUT_X_HALF(idx)                               TEXTURE2D_X_HALF(_UnityFBInput##idx); float4 _UnityFBInput##idx##_TexelSize
        #define FRAMEBUFFER_INPUT_X_FLOAT(idx)                              TEXTURE2D_X_FLOAT(_UnityFBInput##idx); float4 _UnityFBInput##idx##_TexelSize
        #define FRAMEBUFFER_INPUT_X_INT(idx)                                TEXTURE2D_X_INT(_UnityFBInput##idx); float4 _UnityFBInput##idx##_TexelSize
        #define FRAMEBUFFER_INPUT_X_UINT(idx)                               TEXTURE2D_X_UINT(_UnityFBInput##idx); float4 _UnityFBInput##idx##_TexelSize
        #define LOAD_FRAMEBUFFER_X_INPUT(idx, v2fname)                      _UnityFBInput##idx.Load(uint4(v2fname.xy, SLICE_ARRAY_INDEX, 0))
    #else
        #define FRAMEBUFFER_INPUT_X_HALF(idx)                               FRAMEBUFFER_INPUT_HALF(idx)
        #define FRAMEBUFFER_INPUT_X_FLOAT(idx)                              FRAMEBUFFER_INPUT_FLOAT(idx)
        #define FRAMEBUFFER_INPUT_X_INT(idx)                                FRAMEBUFFER_INPUT_INT(idx)
        #define FRAMEBUFFER_INPUT_X_UINT(idx)                               FRAMEBUFFER_INPUT_UINT(idx)
        #define LOAD_FRAMEBUFFER_X_INPUT(idx, v2fname)                      LOAD_FRAMEBUFFER_INPUT(idx, v2fname)
    #endif

    #define LOAD_TEXTURE2D_X(textureName, unCoord2)                         LOAD_TEXTURE2D_ARRAY(textureName, unCoord2, SLICE_ARRAY_INDEX)
    #define LOAD_TEXTURE2D_X_LOD(textureName, unCoord2, lod)                LOAD_TEXTURE2D_ARRAY_LOD(textureName, unCoord2, SLICE_ARRAY_INDEX, lod)
    #define SAMPLE_TEXTURE2D_X(textureName, samplerName, coord2)            SAMPLE_TEXTURE2D_ARRAY(textureName, samplerName, coord2, SLICE_ARRAY_INDEX)
    #define SAMPLE_TEXTURE2D_X_LOD(textureName, samplerName, coord2, lod)   SAMPLE_TEXTURE2D_ARRAY_LOD(textureName, samplerName, coord2, SLICE_ARRAY_INDEX, lod)
    #define GATHER_TEXTURE2D_X(textureName, samplerName, coord2)            GATHER_TEXTURE2D_ARRAY(textureName, samplerName, coord2, SLICE_ARRAY_INDEX)
    #define GATHER_RED_TEXTURE2D_X(textureName, samplerName, coord2)        GATHER_RED_TEXTURE2D(textureName, samplerName, float3(coord2, SLICE_ARRAY_INDEX))
    #define GATHER_GREEN_TEXTURE2D_X(textureName, samplerName, coord2)      GATHER_GREEN_TEXTURE2D(textureName, samplerName, float3(coord2, SLICE_ARRAY_INDEX))
    #define GATHER_BLUE_TEXTURE2D_X(textureName, samplerName, coord2)       GATHER_BLUE_TEXTURE2D(textureName, samplerName, float3(coord2, SLICE_ARRAY_INDEX))
    #define GATHER_ALPHA_TEXTURE2D_X(textureName, samplerName, coord2)      GATHER_ALPHA_TEXTURE2D(textureName, samplerName, float3(coord2, SLICE_ARRAY_INDEX))

#else
    #define SLICE_ARRAY_INDEX       0

    #define TEXTURE2D_X(textureName)                                        TEXTURE2D(textureName)
    #define TYPED_TEXTURE2D_X(type, textureName)                            TYPED_TEXTURE2D(type, textureName)
    #define TEXTURE2D_X_PARAM(textureName, samplerName)                     TEXTURE2D_PARAM(textureName, samplerName)
    #define TEXTURE2D_X_ARGS(textureName, samplerName)                      TEXTURE2D_ARGS(textureName, samplerName)
    #define TEXTURE2D_X_HALF(textureName)                                   TEXTURE2D_HALF(textureName)
    #define TEXTURE2D_X_FLOAT(textureName)                                  TEXTURE2D_FLOAT(textureName)

    #define FRAMEBUFFER_INPUT_X_HALF(idx)                                   FRAMEBUFFER_INPUT_HALF(idx)
    #define FRAMEBUFFER_INPUT_X_FLOAT(idx)                                  FRAMEBUFFER_INPUT_FLOAT(idx)
    #define FRAMEBUFFER_INPUT_X_INT(idx)                                    FRAMEBUFFER_INPUT_INT(idx)
    #define FRAMEBUFFER_INPUT_X_UINT(idx)                                   FRAMEBUFFER_INPUT_UINT(idx)
    #define LOAD_FRAMEBUFFER_X_INPUT(idx, v2fname)                          LOAD_FRAMEBUFFER_INPUT(idx, v2fname)

    #define LOAD_TEXTURE2D_X(textureName, unCoord2)                         LOAD_TEXTURE2D(textureName, unCoord2)
    #define LOAD_TEXTURE2D_X_LOD(textureName, unCoord2, lod)                LOAD_TEXTURE2D_LOD(textureName, unCoord2, lod)
    #define SAMPLE_TEXTURE2D_X(textureName, samplerName, coord2)            SAMPLE_TEXTURE2D(textureName, samplerName, coord2)
    #define SAMPLE_TEXTURE2D_X_LOD(textureName, samplerName, coord2, lod)   SAMPLE_TEXTURE2D_LOD(textureName, samplerName, coord2, lod)
    #define GATHER_TEXTURE2D_X(textureName, samplerName, coord2)            GATHER_TEXTURE2D(textureName, samplerName, coord2)
    #define GATHER_RED_TEXTURE2D_X(textureName, samplerName, coord2)        GATHER_RED_TEXTURE2D(textureName, samplerName, coord2)
    #define GATHER_GREEN_TEXTURE2D_X(textureName, samplerName, coord2)      GATHER_GREEN_TEXTURE2D(textureName, samplerName, coord2)
    #define GATHER_BLUE_TEXTURE2D_X(textureName, samplerName, coord2)       GATHER_BLUE_TEXTURE2D(textureName, samplerName, coord2)
    #define GATHER_ALPHA_TEXTURE2D_X(textureName, samplerName, coord2)      GATHER_ALPHA_TEXTURE2D(textureName, samplerName, coord2)
#endif

#ifdef FRAMEBUFFERFETCH_MSAA
#define DEFINE_FRAMEBUFFER_INPUT_HALF(idx) FRAMEBUFFER_INPUT_HALF_MS(idx)
#define FRAMEBUFFER_INPUT_LOAD(idx, sampleID, samplerName) LOAD_FRAMEBUFFER_INPUT_MS(idx, sampleID, samplerName)
#else
#define DEFINE_FRAMEBUFFER_INPUT_HALF(idx)FRAMEBUFFER_INPUT_HALF(idx)
#define FRAMEBUFFER_INPUT_LOAD(idx, sampleID, samplerName) LOAD_FRAMEBUFFER_INPUT(idx, samplerName)
#endif

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

half3 ToneMapping_ACES(half3 color, half adapted_lum)
{
    const half A = half(2.51);
    const half B = half(0.03);
    const half C = half(2.43);
    const half D = half(0.59);
    const half E = half(0.14);

    color *= adapted_lum;
    return saturate((color * (A * color + B)) / (color * (C * color + D) + E));
}

half3 ACES_To_Linear(half3 col)
{
    half3 res = half3(
        dot(half3(half(1.7049), half(-0.62416), half(-0.0809141)), col),
        dot(half3(half(-0.129553), half(1.13837), half(-0.00876801)), col),
        dot(half3(half(-0.0241261), half(-0.124633), half(1.14882)), col)
    );
    return saturate(res);
}

half3 ACES_To_sRGB(half3 col)
{
    return sqrt(ACES_To_Linear(col));
}

half3 ToneMapping_ACES_To_sRGB(half3 color, half adapted_lum)
{
    #if _ACES_C
    // 下面这种得到的颜色更鲜艳，但不容易通过主贴图控制颜色效果
        return ACES_To_sRGB(ToneMapping_ACES(color, adapted_lum));
    #else
        // return sqrt(ToneMapping_ACES_DS(color, adapted_lum));
        // return sqrt(ACES_To_Linear(color * adapted_lum));  
        return ACES_To_sRGB(color * adapted_lum);
    #endif
}

// Ref: http://jcgt.org/published/0003/02/01/paper.pdf "A Survey of Efficient Representations for Independent Unit Vectors"
// Encode with Oct, this function work with any size of output
// return float between [-1, 1]
half2 PackNormalOctQuadEncode(half3 n)
{
    //float l1norm    = dot(abs(n), 1.0);
    //float2 res0     = n.xy * (1.0 / l1norm);

    //float2 val      = 1.0 - abs(res0.yx);
    //return (n.zz < float2(0.0, 0.0) ? (res0 >= 0.0 ? val : -val) : res0);

    // Optimized version of above code:
    n *= rcp(max(dot(abs(n), half(1.0)), half(0.0001)));
    half t = saturate(-n.z);
    return n.xy + half2(n.x >= half(0.0) ? t : -t, n.y >= half(0.0) ? t : -t);
}

inline half2 EncodeViewNormalStereo( half3 n )
{
    half kScale = half(1.7777);
    half2 enc;
    enc = n.xy / (n.z+half(1));
    enc /= kScale;
    enc = enc*half(0.5)+half(0.5);
    return enc;
}
inline half3 DecodeViewNormalStereo( half4 enc4 )
{
    half kScale = half(1.7777);
    half3 nn = enc4.xyz*half3(half(2)*kScale,half(2)*kScale,half(0)) + half3(-kScale,-kScale,half(1));
    half g = half(2.0) / dot(nn.xyz,nn.xyz);
    half3 n;
    n.xy = g*nn.xy;
    n.z = g-half(1);
    return n;
}

inline half2 EncodeFloatRG( float v )
{
    float2 kEncodeMul = float2(1.0, 255.0);
    float kEncodeBit = 1.0/255.0;
    float2 enc = kEncodeMul * v;
    enc = frac (enc);
    enc.x -= enc.y * kEncodeBit;
    return enc;
}
inline float DecodeFloatRG( float2 enc )
{
    float2 kDecodeDot = float2(1.0, 1/255.0);
    return dot( enc, kDecodeDot );
}

inline half4 EncodeDepthNormal( float depth, half3 normal )
{
    half4 enc;
    enc.xy = EncodeViewNormalStereo (normal);
    enc.zw = EncodeFloatRG(depth);
    return enc;
}

inline void DecodeDepthNormal( half4 enc, out float depth, out half3 normal )
{
    depth = DecodeFloatRG (enc.zw);
    normal = DecodeViewNormalStereo (enc);
}

inline half4 EncodeFloatRGBA( float v )
{
    half4 kEncodeMul = half4(half(1.0), half(255.0), half(65025.0), half(16581375.0));
    half kEncodeBit = 1.0/255.0;
    half4 enc = kEncodeMul * v;
    enc = frac (enc);
    enc -= enc.yzww * kEncodeBit;
    return enc;
}
inline float DecodeFloatRGBA( float4 enc )
{
    float4 kDecodeDot = float4(1.0, 1/255.0, 1/65025.0, 1/16581375.0);
    return dot( enc, kDecodeDot );
}

#endif