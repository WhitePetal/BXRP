using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BXRenderPipeline
{
    /// <summary>
    /// A volume component that holds settings for the Adaptive Probe Volumes System per-camera options.
    /// </summary>
    [Serializable , BXVolumeComponentMenu("Lighting/Adaptive Probe Volumes Options")/**, SupportedOnRenderPipeline **/]
    public sealed class ProbeVolumesOptions : BXVolumeComponment
    {
		/// <summary>
		/// The overridden normal bias to be applied to the world position when sampling the Adaptive Probe Volumes data structure. Unit is meters
		/// </summary>
		[Tooltip("The overridden normal bias to be applied to the world position when sampling the Adaptive Probe Volumes data structure. Unit is meters.")]
		[Range(0f, 2f)]
        public float normalBias = 0.05f;

		/// <summary>
		/// A bias alongside the view vector to be applied to the world position when samping the Adaptive Probe Volumes data structure. Unit is meters
		/// </summary>
		[Tooltip("A bias alongside the view vector to be applied to the world position when sampling the Adaptive Probe Volumes data structure. Unit is meters.")]
		[Range(0f, 2f)]
		public float viewBias = 0.1f;

		/// <summary>
		/// Whether to scale the bias for Adaptive Probe Volumes by the minimum distance between probes.
		/// </summary>
		[Tooltip("Whether to scale the bias for Adaptive Probe Volumes by the minimum distance between probes.")]
		public bool scaleBiasWithMinProbeDistance = false;

		/// <summary>
		/// Noise to be applied to the sampling position. It can hide seams issues subdivison levels, but introduces noise.
		/// </summary>
		[Tooltip("Noise to be applied to the sampling position. It can hide seams issues subdivison levels, but introduces noise.")]
		[Range(0f, 1f)]
		public float samplingNoise = 0.1f;

		/// <summary>
		/// Whether to animate the noise when TAA is enabled, smoothing potentially out the noise pattern introduces
		/// </summary>
		[Tooltip("Whether to animate the noise when TAA is enabled, smoothing potentially out the noise pattern introduces")]
		public bool animateSamplingNoise = true;

		/// <summary>
		/// Method used to reduce leaks
		/// </summary>
		[Tooltip("Method used to reduce leaks")]
		public APVLeakReductionMode leakReductionMode = APVLeakReductionMode.Quality;

		///// <summary>
		///// This parameter isn't used anymore
		///// </summary>
		//public float minValidDotProductValue = 0.1f;

		/// <summary>
		/// When enabled, reflection probe normalization can only decrease the reflection intensity.
		/// </summary>
		[Tooltip("When enabled, reflection probe normalization can only decrease the reflection intensity.")]
		public bool occlusionOnlyReflectionNormalization = true;

		/// <summary>
		/// Global probe volumes weight. Allows for fading out probe volumes influence falling back to ambient probe
		/// </summary>
		[Tooltip("Global probe volumes weight. Allows for fading out probe volumes influence falling back to ambient probe")]
		[Range(0f, 1f)]
		public float intensityMultiplier = 1f;

		/// <summary>
		/// Multiplier applied on the sky lighting when using sky occlusion.
		/// </summary>
		[Tooltip("Multiplier applied on the sky lighting when using sky occlusion.")]
		[Range(0f, 5f)]
		public float skyOcclusionIntensityMultiplier = 1f;

		/// <summary>
		/// Offset applied at runtime to probe positions in world space.\nThis is not considered while baking.
		/// </summary>
		[Tooltip("Offset applied at runtime to probe positions in world space.\nThis is not considered while baking.")]
		public Vector3 worldOffset = Vector3.zero;



		/// <summary>
		/// <see cref="normalBias"/>
		/// </summary>
		[HideInInspector]
		public float normalBias_runtime;
		/// <summary>
		/// <see cref="viewBias"/>
		/// </summary>
		public float viewBias_runtime;
		/// <summary>
		/// <see cref="scaleBiasWithMinProbeDistance"/>
		/// </summary>
		public bool scaleBiasWithMinProbeDistance_runtime;
		/// <summary>
		/// <see cref="samplingNoise"/>
		/// </summary>
		public float samplingNoise_runtime;
		/// <summary>
		/// <see cref="animateSamplingNoise"/>
		/// </summary>
		public bool animateSamplingNoise_runtime;
		/// <summary>
		/// <see cref="leakReductionMode"/>
		/// </summary>
		public APVLeakReductionMode leakReductionMode_runtime;
		/// <summary>
		/// <see cref="occlusionOnlyReflectionNormalization"/>
		/// </summary>
		public bool occlusionOnlyReflectionNormalization_runtime;
		/// <summary>
		/// <see cref="intensityMultiplier"/>
		/// </summary>
		public float intensityMultiplier_runtime;
		/// <summary>
		/// <see cref="skyOcclusionIntensityMultiplier"/>
		/// </summary>
		public float skyOcclusionIntensityMultiplier_runtime;
		/// <summary>
		/// <see cref="worldOffset"/>
		/// </summary>
		public Vector3 worldOffset_runtime;

		// TODO:NEED IMPLEMENT
		public override void OverrideData(BXVolumeComponment component, float interpFactor)
        {
			var target = component as ProbeVolumesOptions;
			normalBias_runtime = Mathf.Lerp(normalBias, target.normalBias, interpFactor);
			viewBias_runtime = Mathf.Lerp(viewBias, target.viewBias, interpFactor);
			scaleBiasWithMinProbeDistance_runtime = target.scaleBiasWithMinProbeDistance;
			samplingNoise_runtime = Mathf.Lerp(samplingNoise, target.samplingNoise, interpFactor);
			animateSamplingNoise_runtime = target.animateSamplingNoise;
			leakReductionMode_runtime = target.leakReductionMode;
			occlusionOnlyReflectionNormalization_runtime = target.occlusionOnlyReflectionNormalization;
			intensityMultiplier_runtime = Mathf.Lerp(intensityMultiplier, target.intensityMultiplier, interpFactor);
			skyOcclusionIntensityMultiplier_runtime = Mathf.Lerp(skyOcclusionIntensityMultiplier, target.skyOcclusionIntensityMultiplier, interpFactor);
			worldOffset_runtime = Vector3.Lerp(worldOffset, target.worldOffset, interpFactor);
		}

        public override void RefreshData()
        {
			normalBias_runtime = normalBias;
			viewBias_runtime = viewBias;
			scaleBiasWithMinProbeDistance_runtime = scaleBiasWithMinProbeDistance;
			samplingNoise_runtime = samplingNoise;
			animateSamplingNoise_runtime = animateSamplingNoise;
			leakReductionMode_runtime = leakReductionMode;
			occlusionOnlyReflectionNormalization_runtime = occlusionOnlyReflectionNormalization;
			intensityMultiplier_runtime = intensityMultiplier;
			skyOcclusionIntensityMultiplier_runtime = skyOcclusionIntensityMultiplier;
			worldOffset_runtime = worldOffset;
		}
    }
}