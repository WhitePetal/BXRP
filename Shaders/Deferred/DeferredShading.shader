Shader "DeferredShading"
{
    Properties
    {

    }
    SubShader
    {
        Cull Back
        ZWrite Off
        ZTest Always

        // _MS means MSAA

        // 0 Base: Dirctioal Lighting + Indirect Lighting for none msaa
        Pass
        {
            HLSLPROGRAM
            #pragma target 4.5
            #pragma editor_sync_compilation
            #pragma vertex vert
            #pragma fragment frag

            // #define _ACES_C 1
            #include "Assets/Shaders/ShaderLibrarys/BXPipelineCommon.hlsl"
            #include "Assets/Shaders/ShaderLibrarys/Lights.hlsl"
            #include "Assets/Shaders/ShaderLibrarys/Shadows.hlsl"
            #include "Assets/Shaders/ShaderLibrarys/PBRFunctions.hlsl"

            FRAMEBUFFER_INPUT_HALF(0); // albedo_roughness
            FRAMEBUFFER_INPUT_HALF(1); // dept_normal
            FRAMEBUFFER_INPUT_HALF(2); // indirect

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

            float4 _ViewSpaceConvert;

            half4 frag (v2f i) : SV_Target0
            {
                half4 color;
                color.a = half(1.0);
                half4 albedo_roughness = LOAD_FRAMEBUFFER_INPUT(0, i.vertex);
                half4 depth_normal = LOAD_FRAMEBUFFER_INPUT(1, i.vertex);
                half3 indirect = LOAD_FRAMEBUFFER_INPUT(2, i.vertex).rgb;
                float depth;
                half3 n;
                DecodeDepthNormal(depth_normal, depth, n);

                half perceptRoughness = albedo_roughness.a;
                half3 albedo = albedo_roughness.rgb;

                half metallicApprox = smoothstep(0.0,  0.5, perceptRoughness);
                half3 f0 = lerp(half(0.16), half(0.0), metallicApprox);
                half f90 = half(1.0);

                half3 v = normalize(i.vray);
                v = -v;
                half3 l = _DirectionalLightDirections[0].xyz;

                half ndotv = max(half(0.0), dot(n, v));
                half ndotl = max(half(0.0), dot(n, l));

                half3 h = SafeNormalize(l + v);
                half ldoth = max(half(0.0), dot(l, h));
                half ndoth = max(half(0.0), dot(n, h));
                half3 F = F_Schlick(f0, f90, ldoth);
                half Vis = V_SmithGGXCorrelated(ndotv, ndotl, perceptRoughness);
                half D = D_GGX(ndoth, perceptRoughness);
                half3 lightStrength = _DirectionalLightColors[0].rgb * ndotl;
                half3 diffuseLighting = Fr_DisneyDiffuse(ndotv, ndotl, ldoth, perceptRoughness) * lightStrength;
                half3 specularLighting = D * F * Vis * lightStrength;
                color.rgb = (albedo * diffuseLighting * pi_inv + specularLighting + indirect) * _ReleateExpourse;
				return color;
            }
            ENDHLSL
        }

        // 1 Base: Dirctioal Lighting + Indirect Lighting for msaa
        Pass
        {
            HLSLPROGRAM
            #pragma target 4.5
            #pragma editor_sync_compilation
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile __ DIRECTIONAL_LIGHT

            // #define _ACES_C 1
            #include "Assets/Shaders/ShaderLibrarys/BXPipelineCommon.hlsl"
            #include "Assets/Shaders/ShaderLibrarys/Lights.hlsl"
            #include "Assets/Shaders/ShaderLibrarys/Shadows.hlsl"
            #include "Assets/Shaders/ShaderLibrarys/PBRFunctions.hlsl"

            FRAMEBUFFER_INPUT_HALF_MS(0); // albedo_roughness
            FRAMEBUFFER_INPUT_HALF_MS(1); // dept_normal
            FRAMEBUFFER_INPUT_HALF_MS(2); // indirect

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
                half3 indirect = LOAD_FRAMEBUFFER_INPUT_MS(2, sampleID, i.vertex).rgb;
                half3 diffuseLighting = half(0.0);
                half3 specularLighting = half(0.0);
                #ifdef DIRECTIONAL_LIGHT
                    half4 albedo_roughness = LOAD_FRAMEBUFFER_INPUT_MS(0, sampleID, i.vertex);
                    half4 depth_normal = LOAD_FRAMEBUFFER_INPUT_MS(1, sampleID, i.vertex);
                    half3 n = DecodeViewNormalStereo(depth_normal);
                    half oneMinusMetallic = depth_normal.z;
                    half reflectance = depth_normal.w;

                    half perceptRoughness = albedo_roughness.a;
                    half3 albedo = albedo_roughness.rgb;

                    half metallicApprox = smoothstep(0.0,  0.5, perceptRoughness);
                    half3 f0 = lerp(albedo, half(0.16) * reflectance, oneMinusMetallic);
                    half f90 = half(1.0);
                    albedo *= oneMinusMetallic;

                    half3 v = normalize(i.vray);
                    v = -v;
                    half3 l = _DirectionalLightDirections[0].xyz;

                    half ndotv = max(half(0.0), dot(n, v));
                    half ndotl = max(half(0.0), dot(n, l));

                    half3 h = SafeNormalize(l + v);
                    half ldoth = max(half(0.0), dot(l, h));
                    half ndoth = max(half(0.0), dot(n, h));
                    half3 F = F_Schlick(f0, f90, ldoth);
                    half Vis = V_SmithGGXCorrelated(ndotv, ndotl, perceptRoughness);
                    half D = D_GGX(ndoth, perceptRoughness);
                    half3 lightStrength = _DirectionalLightColors[0].rgb * ndotl;
                    diffuseLighting = albedo * Fr_DisneyDiffuse(ndotv, ndotl, ldoth, perceptRoughness) * lightStrength * pi_inv;
                    specularLighting = D * F * Vis * lightStrength;
                #endif
                color.rgb = (diffuseLighting + specularLighting + indirect) * _ReleateExpourse;
				return color;
            }
            ENDHLSL
        }
    }
}
