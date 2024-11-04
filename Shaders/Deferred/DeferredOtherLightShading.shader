Shader "DeferredOtherLightShading"
{
    Properties
    {
        _StencilComp("Stencil Comp", Int) = 3
        _StencilOp("Stencil Op", Int) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry"}

        Pass
        {
            Cull Back
            ZWrite Off
            ZTest LEqual

            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            #include "Assets/Shaders/ShaderLibrarys/BXPipelineCommon.hlsl"
            #include "Assets/Shaders/ShaderLibrarys/BakedLights.hlsl"
            #include "Assets/Shaders/ShaderLibrarys/TransformLibrary.hlsl"

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
                o.vertex = mul(UNITY_MATRIX_VP, float4(pos_world, 1.0));
                return o;
            }

            void frag (v2f i)
            {

            }
            ENDHLSL
        }

        // 1
        Pass
        {
            Cull Front
            ZWrite Off
            ZTest GEqual

            Stencil
            {
                Ref 1
                Comp [_StencilComp]
                Pass [_StencilOp]
                Fail Zero
            }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            #include "Assets/Shaders/ShaderLibrarys/BXPipelineCommon.hlsl"
            #include "Assets/Shaders/ShaderLibrarys/BakedLights.hlsl"
            #include "Assets/Shaders/ShaderLibrarys/TransformLibrary.hlsl"

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
                o.vertex = mul(UNITY_MATRIX_VP, float4(pos_world, 1.0));
                return o;
            }

            void frag (v2f i)
            {

            }
            ENDHLSL
        }
    }
}
