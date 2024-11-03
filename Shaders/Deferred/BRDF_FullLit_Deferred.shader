Shader "Test/BRDF_FullLit_Deferred"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _DiffuseColor("Diffuse Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Reflectance("Reflectance", Range(0, 1)) = 0.5
        [VectorRange(0.0, 8, 0.0, 1.0, 0.0, 1.0, 0.0, 1.0)]_KdsExpoureClampMinMax("光照强度_曝光强度_暗部范围Min_暗部范围Max", Vector) = (1.0, 0.0, 0.0, 1.0)
        [NoScaleOffset]_NormalTex("Normal Map", 2D) = "bump" {}
        [NoScaleOffset]_DetilMask("Detil Mask", 2D) = "white" {}
        _DetilTexA("DetilA", 2D) = "bump" {}
        _DetilTexB("DetilB", 2D) = "bump" {}
        [VectorRange(0.0, 4.0, 0.0, 4.0, 0.0, 4.0)]_NormalScales("主法线强度_细节法线A强度_细节法线B强度", Vector) = (1.5, 1.0, 1.0, 0.0)
        [NoScaleOffset]_MRATex("金属度(R) 粗糙度(G) AO(B) 细节遮罩(A)", 2D) = "white" {}
        [VectorRange(0.0, 1.0, 0.01, 1.0, 0.0, 1.0)]_MetallicRoughnessAO("金属度_粗糙度_AO", Vector) = (1.0, 1.0, 0.5, 0.0)
        [Toggle] _Emission("Emission On", Int) = 0
        [NoScaleOffset]_EmissionMap("Emission RGB:Color A:Mask", 2D) = "black" {}
        _EmissionStrength("Emission Strength", Float) = 1.0
        [Enum(UnityEngine.Rendering.BlendMode)]_SrcBlend("Src Blend", Int) = 0
        [Enum(UnityEngine.Rendering.BlendMode)]_DstBlend("Dst Blend", Int) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry"}

        Pass
        {
            Tags { "LightMode"="BXDeferredBase"}
            Blend [_SrcBlend] [_DstBlend]
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_local __ _EMISSION_ON

            #include "Assets/Shaders/ShaderLibrarys/BXPipelineCommon.hlsl"
            #include "Assets/Shaders/ShaderLibrarys/BakedLights.hlsl"
            #include "Assets/Shaders/ShaderLibrarys/TransformLibrary.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                half3 normal : NORMAL;
                half4 tangent : TANGENT;
                half2 texcoord : TEXCOORD0;
                half2 texcoord1 : TEXCOORD1;
            };

            struct v2f
            {
                half4 uv : TEXCOORD0;
                float4 uv_detil : TEXCOORD1;
                float4 vertex : SV_POSITION;
                float3 pos_world : TEXCOORD2;
                half3 normal_world : TEXCOORD3;
                half3 tangent_world : TEXCOORD4;
                half3 binormal_world : TEXCOORD5;
            };

            struct GBuffer
            {
                half4 albedo_roughness : SV_TARGET0;
                half4 normal_depth : SV_TARGET1;
                half3 indirectLighting : SV_TARGET2;
            };

            UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
                UNITY_DEFINE_INSTANCED_PROP(half, _EmissionStrength)
                UNITY_DEFINE_INSTANCED_PROP(half, _Reflectance)
                UNITY_DEFINE_INSTANCED_PROP(half4, _MainTex_ST)
                UNITY_DEFINE_INSTANCED_PROP(half4, _DetilTexA_ST)
                UNITY_DEFINE_INSTANCED_PROP(half4, _DetilTexB_ST)
                UNITY_DEFINE_INSTANCED_PROP(half4, _DiffuseColor)
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
                o.uv.xy = v.texcoord * GET_PROP(_MainTex_ST).xy + GET_PROP(_MainTex_ST).zw; // main map
                o.uv.zw = v.texcoord1 * unity_LightmapST.xy + unity_LightmapST.zw;
                o.uv_detil = float4(v.texcoord * GET_PROP(_DetilTexA_ST).xy + GET_PROP(_DetilTexA_ST).zw, v.texcoord * GET_PROP(_DetilTexB_ST).xy + GET_PROP(_DetilTexB_ST).zw); // detil map
                o.normal_world = TransformObjectToWorldNormal(v.normal);
                o.tangent_world = TransformObjectToWorldDir(v.tangent.xyz);
                o.binormal_world = cross(o.normal_world, o.tangent_world) * v.tangent.w * unity_WorldTransformParams.w;
                return o;
            }

            GBuffer frag (v2f i)
            {
                GBuffer gbuffer;
                half4 mainTex = _MainTex.Sample(sampler_MainTex, i.uv.xy);
                half4 normalMap = _NormalTex.Sample(sampler_NormalTex, i.uv.xy);
                half4 MRA = _MRATex.Sample(sampler_MainTex, i.uv.xy);
                half2 detilMask = _DetilMask.Sample(sampler_MainTex, i.uv.xy).rg;
                #ifdef _EMISSION_ON
                half4 emission = _EmissionMap.Sample(sampler_MainTex, i.uv.xy);
                #endif
                half4 detilA = _DetilTexA.Sample(sampler_NormalTex, i.uv_detil.xy);
                half4 detilB = _DetilTexB.Sample(sampler_NormalTex, i.uv_detil.zw);

                half4 normalScales = GET_PROP(_NormalScales);
                half3 n = GetBlendNormalWorldFromMapAB(i.tangent_world, i.binormal_world, i.normal_world, normalMap, detilA, detilB, normalScales.x, normalScales.y, normalScales.z, detilMask);
                half3 n_view = TransformWorldToViewDir(n);

                half4 mraValues = GET_PROP(_MetallicRoughnessAO);
                half roughness = mraValues.y * MRA.g;
                half perceptRoughness = roughness * roughness;
                half oneMinusMetallic = half(1.0) - MRA.r * mraValues.x;
                half ao = (half(1.0) - (half(1.0) - MRA.b) * mraValues.z);
                half3 albedo = mainTex.rgb * GET_PROP(_DiffuseColor).rgb;
                // albedo *= oneMinusMetallic;
                half reflectance = GET_PROP(_Reflectance);

                float depth = Linear01Depth(i.vertex.z);

                half3 ambient = half(0.0);
                #ifdef LIGHTMAP_ON
                ambient = SampleLightMap(i.uv.zw) * albedo;
                #endif
                #ifdef _EMISSION_ON
                emission.rgb *= emission.a * GET_PROP(_EmissionStrength);
                #endif
                gbuffer.albedo_roughness = half4(albedo, roughness);
                gbuffer.normal_depth = half4(EncodeViewNormalStereo(n_view), oneMinusMetallic, reflectance * reflectance);
                gbuffer.indirectLighting = 
                    ambient * ao
                    #ifdef _EMISSION_ON
                    + emission.rgb
                    #endif
                ;
                
                return gbuffer;
            }
            ENDHLSL
        }

        Pass 
        {
			Tags {"RenderType"="Opaque" "Queue"="Geometry" "LightMode" = "ShadowCaster"}
            
			HLSLPROGRAM
			#pragma target 4.5
			#pragma vertex vert
			#pragma fragment frag

            #include "Assets/Shaders/ShaderLibrarys/BXPipelineCommon.hlsl"
            #include "Assets/Shaders/ShaderLibrarys/Lights.hlsl"
            #include "Assets/Shaders/ShaderLibrarys/Shadows.hlsl"
			
            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };


            v2f vert (appdata v)
            {
                v2f o;
                float3 pos_world = TransformObjectToWorld(v.vertex.xyz);
                o.vertex = TransformWorldToHClip(pos_world);
                if(_ShadowPancaking)
                {
                    #if UNITY_REVERSED_Z
                        o.vertex.z =
                        min(o.vertex.z, o.vertex.w * UNITY_NEAR_CLIP_VALUE);
                    #else
                        o.vertex.z =
                            max(o.vertex.z, o.vertex.w * UNITY_NEAR_CLIP_VALUE);
                    #endif
                }
                return o;
            }

            void frag (v2f i)
            {
                // return 0.0;
            }
			ENDHLSL
		}

        // Meta
        Pass
        {
            Name "META_BAKERY"
            Tags {"LightMode"="Meta"}
            Cull Off
            HLSLPROGRAM

            #pragma target 4.5
            #pragma multi_compile_local __ _EMISSION_ON
            #pragma shader_feature EDITOR_VISUALIZATION
            #include "Assets/Shaders/ShaderLibrarys/BXPipelineCommon.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/MetaPass.hlsl"

            // Include Bakery meta pass
            #include "Assets/Scripts/BXRenderPipeline/Bakery/BakeryMetaPass.cginc"

            Texture2D _MainTex, _MRATex, _EmissionMap;
            Texture2D<half4> _NormalTex, _DetilTexA, _DetilTexB, _DetilMask;
            SamplerState sampler_NormalTex;
            SamplerState sampler_MainTex;
            CBUFFER_START(UnityPerMaterial)
                half _EmissionStrength;
                half _Reflectance;
                half4 _MainTex_ST;
                half4 _DetilTexA_ST;
                half4 _DetilTexB_ST;
                half4 _DiffuseColor;
                half4 _MetallicRoughnessAO;
                half4 _NormalScales;
                half4 _KdsExpoureClampMinMax;
            CBUFFER_END

            struct a2v
            {
                float4 vertex : POSITION;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                half2 uv : TEXCOORD0;
                float4 uv_detil : TEXCOORD1;
                float3 pos_world : TEXCOORD2;
                half3 normal_world : TEXCOORD3;
                half3 tangent_world : TEXCOORD4;
                half3 binormal_world : TEXCOORD5;
            };

            #include "Assets/Shaders/ShaderLibrarys/TransformLibrary.hlsl"

            v2f vert_customeMeta(a2v v)
            {
                v2f o;
                o.pos = float4(((v.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw)*2-1) * float2(1,-1), 0.5, 1);
                o.pos_world = 0.0;
                o.uv = v.uv0 * _MainTex_ST.xy + _MainTex_ST.zw;
                o.uv_detil = float4(v.uv0 * _DetilTexA_ST.xy + _DetilTexA_ST.zw, v.uv0 * _DetilTexB_ST.xy + _DetilTexB_ST.zw); // detil map
                o.normal_world = TransformObjectToWorldNormal(v.normal);
                o.tangent_world = TransformObjectToWorldDir(v.tangent.xyz);
                o.binormal_world = cross(o.normal_world, o.tangent_world) * v.tangent.w;
                return o;
            }

            float4 frag_customMeta (v2f i): SV_Target
            {	
                UnityMetaInput o;
                o = (UnityMetaInput)0;
                // UNITY_INITIALIZE_OUTPUT(UnityMetaInput, o);

                half4 mainTex = _MainTex.Sample(sampler_MainTex, i.uv.xy);
                if (unity_MetaFragmentControl.w)
                {
                    return mainTex.a;
                }

                half4 MRA = _MRATex.Sample(sampler_MainTex, i.uv.xy);

                half4 mraValues = _MetallicRoughnessAO;
                half roughness = mraValues.y * MRA.g;
                half perceptRoughness = roughness * roughness;
                half oneMinusMetallic = half(1.0) - MRA.r * mraValues.x;
                half ao = (half(1.0) - (half(1.0) - MRA.b) * mraValues.z);
                half3 albedo = mainTex.rgb * _DiffuseColor.rgb;
                half reflectance = _Reflectance;
                half3 f0 = lerp(albedo, half(0.16)*reflectance*reflectance, oneMinusMetallic);
                half f90 = half(1.0);
                // albedo = albedo - f0;
                albedo *= oneMinusMetallic;

                #ifdef _EMISSION_ON
                    float4 emission = 0.0;
                    emission = _EmissionMap.Sample(sampler_MainTex, i.uv.xy);
                    emission.rgb = emission.rgb * _EmissionStrength * emission.a;
                    o.Emission = emission.rgb;
                #else
                    o.Emission = 0.0;
                #endif

                o.Albedo = albedo;
                // o.SpecularColor = specular;

                // Output custom normal to use with Bakery's "Baked Normal Map" mode
                if (unity_MetaFragmentControl.z)
                {
                    // Calculate custom normal
                    half4 normalMap = _NormalTex.Sample(sampler_NormalTex, i.uv.xy);
                    half2 detilMask = _DetilMask.Sample(sampler_MainTex, i.uv.xy).rg;
                    half4 detilA = _DetilTexA.Sample(sampler_NormalTex, i.uv_detil.xy);
                    half4 detilB = _DetilTexB.Sample(sampler_NormalTex, i.uv_detil.zw);
   
                    half4 normalScales = _NormalScales;
                    half3 n = GetBlendNormalWorldFromMapAB(i.tangent_world, i.binormal_world, i.normal_world, normalMap, detilA, detilB, normalScales.x, normalScales.y, normalScales.z, detilMask);
                    return float4(EncodeNormalBestFit(n), 1.0);
                }

                return UnityMetaFragment(o);
            }

            // Must use vert_bakerymt vertex shader
            #pragma vertex vert_customeMeta
            #pragma fragment frag_customMeta
            ENDHLSL
        }
    }
}
