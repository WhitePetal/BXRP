using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BXRenderPipeline
{
	[CreateAssetMenu(menuName = "Rendering/BXRenderPipeline/ForwardPlusRenderPiepline")]
	public class BXRenderPipelineAsset : RenderPipelineAsset, IProbeVolumeEnabledRenderPipeline
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

		public bool supportProbeVolume
        {
            get
            {
				return true;
            }
        }

        public ProbeVolumeSHBands maxSHBands
        {
            get
            {
				return ProbeVolumeSHBands.SphericalHarmonicsL2;
            }
        }

		/// <summary>
		/// Returns the projects global ProbeVolumeSceneData instance.
		/// </summary>
		[Obsolete("This property is no longer necessary.")]
		public ProbeVolumeSceneData probeVolumeSceneData => null;

        protected override RenderPipeline CreatePipeline()
		{
			return new BXRenderPipeline(useDynamicBatching, useGPUInstancing, useSRPBatching, commonSettings,
				beforeRenderRenderFeatures, onDirShadowsRenderFeatures, beforeOpaqueRenderFeatures,
				afterOpaqueRenderFeatures, beforeTransparentRenderFeatures, afterTransparentRenderFeatures,
				onPostProcessRenderFeatures);
		}
	}
}
