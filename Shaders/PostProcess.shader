Shader "PostProcess"
{
    Properties
    {

    }
    SubShader
    {
        Cull Back
        ZWrite Off
        ZTest Always

        // 0 Final Combine
        Pass
        {
            HLSLPROGRAM
            #pragma target 4.5
            #pragma editor_sync_compilation
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local_fragment __ _BLOOM

            // #define _ACES_C 1
            #include "Assets/Shaders/ShaderLibrarys/BXPipelineCommon.hlsl"

            Texture2D _PostProcessInput;
			float4 _PostProcessInput_TexelSize;
			SamplerState sampler_PostProcessInput;

            #include "Assets/Shaders/ShaderLibrarys/GSR.hlsl"

            #ifdef _BLOOM
            Texture2D<half4> _BloomTex;
            half2 _BloomStrength;
            #endif

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (uint vertexID : SV_VERTEXID)
            {
                v2f o;

				if(_ProjectionParams.x < 0.0)
				{
					o.vertex = float4(
						vertexID <= 1 ? -1.0 : 3.0,
						vertexID == 0 ? 3.0 : -1.0,
						0.0, 1.0
					);
					o.uv = float2(
						vertexID <= 1 ? 0.0 : 2.0,
						vertexID == 0 ? 2.0 : 0.0
					);
					o.uv.y = 1.0 - o.uv.y;
				}
				else
				{
					o.vertex = float4(
						vertexID <= 1 ? -1.0 : 3.0,
						vertexID == 1 ? 3.0 : -1.0,
						0.0, 1.0
					);
					o.uv = float2(
						vertexID <= 1 ? 0.0 : 2.0,
						vertexID == 1 ? 2.0 : 0.0
					);
				}
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 color = half4(half(0.0), half(0.0), half(0.0), half(1.0));
				SgsrYuvH(color, i.uv, _PostProcessInput_TexelSize);
                #ifdef _BLOOM
                half3 bloomAdd = _BloomTex.SampleLevel(sampler_PostProcessInput, i.uv, 0).rgb;
				half bright = saturate(half(0.3) * color.g + half(0.59) * color.r + half(0.11) * color.b);
				bloomAdd *= lerp(1.0, _BloomStrength.y, bright);
				color.rgb += bloomAdd;
                #endif
                color.rgb = ToneMapping_ACES_To_sRGB(color.rgb, half(1.0));
				return color;
            }
            ENDHLSL
        }

        // 1 Bloom Filter
        Pass 
		{  
			// Cull Front
			HLSLPROGRAM  
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
			#include "Assets/Shaders/ShaderLibrarys/BXPipelineCommon.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

			Texture2D _PostProcessInput;
			float4 _PostProcessInput_TexelSize;
			SamplerState sampler_PostProcessInput; // clamp_bilinear 若要使用 point filter 可以使用 textureFetch 采样
			half4 _BloomFilters;
			half2 _BloomStrength;

            v2f vert (uint vertexID : SV_VERTEXID)
            {
                v2f o;
				if(_ProjectionParams.x < 0.0)
				{
					o.vertex = float4(
						vertexID <= 1 ? -1.0 : 3.0,
						vertexID == 0 ? 3.0 : -1.0,
						0.0, 1.0
					);
					o.uv = float2(
						vertexID <= 1 ? 0.0 : 2.0,
						vertexID == 0 ? 2.0 : 0.0
					);
					o.uv.y = 1.0 - o.uv.y;
				}
				else
				{
					o.vertex = float4(
						vertexID <= 1 ? -1.0 : 3.0,
						vertexID == 1 ? 3.0 : -1.0,
						0.0, 1.0
					);
					o.uv = float2(
						vertexID <= 1 ? 0.0 : 2.0,
						vertexID == 1 ? 2.0 : 0.0
					);
				}
                return o;
            }

			half3 Prefilter(half3 c)
			{
				half brightness = half(1.963) * c.g + c.r;
				half soft = brightness + _BloomFilters.y;
				soft = clamp(soft, half(0.0), _BloomFilters.z);
				soft = soft * soft * _BloomFilters.w;
				half contribution = max(soft, brightness - _BloomFilters.x);
				contribution /= (brightness + half(0.0001));
				return c * contribution;
			}

            half4 frag (v2f i) : SV_Target
            {
				float2 uv = i.uv;
                half3 col = _PostProcessInput.SampleLevel(sampler_PostProcessInput, uv, 0).rgb;
				col = Prefilter(col);

				return half4(col * _BloomStrength.x, half(1.0));
            }
			
			ENDHLSL
		}

		// 2 Gauss Blur X
		Pass 
		{  
			// Cull Front
			HLSLPROGRAM  
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
			#include "Assets/Shaders/ShaderLibrarys/BXPipelineCommon.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

			Texture2D _PostProcessInput;
			float4 _PostProcessInput_TexelSize;
			SamplerState sampler_PostProcessInput; // clamp_bilinear 若要使用 point filter 可以使用 textureFetch 采样

            v2f vert (uint vertexID : SV_VERTEXID)
            {
                v2f o;
				if(_ProjectionParams.x < 0.0)
				{
					o.vertex = float4(
						vertexID <= 1 ? -1.0 : 3.0,
						vertexID == 0 ? 3.0 : -1.0,
						0.0, 1.0
					);
					o.uv = float2(
						vertexID <= 1 ? 0.0 : 2.0,
						vertexID == 0 ? 2.0 : 0.0
					);
					o.uv.y = 1.0 - o.uv.y;
				}
				else
				{
					o.vertex = float4(
						vertexID <= 1 ? -1.0 : 3.0,
						vertexID == 1 ? 3.0 : -1.0,
						0.0, 1.0
					);
					o.uv = float2(
						vertexID <= 1 ? 0.0 : 2.0,
						vertexID == 1 ? 2.0 : 0.0
					);
				}
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
				half3 col = half(0.0);
				float2 uv = i.uv;
				float offsets[] = {-4.0, -3.0, -2.0, -1.0, 0.0, 1.0, 2.0, 3.0, 4.0};
				half weights[] = {half(0.01621622), half(0.05405405), half(0.12162162), half(0.19459459), half(0.22702703), 
								   half(0.194595459), half(0.12162162), half(0.05405405), half(0.01621622)};
				for(int i = 0; i < 9; ++i)
				{
					half offset = offsets[i] * _PostProcessInput_TexelSize.x;;
					half3 p = _PostProcessInput.SampleLevel(sampler_PostProcessInput, uv + float2(offset, 0.0), 0).rgb;
                    p = p * weights[i];
                    col += p;
				}
				return half4(col, half(1.0));
            }
			
			ENDHLSL
		}

		// 3 Gauss Blur Y
		Pass 
		{  
			// Cull Front
			HLSLPROGRAM  
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
			#include "Assets/Shaders/ShaderLibrarys/BXPipelineCommon.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

			Texture2D _PostProcessInput;
			float4 _PostProcessInput_TexelSize;
			SamplerState sampler_PostProcessInput; // clamp_bilinear 若要使用 point filter 可以使用 textureFetch 采样

            v2f vert (uint vertexID : SV_VERTEXID)
            {
                v2f o;
				if(_ProjectionParams.x < 0.0)
				{
					o.vertex = float4(
						vertexID <= 1 ? -1.0 : 3.0,
						vertexID == 0 ? 3.0 : -1.0,
						0.0, 1.0
					);
					o.uv = float2(
						vertexID <= 1 ? 0.0 : 2.0,
						vertexID == 0 ? 2.0 : 0.0
					);
					o.uv.y = 1.0 - o.uv.y;
				}
				else
				{
					o.vertex = float4(
						vertexID <= 1 ? -1.0 : 3.0,
						vertexID == 1 ? 3.0 : -1.0,
						0.0, 1.0
					);
					o.uv = float2(
						vertexID <= 1 ? 0.0 : 2.0,
						vertexID == 1 ? 2.0 : 0.0
					);
				}
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
				float2 uv = i.uv;
				half3 col = half(0.0);
				float offsets[] = {-4.0, -3.0, -2.0, -1.0, 0.0, 1.0, 2.0, 3.0, 4.0};
				half weights[] = {half(0.01621622), half(0.05405405), half(0.12162162), half(0.19459459), half(0.22702703), 
								   half(0.194595459), half(0.12162162), half(0.05405405), half(0.01621622)};
				for(int i = 0; i < 9; ++i)
				{
					half offset = offsets[i] * _PostProcessInput_TexelSize.y;
                    half3 p = _PostProcessInput.SampleLevel(sampler_PostProcessInput, uv + float2(0.0, offset), 0).rgb;
                    p = p * weights[i];
					col += p;
				}
				return half4(col, half(1.0));
            }
			
			ENDHLSL
		}

		// 4 Bloom Add
		Pass 
		{  
			// Cull Front
			HLSLPROGRAM  
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Assets/Shaders/ShaderLibrarys/BXPipelineCommon.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

			Texture2D<half4> _PostProcessInput;
			Texture2D<half4> _BloomTex;
			float4 _PostProcessInput_TexelSize;
			SamplerState sampler_PostProcessInput; // clamp_bilinear 若要使用 point filter 可以使用 textureFetch 采样
			half2 _BloomStrength;
	

            v2f vert (uint vertexID : SV_VERTEXID)
            {
                v2f o;
				if(_ProjectionParams.x < 0.0)
				{
					o.vertex = float4(
						vertexID <= 1 ? -1.0 : 3.0,
						vertexID == 0 ? 3.0 : -1.0,
						0.0, 1.0
					);
					o.uv = float2(
						vertexID <= 1 ? 0.0 : 2.0,
						vertexID == 0 ? 2.0 : 0.0
					);
					o.uv.y = 1.0 - o.uv.y;
				}
				else
				{
					o.vertex = float4(
						vertexID <= 1 ? -1.0 : 3.0,
						vertexID == 1 ? 3.0 : -1.0,
						0.0, 1.0
					);
					o.uv = float2(
						vertexID <= 1 ? 0.0 : 2.0,
						vertexID == 1 ? 2.0 : 0.0
					);
				}
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
				float2 uv = i.uv;
				half3 col = _PostProcessInput.SampleLevel(sampler_PostProcessInput, uv, 0).rgb;
				half3 bloomAdd = _BloomTex.SampleLevel(sampler_PostProcessInput, uv, 0).rgb;
				half bright = saturate(half(0.3) * col.g + half(0.59) * col.r + half(0.11) * col.b);
				bloomAdd *= lerp(half(1.0), _BloomStrength.y, bright);
				return half4(col + bloomAdd, half(1.0));
            }
			
			ENDHLSL
		}

		// 5 Blit
		Pass
		{
			HLSLPROGRAM
			#pragma target 4.5
			#pragma editor_sync_compilation
			#pragma vertex vert
			#pragma fragment frag

			// #define _ACES_C 1
			#include "Assets/Shaders/ShaderLibrarys/BXPipelineCommon.hlsl"

			Texture2D _PostProcessInput;
			float4 _PostProcessInput_TexelSize;
			SamplerState sampler_PostProcessInput;

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (uint vertexID : SV_VERTEXID)
			{
				v2f o;

				if(_ProjectionParams.x < 0.0)
				{
					o.vertex = float4(
						vertexID <= 1 ? -1.0 : 3.0,
						vertexID == 0 ? 3.0 : -1.0,
						0.0, 1.0
					);
					o.uv = float2(
						vertexID <= 1 ? 0.0 : 2.0,
						vertexID == 0 ? 2.0 : 0.0
					);
					o.uv.y = 1.0 - o.uv.y;
				}
				else
				{
					o.vertex = float4(
						vertexID <= 1 ? -1.0 : 3.0,
						vertexID == 1 ? 3.0 : -1.0,
						0.0, 1.0
					);
					o.uv = float2(
						vertexID <= 1 ? 0.0 : 2.0,
						vertexID == 1 ? 2.0 : 0.0
					);
				}
				return o;
			}

			half4 frag (v2f i) : SV_Target
			{
				return _PostProcessInput.SampleLevel(sampler_PostProcessInput, i.uv, 0);
			}
			ENDHLSL
		}
    }
}
