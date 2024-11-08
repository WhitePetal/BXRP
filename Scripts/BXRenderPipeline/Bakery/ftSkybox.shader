// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Bakery/Skybox" {
Properties {
    _Tint ("Tint Color", Color) = (.5, .5, .5, .5)
    _Exposure ("Exposure", Float) = 1.0
    _MatrixRight ("Right", Vector) = (1, 0, 0, 0)
    _MatrixUp ("Up", Vector) = (0, 1, 0, 0)
    _MatrixForward ("Forward", Vector) = (0, 0, 1, 0)
    [NoScaleOffset] _Tex ("Cubemap   (HDR)", Cube) = "white" {}
    _NoTexture ("No texture", Float) = 0.0
    _Hemispherical ("Hemispherical", Float) = 0.0
}

SubShader {
    Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
    Cull Off ZWrite Off

    Pass {
        // Tags { "LightMode"="BXDeferredBase"}
        // Blend [_SrcBlend] [_DstBlend]
        HLSLPROGRAM
        #pragma target 4.5
        #pragma vertex vert
        #pragma fragment frag
        #include "Assets/Shaders/ShaderLibrarys/BXPipelineCommon.hlsl"

        TextureCube _Tex;
        SamplerState sampler_Tex;
        half4 _Tex_HDR;
        half4 _Tint;
        half _Exposure;
        float3 _MatrixRight, _MatrixUp, _MatrixForward;
        float4x4 reflection2World;
        float _NoTexture, _Hemispherical;

        struct appdata_t {
            float4 vertex : POSITION;
        };

        struct v2f {
            float4 vertex : SV_POSITION;
            float3 texcoord : TEXCOORD0;
        };

        struct GBuffer
        {
            half4 lighting : SV_TARGET0;
            half4 albedo_roughness : SV_TARGET1;
            half4 normal_metallic_mask : SV_TARGET2;
            #if SHADER_API_METAL
            half4 depth_metal : SV_TARGET3;
            #endif
        };

        v2f vert (appdata_t v)
        {
            v2f o;
            float3x3 tform = float3x3(_MatrixRight, _MatrixUp, _MatrixForward);
            float3 pos = mul(tform, v.vertex.xyz);
            o.vertex = TransformObjectToHClip(pos);
            o.texcoord = mul((float3x3)reflection2World, v.vertex.xyz);
            return o;
        }

        GBuffer frag (v2f i)
        {
            GBuffer o;
            half4 tex = _NoTexture < 0.5 ? _Tex.Sample(sampler_Tex, i.texcoord) : half4(1,1,1,1);
            half3 c = DecodeHDR (tex, _Tex_HDR);
            // if (unity_ColorSpaceDouble.x < 3) c = pow(c, 2.2f);
            c = c * _Tint.rgb;
            c *= _Exposure;

            if (_Hemispherical > 0.0f) c *= i.texcoord.y < 0 ? 0 : 1;
            // if (unity_ColorSpaceDouble.x < 3) c = pow(c, 1/2.2f);
            o.lighting = half4(c, half(1.0));
            o.albedo_roughness = half(0.0);
            o.normal_metallic_mask = half(0.0);
            o.depth_metal = half(0.0);


            return o;
        }
        ENDHLSL
    }
}


Fallback Off

}
