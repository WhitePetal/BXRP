using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BXRenderPipeline
{
	[CreateAssetMenu(menuName = "Rendering/BXRenderPipeline/ForwardPlusRenderPiepline")]
	public class BXRenderPipelineAsset : RenderPipelineAsset
	{
		public bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatching = true;
		public BXRenderCommonSettings commonSettings;

		public List<BXRenderFeature> beforeRenderRenderFeatures;
		public List<BXRenderFeature> onDirShadowsRenderFeatures;
		public List<BXRenderFeature> beforeOpaqueRenderFeatures;
		public List<BXRenderFeature> afterOpaqueRenderFeatures;
		public List<BXRenderFeature> beforeTransparentRenderFeatures;
		public List<BXRenderFeature> afterTransparentRenderFeatures;
		public List<BXRenderFeature> onPostProcessRenderFeatures;

		protected override RenderPipeline CreatePipeline()
		{
			return new BXRenderPipeline(useDynamicBatching, useGPUInstancing, useSRPBatching, commonSettings,
				beforeRenderRenderFeatures, onDirShadowsRenderFeatures, beforeOpaqueRenderFeatures,
				afterOpaqueRenderFeatures, beforeTransparentRenderFeatures, afterTransparentRenderFeatures,
				onPostProcessRenderFeatures);
		}
	}
}
