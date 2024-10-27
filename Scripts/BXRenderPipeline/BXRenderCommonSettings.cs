using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace BXRenderPipeline
{
    [CreateAssetMenu(menuName = "Rendering/BXRenderPipeline/RenderCommonSettings")]
    public class BXRenderCommonSettings : ScriptableObject
    {
        public const string GraphicsQualityKey = "_BXGraphicsQuality";
        public const int GraphicsQualityExtreValue = 0;
        public const int GraphicsQualityHighValue = GraphicsQualityExtreValue + 1;
        public const int GraphicsQualityMidValue = GraphicsQualityHighValue + 1;
        public const int GraphicsQualityLowValue = GraphicsQualityMidValue + 1;
        public const int GraphicsQualityExLowValue = GraphicsQualityLowValue + 1;

        public enum AAType
		{
            None,
            GSR,
            FXAA,
            TAA,
            FSR,
            DLSS
		}

        [System.Serializable]
        public struct AtlasSettings
        {
            [SerializeField]
            public Vector2Int resolution;
            [SerializeField]
            public GraphicsFormat format;

            public bool isPow2 => Mathf.IsPowerOfTwo(resolution.x) && Mathf.IsPowerOfTwo(resolution.y);
            public bool isSquare => resolution.x == resolution.y;
        }

        [Header("必要文件")]
        public Shader coreBlitPS;
        public Shader coreBlitColorAndDepthPS;

        [Header("分辨率")]
        public float downSample = 1;
        public int minHeight = 1080;
        public int maxHeight = 1440;
        [Range(1, 8)]
        public int msaa = 2;
        public AAType aaType;

        [Header("帧率")]
        public int targetFrameRate = 120;

        [Header(("方向光设置"))]
        public float maxShadowDistance = 20f;
        public int cascadeCount = 3;
        public int shadowMapSize = 2048;
        public int shadowMapBits = 24;
        [Range(0.001f, 1f)]
        public float distanceFade = 0.1f;
        [Range(0f, 1f)]
        public float cascadeRatio1 = 0.1f, cascadeRatio2 = 0.25f, cascadeRatio3 = 0.5f;
        [Range(0.1f, 1f)]
        public float cascadeFade = 0.1f;

        [Header("Cluster光影设置")]
        public ComputeShader clusterLightCompute;
        public int clusterLightShadowMapSize = 1024;
        public int clusterLightShadowMapBits = 24;

        [Header("阴影开关")]
        public bool drawShadows = true;

        [Header("光源Cookie")]
        public AtlasSettings cookieAtlas = new AtlasSettings() 
        {
            resolution = new Vector2Int(1024, 1024),
            format = GraphicsFormat.R8G8B8A8_UNorm
        };
        public float cubeOctahedraSizeScale = 2.5f;
        //public bool useStructuredBuffer = false;

        [Header("后处理")]
        public Material postProcessMaterial;

        [Header("地形")]
        public bool gpuDrive;
        public bool terrShadow;
        public bool terrGrass;
        [Range(0f, 1f)]
        public float grassDensity = 1f;
        [Range(0f, 1f)]
        public float grassDstLOD = 0f;

        [Header("Shader LOD")]
        public int shaderLOD = 3000;

        [Header("Texture LOD")]
        public int textureLOD = 0;
        public int mipmapsMemoryBudge = 512;

        [Header("LOD Bias")]
        public float lodBias = 1f;

        [HideInInspector]
        public int quality;
        [HideInInspector]
        public bool supportComputeShader;
        [HideInInspector]
        public bool fewMemory;

        private int GetDefaultGraphicsQuality()
		{
            return 0;
		}

        private void SetBuiltinQualitySettings()
		{
            Application.targetFrameRate = targetFrameRate;
            QualitySettings.globalTextureMipmapLimit = textureLOD;
            QualitySettings.streamingMipmapsActive = true;
            QualitySettings.lodBias = lodBias;
            QualitySettings.softParticles = false;
            QualitySettings.particleRaycastBudget = 64;
            QualitySettings.billboardsFaceCameraPosition = true;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
			if (fewMemory)
			{
                QualitySettings.streamingMipmapsMemoryBudget = Mathf.Min(mipmapsMemoryBudge, 256);
			}
			else
			{
                QualitySettings.streamingMipmapsMemoryBudget = mipmapsMemoryBudge;
            }
		}

        private void SetPlatformCompatibles()
		{
            supportComputeShader = true;
            fewMemory = false;
		}

        public void Init()
		{
			if (PlayerPrefs.HasKey(GraphicsQualityKey))
			{
                quality = PlayerPrefs.GetInt(GraphicsQualityKey);
			}
			else
			{
                quality = GetDefaultGraphicsQuality();
                PlayerPrefs.SetInt(GraphicsQualityKey, quality);
			}

            SetPlatformCompatibles();
            SetGraphicsSettingsByQualityLevel(quality);
            SetBuiltinQualitySettings();
        }

        public void SetGraphicsSettings(float downSample, int minHeight, int maxHeight,
            float grassDensity, float grassDstLOD, float cascadeRatio1, float cascadeRatio2, float lodBias,
            int targetFrameRate, int shaderLOD, int msaa, int maxShadowDistance, int cascadeCount,
            int shadowMapSize, int shadowMapBits, int clusterShadowMapSize, int clusterShadowMapBits,
            int mipmapsMemoryBudget, int textureLOD,
            bool terrGrass, bool terrGrassShadow, bool drawShadows, bool gpuDrivent)
		{

		}

        public void SetGraphicsSettingsByQualityLevel(int quality)
		{
			switch (quality)
			{
                case GraphicsQualityExtreValue:
                    break;
			}
		}
    }
}
