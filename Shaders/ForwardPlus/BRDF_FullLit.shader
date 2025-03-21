Shader "Test/BRDF_FullLit"
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
        [Enum(UnityEngine.Rendering.BlendMode)]_SrcBlend("Src Blend", Int) = 1
        [Enum(UnityEngine.Rendering.BlendMode)]_DstBlend("Dst Blend", Int) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry"}

        Pass
        {
            Tags { "LightMode"="BXForwardBase"}
            Blend [_SrcBlend] [_DstBlend]
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile __ DIRECTIONAL_LIGHT
            #pragma multi_compile __ CLUSTER_LIGHT
            #pragma multi_compile __ SHADOWS_DIR
            #pragma multi_compile __ SHADOWS_OTHER
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_local __ _EMISSION_ON
            #pragma multi_compile _ENVIRONMENTREFLECTIONS_OFF _ENVIRONMENTREFLECTIONS_ON
            #pragma multi_compile_fragment __ _CLUSTER_GREATE_32

            #include "Assets/Shaders/ShaderLibrarys/BXPipelineCommon.hlsl"
            #include "Assets/Shaders/ShaderLibrarys/Lights.hlsl"
            #include "Assets/Shaders/ShaderLibrarys/Shadows.hlsl"
            #include "Assets/Shaders/ShaderLibrarys/BakedLights.hlsl"
            #include "Assets/Shaders/ShaderLibrarys/TransformLibrary.hlsl"
            #include "Assets/Shaders/ShaderLibrarys/PBRFunctions.hlsl"

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

            half4 frag (v2f i) : SV_Target
            {
                half4 mainTex = _MainTex.Sample(sampler_MainTex, i.uv.xy);
                half4 normalMap = _NormalTex.Sample(sampler_NormalTex, i.uv.xy);
                half4 MRA = _MRATex.Sample(sampler_MainTex, i.uv.xy);
                half2 detilMask = _DetilMask.Sample(sampler_MainTex, i.uv.xy).rg;
                #ifdef _EMISSION_ON
                half4 emission = _EmissionMap.Sample(sampler_MainTex, i.uv.xy);
                #endif
                half4 detilA = _DetilTexA.Sample(sampler_NormalTex, i.uv_detil.xy);
                half4 detilB = _DetilTexB.Sample(sampler_NormalTex, i.uv_detil.zw);

                float3 vSource = _WorldSpaceCameraPos.xyz - i.pos_world;
                half3 v = normalize(vSource);
                half4 normalScales = GET_PROP(_NormalScales);
                half3 n = GetBlendNormalWorldFromMapAB(i.tangent_world, i.binormal_world, i.normal_world, normalMap, detilA, detilB, normalScales.x, normalScales.y, normalScales.z, detilMask);

                half4 mraValues = GET_PROP(_MetallicRoughnessAO);
                half roughness = mraValues.y * MRA.g;
                half perceptRoughness = roughness * roughness;
                half oneMinusMetallic = half(1.0) - MRA.r * mraValues.x;
                half ao = (half(1.0) - (half(1.0) - MRA.b) * mraValues.z);
                half3 albedo = mainTex.rgb * GET_PROP(_DiffuseColor).rgb;
                half reflectance = GET_PROP(_Reflectance);
                half3 f0 = lerp(albedo, half(0.16)*reflectance*reflectance, oneMinusMetallic);
                half f90 = half(1.0);
                // albedo = albedo - f0;
                albedo *= oneMinusMetallic;

                float depthEye = LinearEyeDepth(i.vertex.z);
                half4 kds = GET_PROP(_KdsExpoureClampMinMax);
                half3 diffuseLighting = half(0.0);
                half3 specularLighting = half(0.0);
                float3 pos_world = i.pos_world;
                half ndotv = max(half(0.0), dot(n, v));
                #ifdef DIRECTIONAL_LIGHT
                    for(int lightIndex = 0; lightIndex < _DirectionalLightCount; ++lightIndex)
                    {
                        if(ndotv <= half(0.0)) break;
                        half3 l = _DirectionalLightDirections[lightIndex].xyz;
                        half ndotl = smoothstep(kds.z, kds.w, dot(n, l) + kds.y);
                        if(ndotl <= half(0.0)) continue;

                        half atten = GetDirectionalShadow(lightIndex, i.vertex.xy, pos_world, n, GetShadowDistanceStrength(depthEye));
                        half3 h = SafeNormalize(l + v);
                        half ldoth = max(half(0.0), dot(l, h));
                        half ndoth = max(half(0.0), dot(n, h));
                        ndotl = saturate(ndotl);
                        half3 F = F_Schlick(f0, f90, ldoth);
                        half Vis = V_SmithGGXCorrelated(ndotv, ndotl, perceptRoughness);
                        half D = D_GGX(ndoth, perceptRoughness);
                        half3 lightStrength = _DirectionalLightColors[lightIndex].rgb * kds.x * ndotl * atten;
                        diffuseLighting += Fr_DisneyDiffuse(ndotv, ndotl, ldoth, perceptRoughness) * lightStrength;
                        specularLighting += D * F * Vis * lightStrength;
                    }
                #endif
                // uint clusterCount = 0;
                #ifdef CLUSTER_LIGHT
                    float2 uvClip = i.vertex.xy / _ScreenParams.xy;
                    // uvClip.y = 1.0 - uvClip;
                    LIGHT_LOOP_BEGIN(uvClip, -vSource)
                        float4 lightPos = _OtherLightSpheres[lightIndex];
                        // return half4(lightIndex.xxxx);
                        // clusterCount++;
                        half3 dir = half3(lightPos.xyz - pos_world);
                        half3 l = normalize(dir);
                        half ndotl = smoothstep(kds.z, kds.w, dot(n, l) + kds.y);
                        if(ndotl <= half(0.0)) continue;
                        
                        half4 thresholds = _OtherLightThresholds[lightIndex];
                        half dstSqr = dot(dir, dir);
                        half atten = dstSqr * thresholds.y;
                        atten = max(half(0.0), half(1.0) - atten);
                        atten *= atten * rcp(dstSqr + half(0.0001));
                        half3 lightFwd = _OtherLightDirections[lightIndex].xyz;
                        half spotAtten = saturate(dot(l, lightFwd) * thresholds.z + thresholds.w);
                        atten *= spotAtten * spotAtten;
                        atten *= ndotl * GetOtherShadow(lightIndex, lightFwd, pos_world, n);
                        half3 lightStrength = _OtherLightColors[lightIndex].rgb * kds.x * atten;
                        lightStrength *= SampleOtherLightCookie(lightIndex, pos_world);

                        half3 h = SafeNormalize(l + v);
                        half ldoth = max(half(0.0), dot(l, h));
                        half ndoth = max(half(0.0), dot(n, h));
                        ndotl = max(half(0.0), ndotl);
                        half3 F = F_Schlick(f0, f90, ldoth);
                        half Vis = V_SmithGGXCorrelated(ndotv, ndotl, perceptRoughness);
                        half D = D_GGX(ndoth, perceptRoughness);
                        diffuseLighting += Fr_DisneyDiffuse(ndotv, ndotl, ldoth, perceptRoughness) * lightStrength;
                        specularLighting += D * F * Vis * lightStrength;
                    LIGHT_LOOP_END
                #endif
                // return clusterCount * 0.2 + mainTex.a * 0.0001;
                half3 ambient = half(0.0);
                #ifdef LIGHTMAP_ON
                ambient = SampleLightMap(i.uv.zw);
                #endif
                ambient += SampleSH(n);
                half3 F_ambient = F_Schlick(f0, f90, ndotv);
                half3 ambientSpecular = SampleEnvironment(i.vertex.xyz, vSource, pos_world, v, n, perceptRoughness) * F_ambient / (perceptRoughness + half(1.0));
                #ifdef _EMISSION_ON
                emission.rgb *= emission.a * GET_PROP(_EmissionStrength) * ndotv;
                #endif
                return half4(
                    (
                        diffuseLighting * albedo * pi_inv + 
                        specularLighting + 
                        (ambient * albedo /** + ambientSpecular **/) * ao
                        #ifdef _EMISSION_ON
                        + emission.rgb
                        #endif
                    ) * _ReleateExpourse, mainTex.a * GET_PROP(_DiffuseColor).a);
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
