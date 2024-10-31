using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BXRenderPipeline
{
    public static class BXShaderPropertyIDs
    {
        public static int _ShadowsDistanceFade_ID = Shader.PropertyToID("_ShadowsDistanceFade");
        public static int _DirectionalShadowMatrixs_ID = Shader.PropertyToID("_DirectionalShadowMatrixs");
        public static int _CascadeCount_ID = Shader.PropertyToID("_CascadeCount");
        public static int _CascadeCullingSpheres_ID = Shader.PropertyToID("_CascadeCullingSpheres");
        public static int _CascadeDatas_ID = Shader.PropertyToID("_CascadeDatas");
        public static int _ShadowMapSize_ID = Shader.PropertyToID("_ShadowMapSize");
        public static int _DirectionalShadowMap_ID = Shader.PropertyToID("_DirectionalShadowMap");
        public static int _ClusterShadowMap_ID = Shader.PropertyToID("_ClusterShadowMap");
        public static RenderTargetIdentifier _DirectionalShadowMap_TargetID = new RenderTargetIdentifier(_DirectionalShadowMap_ID);
        public static RenderTargetIdentifier _ClusterLightShadowMap_TargetID = new RenderTargetIdentifier(_ClusterShadowMap_ID);
        public static int _ShadowPancaking_ID = Shader.PropertyToID("_ShadowPancaking");
        public static int _ClusterShadowTiles_ID = Shader.PropertyToID("_ClusterShadowTiles");
        public static int _ClusterShadowMatrices_ID = Shader.PropertyToID("_ClusterShadowMatrices");
        public static int _DirectionalLightCount_ID = Shader.PropertyToID("_DirectionalLightCount");
        public static int _DirectionalLightDirections_ID = Shader.PropertyToID("_DirectionalLightDirections");
        public static int _DirectionalLightColors_ID = Shader.PropertyToID("_DirectionalLightColors");
        public static int _DirectionalShadowDatas_ID = Shader.PropertyToID("_DirectionalShadowDatas");
        public static int _ClusterLightCount_ID = Shader.PropertyToID("_ClusterLightCount");
        public static int _ClusterLightSpheres_ID = Shader.PropertyToID("_ClusterLightSpheres");
        public static int _ClusterLightDirections_ID = Shader.PropertyToID("_ClusterLightDirections");
        public static int _ClusterLightThresholds_ID = Shader.PropertyToID("_ClusterLightThresholds");
        public static int _ClusterLightColors_ID = Shader.PropertyToID("_ClusterLightColors");
        public static int _ClusterShadowDatas_ID = Shader.PropertyToID("_ClusterShadowDatas");
        public static int _FrameBuffer_ID = Shader.PropertyToID("_FrameBuffer");
        public static int _DepthBuffer_ID = Shader.PropertyToID("_DepthBuffer");
        public static RenderTargetIdentifier _FrameBuffer_TargetID = new RenderTargetIdentifier(_FrameBuffer_ID);
        public static RenderTargetIdentifier _DepthBuffer_TargetID = new RenderTargetIdentifier(_DepthBuffer_ID);
        public static int _PostProcessInput_ID = Shader.PropertyToID("_PostProcessInput");
        public static int _ClusterSize_ID = Shader.PropertyToID("_ClusterSize");
        public static int _ClusterLightingIndices_ID = Shader.PropertyToID("_ClusterLightingIndices");
        public static int _ClusterLightingDatas_ID = Shader.PropertyToID("_ClusterLightingDatas");
        public static int _ClusterLightMaxBounds_ID = Shader.PropertyToID("_ClusterLightMaxBounds");
        public static int _ClusterLightMinBounds_ID = Shader.PropertyToID("_ClusterLightMinBounds");
        public static int _CameraPosition_ID = Shader.PropertyToID("_CameraPosition");
        public static int _CmaeraUpward_ID = Shader.PropertyToID("_CmaeraUpward");
        public static int _CameraForward_ID = Shader.PropertyToID("_CameraForward");
        public static int _CameraRightword_ID = Shader.PropertyToID("_CameraRightword");
        public static int _TileLBStart_ID = Shader.PropertyToID("_TileLBStart");
        public static int _TileRBStart_ID = Shader.PropertyToID("_TileRBStart");
        public static int _TileLUStart_ID = Shader.PropertyToID("_TileLUStart");
        public static int _TileRVec_ID = Shader.PropertyToID("_TileRVec");
        public static int _ProjectionParams_ID = Shader.PropertyToID("_ProjectionParams");

        //public static int _ClusterLightWorldToLightsBuffer_ID = Shader.PropertyToID("_ClusterLightWorldToLightsBuffer");
        //public static int _ClusterLightCookieAltasUVRectsBuffer_ID = Shader.PropertyToID("_ClusterLightCookieAltasUVRectsBuffer");
        //public static int _ClusterLightLightTypesBuffer_ID = Shader.PropertyToID("_ClusterLightLightTypesBuffer");
        public static int _ClusterLightWorldToLights_ID = Shader.PropertyToID("_ClusterLightWorldToLights");
        public static int _ClusterLightCookieAltasUVRects_ID = Shader.PropertyToID("_ClusterLightCookieAltasUVRects");
        public static int _ClusterLightLightTypes_ID = Shader.PropertyToID("_ClusterLightLightTypes");
        public static int _ClusterLightCookieEnableBits_ID = Shader.PropertyToID("_ClusterLightCookieEnableBits");
        public static int _ClusterLightCookieAltas_ID = Shader.PropertyToID("_ClusterLightCookieAltas");
        public static int _ClusterLightCookieAltasFormat_ID = Shader.PropertyToID("_ClusterLightCookieAltasFormat");


        //public static int _MainLightCookie_ID = Shader.PropertyToID("_MainLightCookie");
        //public static int _MainLightWorldToLight_ID = Shader.PropertyToID("_MainLightWorldToLight");
        //public static int _MainLightCookieFormat_ID = Shader.PropertyToID("_MainLightCookieFormat");

        public static int _ReleateExpourse_ID = Shader.PropertyToID("_ReleateExpourse");

        public static int _BlurSpeard_ID = Shader.PropertyToID("_BlurSpeard");
        public static int _BloomFilters_ID = Shader.PropertyToID("_BloomFilters");
        public static int _BloomStrength_ID = Shader.PropertyToID("_BloomStrength");
        public static int _BloomBlurStrength_ID = Shader.PropertyToID("_BloomBlurStrength");
        public static int _BloomTex_ID = Shader.PropertyToID("_BloomTex");

        public static int[] _BloomTempRT_IDs = new int[]
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
        public static RenderTargetIdentifier[] _BloomTempRT_RTIDs = new RenderTargetIdentifier[]
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
    }
}
