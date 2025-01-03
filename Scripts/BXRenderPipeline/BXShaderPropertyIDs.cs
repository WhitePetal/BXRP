using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BXRenderPipeline
{
    public static class BXShaderPropertyIDs
    {
		public static readonly int _ViewPortRaysID = Shader.PropertyToID("_ViewPortRays");

        public static readonly int _StencilComp_ID = Shader.PropertyToID("_StencilComp");
        public static readonly int _StencilOp_ID = Shader.PropertyToID("_StencilOp");

        public static readonly int _FrameBuffer_ID = Shader.PropertyToID("_FrameBuffer");
        public static readonly int _DepthBuffer_ID = Shader.PropertyToID("_DepthBuffer");
        public static readonly int _EncodeDepthBuffer_ID = Shader.PropertyToID("_EncodeDepthBuffer");
        public static readonly RenderTargetIdentifier _FrameBuffer_TargetID = new RenderTargetIdentifier(_FrameBuffer_ID);
        public static readonly RenderTargetIdentifier _DepthBuffer_TargetID = new RenderTargetIdentifier(_DepthBuffer_ID);
        public static readonly RenderTargetIdentifier _EncodeDepthBuffer_TargetID = new RenderTargetIdentifier(_EncodeDepthBuffer_ID);
        public static readonly int _PostProcessInput_ID = Shader.PropertyToID("_PostProcessInput");

        public static readonly int _CameraPosition_ID = Shader.PropertyToID("_CameraPosition");
        public static readonly int _CmaeraUpward_ID = Shader.PropertyToID("_CmaeraUpward");
        public static readonly int _CameraForward_ID = Shader.PropertyToID("_CameraForward");
        public static readonly int _CameraRightword_ID = Shader.PropertyToID("_CameraRightword");

        public static readonly int _ProjectionParams_ID = Shader.PropertyToID("_ProjectionParams");

        public static readonly int _OtherLightWorldToLights_ID = Shader.PropertyToID("_OtherLightWorldToLights");
        public static readonly int _OtherLightCookieAltasUVRects_ID = Shader.PropertyToID("_OtherLightCookieAltasUVRects");
        public static readonly int _OtherLightLightTypes_ID = Shader.PropertyToID("_OtherLightLightTypes");
        public static readonly int _OtherLightCookieEnableBits_ID = Shader.PropertyToID("_OtherLightCookieEnableBits");
        public static readonly int _OtherLightCookieAltas_ID = Shader.PropertyToID("_OtherLightCookieAltas");
        public static readonly int _OtherLightCookieAltasFormat_ID = Shader.PropertyToID("_OtherLightCookieAltasFormat");
        public static readonly int _OtherLightIndex_ID = Shader.PropertyToID("_OtherLightIndex");


        //public static int _MainLightCookie_ID = Shader.PropertyToID("_MainLightCookie");
        //public static int _MainLightWorldToLight_ID = Shader.PropertyToID("_MainLightWorldToLight");
        //public static int _MainLightCookieFormat_ID = Shader.PropertyToID("_MainLightCookieFormat");

        public static readonly int _ReleateExpourse_ID = Shader.PropertyToID("_ReleateExpourse");

        public static readonly int _BlurSpeard_ID = Shader.PropertyToID("_BlurSpeard");
        public static readonly int _BloomFilters_ID = Shader.PropertyToID("_BloomFilters");
        public static readonly int _BloomStrength_ID = Shader.PropertyToID("_BloomStrength");
        public static readonly int _BloomBlurStrength_ID = Shader.PropertyToID("_BloomBlurStrength");
        public static readonly int _BloomTex_ID = Shader.PropertyToID("_BloomTex");

        public static readonly int[] _BloomTempRT_IDs = new int[]
        {
            Shader.PropertyToID("_BloomTempRT0"),
            Shader.PropertyToID("_BloomTempRT1"),
            Shader.PropertyToID("_BloomTempRT2"),
            Shader.PropertyToID("_BloomTempRT3"),
            Shader.PropertyToID("_BloomTempRT4"),
            Shader.PropertyToID("_BloomTempRT5"),
            Shader.PropertyToID("_BloomTempRT6"),
            Shader.PropertyToID("_BloomTempRT7"),
            Shader.PropertyToID("_BloomTempRT8"),

            Shader.PropertyToID("_BloomTempRT9"),
        };
        public static readonly RenderTargetIdentifier[] _BloomTempRT_RTIDs = new RenderTargetIdentifier[]
        {
            new RenderTargetIdentifier(_BloomTempRT_IDs[0]),
            new RenderTargetIdentifier(_BloomTempRT_IDs[1]),
            new RenderTargetIdentifier(_BloomTempRT_IDs[2]),
            new RenderTargetIdentifier(_BloomTempRT_IDs[3]),
            new RenderTargetIdentifier(_BloomTempRT_IDs[4]),
            new RenderTargetIdentifier(_BloomTempRT_IDs[5]),
            new RenderTargetIdentifier(_BloomTempRT_IDs[6]),
            new RenderTargetIdentifier(_BloomTempRT_IDs[7]),
            new RenderTargetIdentifier(_BloomTempRT_IDs[8]),
            new RenderTargetIdentifier(_BloomTempRT_IDs[9]),
        };


        public static readonly int _DiffuseColor_ID = Shader.PropertyToID("_DiffuseColor");
    }
}
