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
			return new BXRenderPipeline(useDynamicBatching, useGPUInstancing, useSRPBatching, commonSettings);
		}
	}
}
