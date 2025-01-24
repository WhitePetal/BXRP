Shader "Hidden/Core/ProbeVolumeFragmentationDebug"
{
    SubShader
    {
        Tags { }

        Pass
        {
            ZWrite On
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
            #pragma editor_sync_compilation
            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
            // #pragma enable_d3d11_debug_symbols

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Assets/Shaders/ShaderLibrarys/BXPipelineCommon.hlsl"
            #include "ProbeVolumeDebugBase.hlsl"
            #define PROBE_VOLUME_DEBUG_FUNCTION_FRAGMENTATION
            #include "ProbeVolumeDebugFunctions.hlsl"

            ENDHLSL
        }
    }
}
