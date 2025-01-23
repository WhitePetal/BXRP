Shader "Unlit/TestShader"
{
    Properties
    {
        _UintIndex("_UintIndex", Int) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            #include "Assets/Shaders/ShaderLibrarys/BXPipelineCommon.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                uint _UintIndex;
            CBUFFER_END
            CBUFFER_START(MLIGHTS)
                half4 _MLightColors[32];
            CBUFFER_END

            v2f vert (appdata v)
            {
                v2f o;
                float3 pos_world = TransformObjectToWorld(v.vertex.xyz);
                o.vertex = mul(UNITY_MATRIX_VP, float4(pos_world, 1.0));
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                uint tl = _UintIndex >> 2u;
                uint tr = (_UintIndex & 5u) << 2u;
                uint t = tl + tr;
                uint index = (int)t;
                half4 lightColor = _MLightColors[index];
                return lightColor;
            }
            ENDHLSL
        }
    }
}
