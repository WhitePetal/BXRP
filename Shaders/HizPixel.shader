Shader "HizPixel"
{
    Properties
    {

    }
    SubShader
    {
        Cull Back
        ZWrite Off
        ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma target 4.5
            #pragma editor_sync_compilation
            #pragma vertex vert
            #pragma fragment frag

            #include "Assets/Shaders/ShaderLibrarys/BXPipelineCommon.hlsl"

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            Texture2D _PostProcessInput;
			float4 _PostProcessInput_TexelSize;
			SamplerState sampler_point_clamp;

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

            float4 frag (v2f i) : SV_Target
            {
                float2 dp = _PostProcessInput_TexelSize.xy;
                float4 encodeDepth0 = _PostProcessInput.SampleLevel(sampler_point_clamp, i.uv, 0);
                float4 encodeDepth1 = _PostProcessInput.SampleLevel(sampler_point_clamp, i.uv + float2(dp.x, 0.0), 0);
                float4 encodeDepth2 = _PostProcessInput.SampleLevel(sampler_point_clamp, i.uv + float2(0.0, dp.y), 0);
                float4 encodeDepth3 = _PostProcessInput.SampleLevel(sampler_point_clamp, i.uv + dp, 0);
                float depth0 = DecodeFloatRGBA(encodeDepth0);
                float depth1 = DecodeFloatRGBA(encodeDepth1);
                float depth2 = DecodeFloatRGBA(encodeDepth2);
                float depth3 = DecodeFloatRGBA(encodeDepth3);
                return min(depth0, min(depth1, min(depth2, depth3)));
            }
            ENDHLSL
        }

        Pass
        {
            HLSLPROGRAM
            #pragma target 4.5
            #pragma editor_sync_compilation
            #pragma vertex vert
            #pragma fragment frag

            #include "Assets/Shaders/ShaderLibrarys/BXPipelineCommon.hlsl"

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            Texture2D _PostProcessInput;
			float4 _PostProcessInput_TexelSize;
			SamplerState sampler_point_clamp;

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

            float4 frag (v2f i) : SV_Target
            {
                float2 dp = _PostProcessInput_TexelSize.xy;
                float depth0 = _PostProcessInput.SampleLevel(sampler_point_clamp, i.uv, 0).x;
                float depth1 = _PostProcessInput.SampleLevel(sampler_point_clamp, i.uv + float2(dp.x, 0.0), 0).x;
                float depth2 = _PostProcessInput.SampleLevel(sampler_point_clamp, i.uv + float2(0.0, dp.y), 0).x;
                float depth3 = _PostProcessInput.SampleLevel(sampler_point_clamp, i.uv + dp, 0).x;
                return min(depth0, min(depth1, min(depth2, depth3)));
            }
            ENDHLSL
        }

        Pass
        {
            HLSLPROGRAM
            #pragma target 4.5
            #pragma editor_sync_compilation
            #pragma vertex vert
            #pragma fragment frag

            #include "Assets/Shaders/ShaderLibrarys/BXPipelineCommon.hlsl"

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            Texture2D _PostProcessInput;
			float4 _PostProcessInput_TexelSize;
			SamplerState sampler_point_clamp;

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

            float4 frag (v2f i) : SV_Target
            {
                return _PostProcessInput.SampleLevel(sampler_point_clamp, i.uv, 0);
            }
            ENDHLSL
        }

        Pass
        {
            HLSLPROGRAM
            #pragma target 4.5
            #pragma editor_sync_compilation
            #pragma vertex vert
            #pragma fragment frag

            #include "Assets/Shaders/ShaderLibrarys/BXPipelineCommon.hlsl"

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            Texture2D _BoundCentersTex;
            Texture2D _BoundSizesTex;
            Texture2D _HizMapInput;
			SamplerState sampler_point_clamp;
            int4 _HizParams;
            float2 _HizTexSize;;
            float4 _HizMipSize[16];
            float4x4 _HizProjectionMatrix;

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

            inline float3 GetNDCPos(float3 pos)
            {
                float4 p = mul(_HizProjectionMatrix, float4(pos, 1.0));
                p.xyz /= p.w;
                p.xy = p.xy * 0.5 + 0.5;
                // p.y = 1.0f - p.y;
                return p.xyz;
            }

            float4 frag (v2f i) : SV_Target
            {
                uint3 id;
                id.xy = ceil(i.uv * 64);
                id.z = 0;
                float3 size = _BoundSizesTex.SampleLevel(sampler_point_clamp, i.uv, 0).xyz;
                // if(all(size <= 0.0))
                // if(id.x + id.y * 64 >= (uint)_HizParams.w) 
                // {
                //     return 0.0;
                // }
                float3 center = _BoundCentersTex.SampleLevel(sampler_point_clamp, i.uv, 0).xyz;
            
                float3 p0 = center + size;
                float3 p1 = center - size;
                float3 p2 = float3(p0.xy, p1.z);     // + + -
                float3 p3 = float3(p0.x, p1.yz);     // + - -
                float3 p4 = float3(p1.xy, p0.z);     // - - +
                float3 p5 = float3(p1.x, p0.y, p1.z);// - + -
                float3 p6 = float3(p0.x, p1.y, p0.z);// + - +
                float3 p7 = float3(p1.x, p0.yz);     // - + +
            
                p0 = GetNDCPos(p0);
                p1 = GetNDCPos(p1);
                p2 = GetNDCPos(p2);
                p3 = GetNDCPos(p3);
                p4 = GetNDCPos(p4);
                p5 = GetNDCPos(p5);
                p6 = GetNDCPos(p6);
                p7 = GetNDCPos(p7);
            
                float3 aabbMin = min(p0, min(p1, min(p2, min(p3, min(p4, min(p5, min(p6, p7))))))).xyz;
                float3 aabbMax = max(p0, max(p1, max(p2, max(p3, max(p4, max(p5, max(p6, p7))))))).xyz;
            
                float2 ndcSize = (aabbMax.xy - aabbMin.xy) * _HizParams.xy;
                float radius = max(ndcSize.x, ndcSize.y);
                int mip = (int)floor(log2(radius));
                // or not mip - 2 for more safe culling
                mip = clamp(mip-2, 0, _HizParams.z);
                float4 mipSize = _HizMipSize[mip];
                float2 minPx = saturate((aabbMin.xy * mipSize.zw + mipSize.xy) / _HizTexSize);
                float2 maxPx = saturate((aabbMax.xy * mipSize.zw + mipSize.xy) / _HizTexSize);
            
                float d0 = _HizMapInput.SampleLevel(sampler_point_clamp, minPx, 0);
                float d1 = _HizMapInput.SampleLevel(sampler_point_clamp, maxPx, 0);
                float d2 = _HizMapInput.SampleLevel(sampler_point_clamp, float2(minPx.x, maxPx.y), 0);
                float d3 = _HizMapInput.SampleLevel(sampler_point_clamp, float2(maxPx.x, minPx.y), 0);
                float minD = min(d0, min(d1, min(d2, d3)));
                return (minD >= aabbMax.z) ? 0.0 : 1.0;
            }
            ENDHLSL
        }
    }
}
