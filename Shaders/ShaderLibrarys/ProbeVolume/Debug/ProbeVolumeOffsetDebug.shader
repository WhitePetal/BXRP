Shader "Hidden/Core/ProbeVolumeOffsetDebug"
{
    SubShader
    {
        Tags{ "RenderType" = "Opaque" }

        HLSLINCLUDE
        #pragma editor_sync_compilation
        #pragma target 4.5
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
        #pragma multi_compile_fragment _ PROBE_VOLUMES_L1 PROBE_VOLUMES_L2
        //#pragma enable_d3d11_debug_symbols

        #include "Assets/Shaders/ShaderLibrarys/BXPipelineCommon.hlsl"

        #include "ProbeVolumeDebugBase.hlsl"

        #define PROBE_VOLUME_DEBUG_FUNCTION_OFFSET
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
