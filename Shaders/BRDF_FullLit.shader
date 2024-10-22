Shader "Test/BRDF_FullLit"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _DiffuseColor("Diffuse Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Fresnel("Fresnel0", Color) = (0.09, 0.09, 0.09, 1)
        [VectorRange(0.0, 8, 0.0, 1.0, 0.0, 1.0, 0.0, 1.0)]_KdsExpoureClampMinMax("光照强度_曝光强度_暗部范围Min_暗部范围Max", Vector) = (1.0, 0.0, 0.0, 1.0)
        [NoScaleOffset]_NormalTex("Normal Map", 2D) = "bump" {}
        [NoScaleOffset]_DetilMask("Detil Mask", 2D) = "white" {}
        _DetilTexA("DetilA", 2D) = "bump" {}
        _DetilTexB("DetilB", 2D) = "bump" {}
        [VectorRange(0.0, 4.0, 0.0, 4.0, 0.0, 4.0)]_NormalScales("主法线强度_细节法线A强度_细节法线B强度", Vector) = (1.5, 1.0, 1.0, 0.0)
        [NoScaleOffset]_MRATex("金属度(R) 粗糙度(G) AO(B) 细节遮罩(A)", 2D) = "white" {}
        [VectorRange(0.0, 1.0, 0.01, 1.0, 0.0, 1.0)]_MetallicRoughnessAO("金属度_粗糙度_AO", Vector) = (1.0, 1.0, 0.5, 0.0)
        [NoScaleOffset]_EmissionMap("Emission RGB:Color A:Mask", 2D) = "black" {}
        _EmissionStrength("Emission Strength", Range(0.0, 10.0)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry"}

        Pass
        {
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile __ DIRECTIONAL_LIGHT
            #pragma multi_compile __ CLUSTER_LIGHT
            #pragma multi_compile __ SHADOWS_DIR
            #pragma multi_compile __ SHADOWS_CLUSTER

            #include "Assets/Shaders/ShaderLibrarys/BXPipelineCommon.hlsl"
            #include "Assets/Shaders/ShaderLibrarys/Lights.hlsl"
            #include "Assets/Shaders/ShaderLibrarys/Shadows.hlsl"
            #include "Assets/Shaders/ShaderLibrarys/TransformLibrary.hlsl"
            #include "Assets/Shaders/ShaderLibrarys/PBRFunctions.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                half3 normal : NORMAL;
                half4 tangent : TANGENT;
                half2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 uv_detil : TEXCOORD1;
                float4 vertex : SV_POSITION;
                float3 pos_world : TEXCOORD2;
                half3 normal_world : TEXCOORD3;
                half3 tangent_world : TEXCOORD4;
                half3 binormal_world : TEXCOORD5;
            };

            UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
                UNITY_DEFINE_INSTANCED_PROP(half, _EmissionStrength)
                UNITY_DEFINE_INSTANCED_PROP(half4, _MainTex_ST)
                UNITY_DEFINE_INSTANCED_PROP(half4, _DetilTexA_ST)
                UNITY_DEFINE_INSTANCED_PROP(half4, _DetilTexB_ST)
                UNITY_DEFINE_INSTANCED_PROP(half4, _DiffuseColor)
                UNITY_DEFINE_INSTANCED_PROP(half4, _Fresnel)
                UNITY_DEFINE_INSTANCED_PROP(half4, _MetallicRoughnessAO)
                UNITY_DEFINE_INSTANCED_PROP(half4, _NormalScales)
                UNITY_DEFINE_INSTANCED_PROP(half4, _KdsExpoureClampMinMax)
            UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

            Texture2D<half4> _MainTex, _MRATex, _DetilMask;
            SamplerState sampler_MainTex;
            Texture2D<half4> _NormalTex, _DetilTexA, _DetilTexB;
            SamplerState sampler_NormalTex;
            Texture2D<half4> _EmissionMap;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos_world = TransformObjectToWorld(v.vertex.xyz);
                o.vertex = mul(UNITY_MATRIX_VP, float4(o.pos_world, 1.0));
                o.uv = v.texcoord * GET_PROP(_MainTex_ST).xy + GET_PROP(_MainTex_ST).zw; // main map
                o.uv_detil = float4(v.texcoord * GET_PROP(_DetilTexA_ST).xy + GET_PROP(_DetilTexA_ST).zw, v.texcoord * GET_PROP(_DetilTexB_ST).xy + GET_PROP(_DetilTexB_ST).zw); // detil map
                o.normal_world = TransformObjectToWorldNormal(v.normal);
                o.tangent_world = TransformObjectToWorldDir(v.tangent.xyz);
                o.binormal_world = cross(o.normal_world, o.tangent_world) * v.tangent.w * unity_WorldTransformParams.w;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 mainTex = _MainTex.Sample(sampler_MainTex, i.uv);
                half4 normalMap = _NormalTex.Sample(sampler_NormalTex, i.uv);
                half4 MRA = _MRATex.Sample(sampler_MainTex, i.uv);
                half2 detilMask = _DetilMask.Sample(sampler_MainTex, i.uv).rg;
                half4 emission = _EmissionMap.Sample(sampler_MainTex, i.uv);
                half4 detilA = _DetilTexA.Sample(sampler_NormalTex, i.uv_detil.xy);
                half4 detilB = _DetilTexB.Sample(sampler_NormalTex, i.uv_detil.zw);

                half3 v = _WorldSpaceCameraPos.xyz - i.pos_world;
                v = normalize(v);
                half4 normalScales = GET_PROP(_NormalScales);
                half3 n = GetBlendNormalWorldFromMapAB(i.tangent_world, i.binormal_world, i.normal_world, normalMap, detilA, detilB, normalScales.x, normalScales.y, normalScales.z, detilMask);
                half ndotv = max(half(0.0), dot(n, v));
                half fnv = PBR_F(ndotv);

                half4 mraValues = GET_PROP(_MetallicRoughnessAO);
                half roughness = mraValues.y * MRA.g;
                half oneMinusMetallic = max(half(1.0) - MRA.r * mraValues.x, half(0.2));
                half ao = (half(1.0) - (half(1.0) - MRA.b) * mraValues.z);
                half3 albedo = mainTex.rgb * GET_PROP(_DiffuseColor).rgb;
                half3 specular = lerp(albedo, GET_PROP(_Fresnel).rgb, oneMinusMetallic);
                albedo *= oneMinusMetallic;

                float depthEye = LinearEyeDepth(i.vertex.z);
                half4 kds = GET_PROP(_KdsExpoureClampMinMax);
                half3 diffuseLighting = half(0.0);
                half3 specularLighting = half(0.0);
                #ifdef DIRECTIONAL_LIGHT
                    for(int lightIndex = 0; lightIndex < _DirectionalLightCount; ++lightIndex)
                    {
                        half atten = GetDirectionalShadow(0, i.vertex.xy, i.pos_world, n, GetShadowDistanceStrength(depthEye));
                        half3 l = _DirectionalLightDirections[lightIndex].xyz;
                        half3 h = SafeNormalize(l + v);
                        half ldoth = max(half(0.0), dot(l, h));
                        half ndotlm = max(half(0.0), dot(n, l)) + kds.y;
                        half ndotl = smoothstep(kds.z, kds.w, min(ndotlm, atten));
                        half ndoth = max(half(0.0), dot(n, h));
                        half2 fgd = half2(PBR_F(ldoth), PBR_G(ndotl, ndotv, roughness) * PBR_D(roughness, ndoth));
                        half3 fresnel = specular + (half(1.0) - specular) * fgd.x;
                        half3 lightStrength = _DirectionalLightColors[lightIndex].rgb * kds.x * ndotl;
                        diffuseLighting += (half(1.0) - fresnel) * albedo * lightStrength;
                        specularLighting += fresnel * fgd.y * lightStrength;
                    }
                #endif

                return half4(diffuseLighting + specularLighting, 1.0);
            }
            ENDHLSL
        }
    }
}
