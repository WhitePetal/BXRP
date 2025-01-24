Shader "Hidden/Core/ProbeVolumeDebug"
{
    SubShader
    {
        Tags { "RenderPipeline"="BXRenderPipeline" "RenderType"="Opaque" }

        HLSLINCLUDE
        #pragma target 4.5
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
        #pragma multi_compile_fragment _ PROBE_VOLUMES_L1 PROBE_VOLUMES_L2

        #include "Assets/Shaders/ShaderLibrarys/BXPipelineCommon.hlsl"

        #include "ProbeVolumeDebugBase.hlsl"

        #define PROBE_VOLUME_DEBUG_FUNCTION_MAIN
        #include "ProbeVolumeDebugFunctions.hlsl"
        ENDHLSL

        Pass
        {
            Name "BXDeferredBase"

            ZTest LEqual
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }
}
