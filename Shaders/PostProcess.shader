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

        Pass
        {
            HLSLPROGRAM
            #pragma editor_sync_compilation
            #pragma vertex vert
            #pragma fragment frag

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
				half4 color = _PostProcessInput.SampleLevel(sampler_PostProcessInput, i.uv, 0);
                color.rgb = saturate(ToneMapping_ACES_To_sRGB(color.rgb, half(1.0)));
				return color;
            }
            ENDHLSL
        }
    }
}
