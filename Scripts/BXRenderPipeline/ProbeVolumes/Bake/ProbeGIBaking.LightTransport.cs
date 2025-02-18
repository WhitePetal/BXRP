#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BXRenderPipeline.LightTransport;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace BXRenderPipeline
{
    partial class AdaptiveProbeVolumes
    {
        /// <summary>
        /// Lighting baker
        /// </summary>
        public abstract class LightingBaker : IDisposable
        {
            /// <summary>
            /// Indicates that the Step method can be safely called from a thread.
            /// </summary>
            public virtual bool isThreadSafe => false;
            /// <summary>
            /// Set to true when the main thread cancels baking
            /// </summary>
            public static bool cancel { get; internal set; }

            /// <summary>
            /// The current baking step.
            /// </summary>
            public abstract ulong currentStep { get; }
            /// <summary>
            /// The total amount of step
            /// </summary>
            public abstract ulong stepCount { get; }

            /// <summary>
            /// Array storing the probe lighting as Spherical Harmonics.
            /// </summary>
            public abstract NativeArray<SphericalHarmonicsL2> irradiance { get; }
            /// <summary>
            /// Array storing the probe validity. A value of 1 means a probe is invalid
            /// </summary>
            public abstract NativeArray<float> validity { get; }
            /// <summary>
            /// Array storing 4 light occlusion values for each probe.
            /// </summary>
            public abstract NativeArray<Vector4> occlusion { get; }

            /// <summary>
            /// This is called before the start of baking to allow allocating necessary resources.
            /// </summary>
            /// <param name="bakeProbeOcclusion">Whether to bake occlusion for mixed lights for each probe</param>
            /// <param name="probePositions">The probe positions. Also contains reflection probe positions used for normalization.</param>
            public abstract void Initialize(bool bakeProbeOcclusion, NativeArray<Vector3> probePositions);

            /// <summary>
            /// This is called before the start of baking to allow allocating necessary resources.
            /// </summary>
            /// <param name="bakeProbeOcclusion">Whether to bake occlusion for mixed lights for each probe.</param>
            /// <param name="probePositions">The probe positions. Also contains reflection probe positions used for normalization.</param>
            /// <param name="bakedRenderingLayerMasks">The rendering layer masks assigned to each probe. It is used when fixing seams between subdivision levels</param>
            public abstract void Initialize(bool bakeProbeOcclusion, NativeArray<Vector3> probePositions, NativeArray<uint> bakedRenderingLayerMasks);

            /// <summary>
            /// Run a step of light baking. Baking is considered done when currentStep property equals stepCount.
            /// </summary>
            /// <returns>Return false if bake failed and should be stopped.</returns>
            public abstract bool Step();

            /// <summary>
            /// Performs necessary tasks to free allocated resources.
            /// </summary>
            public abstract void Dispose();
        }

        class DefaultLightTransport : LightingBaker
        {
            public override bool isThreadSafe => true;

            private int bakedProbeCount;
            private NativeArray<Vector3> positions;



            public override ulong currentStep => throw new NotImplementedException();

            public override ulong stepCount => throw new NotImplementedException();

            public override NativeArray<SphericalHarmonicsL2> irradiance => throw new NotImplementedException();

            public override NativeArray<float> validity => throw new NotImplementedException();

            public override NativeArray<Vector4> occlusion => throw new NotImplementedException();

            public override void Dispose()
            {
                throw new NotImplementedException();
            }

            public override void Initialize(bool bakeProbeOcclusion, NativeArray<Vector3> probePositions)
            {
                throw new NotImplementedException();
            }

            public override void Initialize(bool bakeProbeOcclusion, NativeArray<Vector3> probePositions, NativeArray<uint> bakedRenderingLayerMasks)
            {
                throw new NotImplementedException();
            }

            public override bool Step()
            {
                throw new NotImplementedException();
            }
        }

        struct BakeJob
        {
            public Bounds aabb;
            public ProbeReferenceVolume.Volume obb;
            public ProbeAdjustmentVolume touchup;

            public int startOffset;
            public int probeCount;

            public int directSampleCount;
            public int indirectSampleCount;
            public int validitySampleCount;
            public int occlusionSampleCount;
            public int maxBounces;

            public int skyOcclusionBakingSamples;
            public int skyOcclusionBakingBounces;

            public float indirectScale;
            public bool ignoreEnvironement;
            public BakeProgressState progress;
            public ulong stepCount => (ulong)probeCount;
            public ulong currentStep => (ulong)Mathf.Min(progress.Progress() * 0.01f / (float)(directSampleCount + indirectSampleCount + validitySampleCount), stepCount);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Create(ProbeVolumeBakingSet bakingSet, LightingSettings lightingSettings, bool ignoreEnvironement)
            {
                skyOcclusionBakingSamples = bakingSet != null ? bakingSet.skyOcclusionBakingSamples : 0;
                skyOcclusionBakingBounces = bakingSet != null ? bakingSet.skyOcclusionBakingBounces : 0;

                int indirectSampleCount = Math.Max(lightingSettings.indirectSampleCount, lightingSettings.environmentSampleCount);
                Create(lightingSettings, ignoreEnvironement, lightingSettings.directSampleCount, indirectSampleCount,
                    (int)lightingSettings.lightProbeSampleCountMultiplier, lightingSettings.maxBounces);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void Create(LightingSettings lightingSettings, bool ignoreEnvironement, (ProbeReferenceVolume.Volume obb, Bounds aabb, ProbeAdjustmentVolume touchup) volume)
            {
                obb = volume.obb;
                aabb = volume.aabb;
                touchup = volume.touchup;

                skyOcclusionBakingSamples = touchup.skyOcclusionSampleCount;
                skyOcclusionBakingBounces = touchup.skyOcclusionMaxBounces;

                Create(lightingSettings, ignoreEnvironement, touchup.directSampleCount, touchup.indirectSampleCount, touchup.sampleCountMultiplier, touchup.maxBounces);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void Create(LightingSettings lightingSettings, bool ignoreEnvironement, int directSampleCount, int indirectSampleCount, int sampleCountMultiplier, int maxBounces)
            {
                // We could prealloate wrt touchup aabb volume, or total brick count for the global job
                progress = new BakeProgressState();

                this.directSampleCount = directSampleCount * sampleCountMultiplier;
                this.indirectSampleCount = indirectSampleCount * sampleCountMultiplier;
                this.validitySampleCount = indirectSampleCount * sampleCountMultiplier;
                this.occlusionSampleCount = directSampleCount * sampleCountMultiplier;
                this.maxBounces = maxBounces;

                this.indirectScale = lightingSettings.indirectScale;
                this.ignoreEnvironement = ignoreEnvironement;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Contains(Vector3 point)
            {
                return touchup.ContainsPoint(obb, aabb.center, point);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                progress.Dispose();
            }
        }


    }
}
#endif