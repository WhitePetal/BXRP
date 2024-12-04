Shader "DeferredShading"
{
    Properties
    {

    }
    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        // 0 Base: Dirctioal Lighting
        Pass
        {
            Blend One One
            HLSLPROGRAM
            #pragma target 4.5
            #pragma editor_sync_compilation
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile __ DIRECTIONAL_LIGHT
            #pragma multi_compile __ SHADOWS_DIR
            #pragma multi_compile __ FRAMEBUFFERFETCH_MSAA

            // #define _ACES_C 1
            #include "Assets/Shaders/ShaderLibrarys/BXPipelineCommon.hlsl"
            #include "Assets/Shaders/ShaderLibrarys/Lights.hlsl"
            #include "Assets/Shaders/ShaderLibrarys/Shadows.hlsl"
            #include "Assets/Shaders/ShaderLibrarys/PBRFunctions.hlsl"

            DEFINE_FRAMEBUFFER_INPUT_HALF(0); // albedo_roughness
            DEFINE_FRAMEBUFFER_INPUT_HALF(1); // normal_metallic_mask
            DEFINE_FRAMEBUFFER_INPUT_HALF(2); // depth

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv_screen : TEXCOORD0;
                float4 vray : TEXCOORD1;
            };

            v2f vert (uint vertexID : SV_VERTEXID)
            {
                v2f o;

               const float4 vertexs[6] = {
                    float4(1, -1, 0, 1),
                    float4(-1, 1, 0, 1),
                    float4(-1, -1, 0, 1),
                    float4(-1, 1, 0, 1),
                    float4(1, -1, 0, 1),
                    float4(1, 1, 0, 1)
                };
                const float2 uvs[6] = {
                    float2(1, 0),
                    float2(0, 1),
                    float2(0, 0),
                    float2(0, 1),
                    float2(1, 0),
                    float2(1, 1)
                };
                o.vertex = vertexs[vertexID];
                o.uv_screen = uvs[vertexID];
                if(_ProjectionParams.x < 0.0) o.uv_screen.y = 1.0 - o.uv_screen.y;
                o.vray = _ViewPortRays[o.uv_screen.x * 2 + o.uv_screen.y];
                return o;
            }

            half4 frag (v2f i, uint sampleID : SV_SampleIndex) : SV_Target0
            {
                half4 color;
                color.a = half(1.0);
                half3 diffuseLighting = half(0.0);
                half3 specularLighting = half(0.0);

                half4 albedo_roughness = FRAMEBUFFER_INPUT_LOAD(0, sampleID, i.vertex);
                half4 normal_metallic_mask = FRAMEBUFFER_INPUT_LOAD(1, sampleID, i.vertex);
                half3 n = UnpackNormalOctQuadEncode(normal_metallic_mask.xy);
                half oneMinusMetallic = normal_metallic_mask.z;
                half reflectance = normal_metallic_mask.w;

                // #if SHADER_API_METAL
                float4 encodeDepth = FRAMEBUFFER_INPUT_LOAD(2, sampleID, i.vertex);
                float depth = DecodeFloatRGBA(encodeDepth);
                // #else
                // float depth = FRAMEBUFFER_INPUT_LOAD(2, sampleID, i.vertex).x;
                // #endif
                float depthEye = LinearEyeDepth(depth);

                half perceptRoughness = albedo_roughness.a;
                half3 albedo = albedo_roughness.rgb;

                half3 f0 = lerp(albedo, half(0.16) * reflectance, oneMinusMetallic);
                half f90 = half(1.0);
                albedo *= oneMinusMetallic;

                half3 v = normalize(i.vray.xyz);
                v = -v;
                half3 l = _DirectionalLightDirections[0].xyz;

                float3 vPos = i.vray.xyz * depthEye;

                half ndotv = max(half(0.0), dot(n, v));
                half ndotl = max(half(0.0), dot(n, l));

                half3 h = SafeNormalize(l + v);
                half ldoth = max(half(0.0), dot(l, h));
                half ndoth = max(half(0.0), dot(n, h));
                half3 F = F_Schlick(f0, f90, ldoth);
                half Vis = V_SmithGGXCorrelated(ndotv, ndotl, perceptRoughness);
                half D = D_GGX(ndoth, perceptRoughness);
                half atten = GetDirectionalShadow(0, i.vertex.xy, vPos, n, GetShadowDistanceStrength(depthEye));
                half3 lightStrength = _DirectionalLightColors[0].rgb * ndotl * atten;
                diffuseLighting = albedo * Fr_DisneyDiffuse(ndotv, ndotl, ldoth, perceptRoughness) * lightStrength * pi_inv;
                specularLighting = D * F * Vis * lightStrength;
                
                color.rgb = (diffuseLighting + specularLighting) * _ReleateExpourse;
                // color.rgb = 
				return color;
            }
            ENDHLSL
        }

        // 1: Other Light Lighting
        Pass
        {
            Stencil
            {
                Ref 1
                Comp Equal
                Pass Zero
            }
            Blend One One
            HLSLPROGRAM
            #pragma target 5.0
            #pragma editor_sync_compilation
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile __ FRAMEBUFFERFETCH_MSAA
            #pragma multi_compile __ SHADOWS_OTHER

            #define _DEFERRED 1

            // #define _ACES_C 1
            #include "Assets/Shaders/ShaderLibrarys/BXPipelineCommon.hlsl"
            #include "Assets/Shaders/ShaderLibrarys/Lights.hlsl"
            #include "Assets/Shaders/ShaderLibrarys/Shadows.hlsl"
            #include "Assets/Shaders/ShaderLibrarys/PBRFunctions.hlsl"

            DEFINE_FRAMEBUFFER_INPUT_HALF(0); // albedo_roughness
            DEFINE_FRAMEBUFFER_INPUT_HALF(1); // normal_metallic_mask
            DEFINE_FRAMEBUFFER_INPUT_HALF(2); // depth

            CBUFFER_START(_DEFERRED_OTHER_LIGHT)
                int _OtherLightIndex;
            CBUFFER_END

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv_screen : TEXCOORD0;
                float4 vray : TEXCOORD1;
            };

            v2f vert (uint vertexID : SV_VERTEXID)
            {
                v2f o;

               const float4 vertexs[6] = {
                    float4(1, -1, 0, 1),
                    float4(-1, 1, 0, 1),
                    float4(-1, -1, 0, 1),
                    float4(-1, 1, 0, 1),
                    float4(1, -1, 0, 1),
                    float4(1, 1, 0, 1)
                };

                const float2 uvs[6] = {
                    float2(1, 0),
                    float2(0, 1),
                    float2(0, 0),
                    float2(0, 1),
                    float2(1, 0),
                    float2(1, 1)
                };
                o.vertex = vertexs[vertexID];
                o.uv_screen = uvs[vertexID];
                if(_ProjectionParams.x < 0.0) o.uv_screen.y = 1.0 - o.uv_screen.y;
                o.vray = _ViewPortRays[o.uv_screen.x * 2 + o.uv_screen.y];
                return o;
            }

            // [earlydepthstencil]
            half4 frag (v2f i, uint sampleID : SV_SampleIndex) : SV_Target0
            {
                half4 color;
                color.a = half(1.0);
                half4 albedo_roughness = FRAMEBUFFER_INPUT_LOAD(0, sampleID, i.vertex);
                half4 normal_metallic_mask = FRAMEBUFFER_INPUT_LOAD(1, sampleID, i.vertex);
                // #if SHADER_API_METAL
                float4 encodeDepth = FRAMEBUFFER_INPUT_LOAD(2, sampleID, i.vertex);
                float depth = DecodeFloatRGBA(encodeDepth);
                // #else
                // float depth = FRAMEBUFFER_INPUT_LOAD(2, sampleID, i.vertex).x;
                // #endif
                float depthEye = LinearEyeDepth(depth);
                half3 n = UnpackNormalOctQuadEncode(normal_metallic_mask.xy);
                half oneMinusMetallic = normal_metallic_mask.z;
                half reflectance = normal_metallic_mask.w;
                half perceptRoughness = albedo_roughness.a;
                half3 albedo = albedo_roughness.rgb;

                half3 diffuseLighting = half(0.0);
                half3 specularLighting = half(0.0);

                half3 f0 = lerp(albedo, half(0.16) * reflectance * reflectance, oneMinusMetallic);
                half f90 = half(1.0);

                half3 vDir = normalize(i.vray.xyz);
                half3 v = -vDir;
                float3 vPos = i.vray.xyz * depthEye;
                float4 lightPos = _OtherLightSpheres[_OtherLightIndex];
                half3 dir = half3(lightPos.xyz - vPos);
                half3 l = normalize(dir);

                half ndotv = max(half(0.0), dot(n, v));
                half ndotl = max(half(0.0), dot(n, l));
                
                half4 thresholds = _OtherLightThresholds[_OtherLightIndex];
                half dstSqr = dot(dir, dir);
                half atten = dstSqr * thresholds.y;
                atten = max(half(0.0), half(1.0) - atten);
                atten *= atten * rcp(dstSqr + half(0.0001));
                half3 lightFwd = _OtherLightDirections[_OtherLightIndex].xyz;
                half spotAtten = saturate(dot(l, lightFwd) * thresholds.z + thresholds.w);
                atten *= spotAtten * spotAtten;
                atten *= ndotl * GetOtherShadow(_OtherLightIndex, lightFwd, vPos, n);
                half3 lightStrength = _OtherLightColors[_OtherLightIndex].rgb * atten;
                lightStrength *= SampleOtherLightCookie(_OtherLightIndex, vPos);

                half3 h = SafeNormalize(l + v);
                half ldoth = max(half(0.0), dot(l, h));
                half ndoth = max(half(0.0), dot(n, h));
                ndotl = max(half(0.0), ndotl);
                half3 F = F_Schlick(f0, f90, ldoth);
                half Vis = V_SmithGGXCorrelated(ndotv, ndotl, perceptRoughness);
                half D = D_GGX(ndoth, perceptRoughness);
                diffuseLighting = albedo * Fr_DisneyDiffuse(ndotv, ndotl, ldoth, perceptRoughness) * lightStrength * pi_inv;
                specularLighting = D * F * Vis * lightStrength;
                color.rgb = (diffuseLighting + specularLighting) * _ReleateExpourse;
                // color.rgb = lightStrength;
                // return depth;
				return color;
            }
            ENDHLSL
        }
    }
}
