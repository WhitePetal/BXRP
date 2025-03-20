using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BXRenderPipeline
{
	[BXVolumeComponentMenu("PostProcess/Bloom")]
    public class BXBloomComponent : BXVolumeComponment
    {
        [Range(0f, 10f)]
        public float threshold = 1f;
        [Range(0f, 1f)]
        public float threshold_knne = 0.7f;
        [Range(0f, 10f)]
        public float bloom_strength = 1f;
        [Range(0f, 1f)]
        public float bright_clamp = 1f;

        [HideInInspector]
        public float threshold_runtime;
        [HideInInspector]
        public float threshold_knne_runtime;
        [HideInInspector]
        public float bloom_strength_runtime;
        [HideInInspector]
        public float bright_clamp_runtime;

        public override RenderFeatureStep RenderFeatureStep => RenderFeatureStep.OnPostProcess;

		public override void BeDisabled()
		{
			if (BXRenderPipeline.TryGetRenderCommonSettings(out var settings))
			{
                var postProcessMat = settings.postProcessMaterial;
                if (postProcessMat.IsKeywordEnabled("_BLOOM"))
                {
                    postProcessMat.DisableKeyword("_BLOOM");
                }
            }
        }

		public override void BeEnabled()
		{
            bloom_strength_runtime = 0f;
        }

		public override void OnDisabling(float interpFactor)
		{
            bloom_strength_runtime = Mathf.Lerp(bloom_strength_runtime, 0f, interpFactor);
		}

		public override void OnRender(CommandBuffer cmd, BXMainCameraRenderBase render)
		{
            var postProcessMat = render.commonSettings.postProcessMaterial;
            if (!postProcessMat.IsKeywordEnabled("_BLOOM"))
            {
                postProcessMat.EnableKeyword("_BLOOM");
            }

            int width = render.width;
            int height = render.height;

            cmd.GetTemporaryRT(BXShaderPropertyIDs._BloomTempRT_IDs[0], width >> 1, height >> 1, 0, FilterMode.Bilinear, RenderTextureFormat.RGB111110Float);

            cmd.GetTemporaryRT(BXShaderPropertyIDs._BloomTempRT_IDs[1], width >> 2, height >> 2, 0, FilterMode.Bilinear, RenderTextureFormat.RGB111110Float);
            cmd.GetTemporaryRT(BXShaderPropertyIDs._BloomTempRT_IDs[2], width >> 2, height >> 2, 0, FilterMode.Bilinear, RenderTextureFormat.RGB111110Float);

            cmd.GetTemporaryRT(BXShaderPropertyIDs._BloomTempRT_IDs[3], width >> 3, height >> 3, 0, FilterMode.Bilinear, RenderTextureFormat.RGB111110Float);
            cmd.GetTemporaryRT(BXShaderPropertyIDs._BloomTempRT_IDs[4], width >> 3, height >> 3, 0, FilterMode.Bilinear, RenderTextureFormat.RGB111110Float);

            cmd.GetTemporaryRT(BXShaderPropertyIDs._BloomTempRT_IDs[5], width >> 4, height >> 4, 0, FilterMode.Bilinear, RenderTextureFormat.RGB111110Float);
            cmd.GetTemporaryRT(BXShaderPropertyIDs._BloomTempRT_IDs[6], width >> 4, height >> 4, 0, FilterMode.Bilinear, RenderTextureFormat.RGB111110Float);

            var renderSettings = BXVolumeManager.instance.renderSettings.GetComponent<BXBloomComponent>();

            Vector4 threshold;
            threshold.x = renderSettings.threshold_runtime;
            threshold.y = threshold.x * renderSettings.threshold_knne_runtime;
            threshold.z = 2f * threshold.y;
            threshold.w = 0.25f / (threshold.y + 0.0001f);
            threshold.y -= threshold.x;
            cmd.SetGlobalVector(BXShaderPropertyIDs._BloomFilters_ID, threshold);
            cmd.SetGlobalVector(BXShaderPropertyIDs._BloomStrength_ID, new Vector4(renderSettings.bloom_strength_runtime, renderSettings.bright_clamp_runtime));

            render.DrawPostProcess(render.postProcessInputTarget, BXShaderPropertyIDs._BloomTempRT_RTIDs[0], postProcessMat, 1, false);

            render.DrawPostProcess(BXShaderPropertyIDs._BloomTempRT_RTIDs[0], BXShaderPropertyIDs._BloomTempRT_RTIDs[1], postProcessMat, 2, false);
            render.DrawPostProcess(BXShaderPropertyIDs._BloomTempRT_RTIDs[1], BXShaderPropertyIDs._BloomTempRT_RTIDs[2], postProcessMat, 3, false);

            render.DrawPostProcess(BXShaderPropertyIDs._BloomTempRT_RTIDs[2], BXShaderPropertyIDs._BloomTempRT_RTIDs[3], postProcessMat, 2, false);
            render.DrawPostProcess(BXShaderPropertyIDs._BloomTempRT_RTIDs[3], BXShaderPropertyIDs._BloomTempRT_RTIDs[4], postProcessMat, 3, false);

            render.DrawPostProcess(BXShaderPropertyIDs._BloomTempRT_RTIDs[4], BXShaderPropertyIDs._BloomTempRT_RTIDs[5], postProcessMat, 2, false);
            render.DrawPostProcess(BXShaderPropertyIDs._BloomTempRT_RTIDs[5], BXShaderPropertyIDs._BloomTempRT_RTIDs[6], postProcessMat, 3, false);

            cmd.SetGlobalTexture(BXShaderPropertyIDs._BloomTex_ID, BXShaderPropertyIDs._BloomTempRT_RTIDs[4]);
            render.DrawPostProcess(BXShaderPropertyIDs._BloomTempRT_RTIDs[6], BXShaderPropertyIDs._BloomTempRT_RTIDs[3], postProcessMat, 4, false);
            cmd.SetGlobalTexture(BXShaderPropertyIDs._BloomTex_ID, BXShaderPropertyIDs._BloomTempRT_RTIDs[2]);
            render.DrawPostProcess(BXShaderPropertyIDs._BloomTempRT_RTIDs[3], BXShaderPropertyIDs._BloomTempRT_RTIDs[1], postProcessMat, 4, false);
            cmd.SetGlobalTexture(BXShaderPropertyIDs._BloomTex_ID, BXShaderPropertyIDs._BloomTempRT_RTIDs[1]);

            cmd.ReleaseTemporaryRT(BXShaderPropertyIDs._BloomTempRT_IDs[0]);
            for (int i = 2; i < BXShaderPropertyIDs._BloomTempRT_IDs.Length; ++i)
            {
                cmd.ReleaseTemporaryRT(BXShaderPropertyIDs._BloomTempRT_IDs[i]);
            }
            render.RegisterNeedReleasePostProcessTempRT(BXShaderPropertyIDs._BloomTempRT_IDs[1]);
        }

		public override void OverrideData(BXVolumeComponment component, float interpFactor)
        {
            var target = component as BXBloomComponent;
            threshold_runtime = Mathf.Lerp(threshold, target.threshold, interpFactor);
            threshold_knne_runtime = Mathf.Lerp(threshold_knne, target.threshold_knne, interpFactor);
            bloom_strength_runtime = Mathf.Lerp(bloom_strength, target.bloom_strength, interpFactor);
            bright_clamp_runtime = Mathf.Lerp(bright_clamp, target.bright_clamp, interpFactor);
        }

        public override void RefreshData()
        {
            threshold_runtime = threshold;
            threshold_knne_runtime = threshold_knne;
            bloom_strength_runtime = bloom_strength;
            bright_clamp_runtime = bright_clamp;
        }
    }
}
