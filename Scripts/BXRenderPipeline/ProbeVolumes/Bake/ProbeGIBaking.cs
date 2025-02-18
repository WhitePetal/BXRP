#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


namespace BXRenderPipeline
{
    using Brick = ProbeBrickIndex.Brick;
    using IndirectionEntryInfo = ProbeReferenceVolume.IndirectionEntryInfo;
    using TouchupVolumeWithBoundsList = List<(ProbeReferenceVolume.Volume obb, Bounds aabb, ProbeAdjustmentVolume volume)>;

    internal struct BakingCell
    {
        public Vector3Int position;
        public int index;

        public Brick[] bricks;
        public Vector3[] probePositions;
        public SphericalHarmonicsL2[] sh;
        public byte[,] validityNeighbourMask;
        public Vector4[] skyOcclusionDataL0L1;
        public byte[] skyShadingDirectionIndices;
        public float[] validity;
        public Vector4[] probeOcclusion;
        public byte[] layerValidity;
        public Vector3[] offsetVectors;
        public float[] touchupVolumeInteraction;

        public int minSubdiv;
        public int indexChunkCount;
        public int shChunkCount;
        public IndirectionEntryInfo[] indirectionEntryInfo;

        public Bounds bounds;

        internal void ComputeBounds(float cellSize)
        {
            var center = new Vector3((position.x + 0.5f) * cellSize, (position.y + 0.5f) * cellSize, (position.z + 0.5f) * cellSize);
            bounds = new Bounds(center, new Vector3(cellSize, cellSize, cellSize));
        }

        internal TouchupVolumeWithBoundsList SelectIntersectingAdjustmentVolumes(TouchupVolumeWithBoundsList touchupVolumesAndBounds)
        {
            // Find the subset of touchup volumes that will be considered for this cell.
            // Capacity of the list to cover the worst case.
            var localTouchupVolumes = new TouchupVolumeWithBoundsList(touchupVolumesAndBounds.Count);
            foreach(var touchup in touchupVolumesAndBounds)
            {
                if (touchup.aabb.Intersects(bounds))
                    localTouchupVolumes.Add(touchup);
            }
            return localTouchupVolumes;
        }

        internal static void CompressSH(ref SphericalHarmonicsL2 shv, float intensityScale, bool clearForDilation)
        {
            // Compress the range of all coefficients but the DC component to [0..1]
            // Upper bounds taken from http://ppsloan.org/publications/Sig20_Advances.pptx
            // Divide each coefficient by DC*f to get to [-1, 1] where f is from slide 33
            for(int rgb = 0; rgb < 3; ++rgb)
            {
                for (int k = 0; k < 9; ++k)
                    shv[rgb, k] *= intensityScale;

                var l0 = shv[rgb, 0];

                if(l0 == 0.0f)
                {
                    shv[rgb, 0] = 0.0f;
                    for (int k = 1; k < 9; ++k)
                        shv[rgb, k] = 0.5f;
                }
                else if (clearForDilation)
                {
                    for (int k = 0; k < 9; ++k)
                        shv[rgb, k] = 0.0f;
                }
                else
                {
                    // TODO: We're working on irradiance instead of radiance coefficients
                    //      Add safety margin 2 to avoid out-of-bounds values
                    float l1scale = 2.0f; // Should be: 3/(2*sqrt(3)) * 2, but rounding to 2 to issues we are observing.
                    float l2scale = 3.5777088f; // 4/sqrt(5) * 2

                    // L_1^m
                    shv[rgb, 1] = shv[rgb, 1] / (l0 * l1scale * 2.0f) + 0.5f;
                    shv[rgb, 2] = shv[rgb, 2] / (l0 * l1scale * 2.0f) + 0.5f;
                    shv[rgb, 3] = shv[rgb, 3] / (l0 * l1scale * 2.0f) + 0.5f;

                    // L_2^-2
                    shv[rgb, 4] = shv[rgb, 4] / (l0 * l2scale * 2.0f) + 0.5f;
                    shv[rgb, 5] = shv[rgb, 5] / (l0 * l2scale * 2.0f) + 0.5f;
                    shv[rgb, 6] = shv[rgb, 6] / (l0 * l2scale * 2.0f) + 0.5f;
                    shv[rgb, 7] = shv[rgb, 7] / (l0 * l2scale * 2.0f) + 0.5f;
                    shv[rgb, 8] = shv[rgb, 8] / (l0 * l2scale * 2.0f) + 0.5f;

                    for (int coeff = 1; coeff < 9; ++coeff)
                        shv[rgb, coeff] = Mathf.Clamp01(shv[rgb, coeff]);
                }
            }
        }

        internal static void DecompressSH(ref SphericalHarmonicsL2 shv)
        {
            for(int rgb = 0; rgb < 3; ++rgb)
            {
                var l0 = shv[rgb, 0];

                // See CompressSH
                float l1scale = 2.0f;
                float l2scale = 3.5777088f;

                // L_1^m
                shv[rgb, 1] = (shv[rgb, 1] - 0.5f) * (l0 * l1scale * 2.0f);
                shv[rgb, 2] = (shv[rgb, 2] - 0.5f) * (l0 * l1scale * 2.0f);
                shv[rgb, 3] = (shv[rgb, 3] - 0.5f) * (l0 * l1scale * 2.0f);

                // L_2^-2
                shv[rgb, 4] = (shv[rgb, 4] - 0.5f) * (l0 * l2scale * 2.0f);
                shv[rgb, 5] = (shv[rgb, 5] - 0.5f) * (l0 * l2scale * 2.0f);
                shv[rgb, 6] = (shv[rgb, 6] - 0.5f) * (l0 * l2scale * 2.0f);
                shv[rgb, 7] = (shv[rgb, 7] - 0.5f) * (l0 * l2scale * 2.0f);
                shv[rgb, 8] = (shv[rgb, 8] - 0.5f) * (l0 * l2scale * 2.0f);
            }
        }

        private void SetSHCoefficients(int i, SphericalHarmonicsL2 value, float intensityScale, float valid, in ProbeDilationSettings dilationSettings)
        {
            bool clearForDilation = dilationSettings.enableDilation && dilationSettings.dilationDistance > 0.0f && valid > dilationSettings.dilationValidityThreshold;
            CompressSH(ref value, intensityScale, clearForDilation);

            SphericalHarmonicsL2Utils.SetL0(ref sh[i], new Vector3(value[0, 0], value[1, 0], value[2, 0]));
            SphericalHarmonicsL2Utils.SetL1R(ref sh[i], new Vector3(value[0, 3], value[0, 1], value[0, 2]));
            SphericalHarmonicsL2Utils.SetL1G(ref sh[i], new Vector3(value[1, 3], value[1, 1], value[1, 2]));
            SphericalHarmonicsL2Utils.SetL1B(ref sh[i], new Vector3(value[2, 3], value[2, 1], value[2, 2]));

            SphericalHarmonicsL2Utils.SetCoefficient(ref sh[i], 4, new Vector3(value[0, 4], value[1, 4], value[2, 4]));
            SphericalHarmonicsL2Utils.SetCoefficient(ref sh[i], 5, new Vector3(value[0, 5], value[1, 5], value[2, 5]));
            SphericalHarmonicsL2Utils.SetCoefficient(ref sh[i], 6, new Vector3(value[0, 6], value[1, 6], value[2, 6]));
            SphericalHarmonicsL2Utils.SetCoefficient(ref sh[i], 7, new Vector3(value[0, 7], value[1, 7], value[2, 7]));
            SphericalHarmonicsL2Utils.SetCoefficient(ref sh[i], 8, new Vector3(value[0, 8], value[1, 8], value[2, 8]));
        }

        private void ReadAdjustmentVolumes(ProbeVolumeBakingSet bakingSet, BakingBatch bakingBatch, TouchupVolumeWithBoundsList localTouchupVolumes, int i, float validity,
            ref byte validityMask, out bool invalidatedProbe, out float intensityScale, out uint? skyShadingDirectionOverride)
        {
            invalidatedProbe = false;
            intensityScale = 1.0f;
            skyShadingDirectionOverride = null;

            foreach(var touchup in localTouchupVolumes)
            {
                var touchupBound = touchup.aabb;
                var touchupVolume = touchup.volume;

                // We check a small box arround the probe to give some leniency (a couple of centimerters).
                var probeBounds = new Bounds(probePositions[i], new Vector3(0.02f, 0.02f, 0.02f));
                if(touchupVolume.IntersectsVolume(touchup.obb, touchup.aabb, probeBounds))
                {
                    if(touchupVolume.mode == ProbeAdjustmentVolume.Mode.InvalidateProbes)
                    {
                        invalidatedProbe = true;

                        if(validity < 0.05f) //  We just want to add probes that were not already invalid or close to.
                        {
                            // We check as below 1 but bigger than 0 in the debug shader, so any value <1 will do to signify touched up.
                            touchupVolumeInteraction[i] = 0.5f;

                            bakingBatch.forceInvalidatedProbesAndTouchupVols[probePositions[i]] = touchupBound;
                        }
                        break;
                    }
                    else if(touchupVolume.mode == ProbeAdjustmentVolume.Mode.OverrideValidityThreshold)
                    {
                        float thresh = (1.0f - touchupVolume.overriddenDilationThreshold);
                        // The 1.0f + is used to determine the action (debug shader tests above 1), then we add the threshold to be able to retrieve it in debug phase.
                        touchupVolumeInteraction[i] = 1.0f + thresh;
                        bakingBatch.customDilationThresh[(index, i)] = thresh;
                    }
                    else if(touchupVolume.mode == ProbeAdjustmentVolume.Mode.OverrideSkyDirection && bakingSet.skyOcclusion && bakingSet.skyOcclusionShadingDirection)
                    {
                        skyShadingDirectionOverride = AdaptiveProbeVolumes.SkyOcclusionBaker.EncodeSkyShadingDirection(touchupVolume.skyDirection);
                    }
                    else if(touchupVolume.mode == ProbeAdjustmentVolume.Mode.OverrideRenderingLayerMask && bakingSet.useRenderingLayers)
                    {
                        switch (touchupVolume.renderingLayerMaskOperation)
                        {
                            case ProbeAdjustmentVolume.RenderingLayerMaskOperation.Override:
                                validityMask = touchupVolume.renderingLayerMask;
                                break;
                            case ProbeAdjustmentVolume.RenderingLayerMaskOperation.Add:
                                validityMask |= touchupVolume.renderingLayerMask;
                                break;
                            case ProbeAdjustmentVolume.RenderingLayerMaskOperation.Remove:
                                validityMask &= (byte)(~touchupVolume.renderingLayerMask);
                                break;
                        }
                    }

                    if (touchupVolume.mode == ProbeAdjustmentVolume.Mode.IntensityScale)
                        intensityScale = touchupVolume.intensityScale;
                    if (intensityScale != 1.0f)
                        touchupVolumeInteraction[i] = 2.0f + intensityScale;
                }
            }

            //if(validity < 0.05f && bakingBatch.invalidatedPositions.ContainsKey(probePositions[i]) && bakingBatch.invalidatedPositions[i])
            //{
            //    if (!bakingBatch.forceInvalidatedProbesAndTouchupVols.ContainsKey(probePositions[i]))
            //        bakingBatch.forceInvalidatedProbesAndTouchupVols.Add(probePositions[i], new Bounds());

            //    invalidatedProbe = true;
            //}
        }

        private static bool m_IsInit = false;
        private static BakingBatch m_BakingBatch;
        private static ProbeVolumeBakingSet m_BakingSet = null;
        private static TouchupVolumeWithBoundsList s_AdjustmentVolumes;

        private static Bounds globalBounds = new Bounds();
        private static Vector3Int minCellPosition = Vector3Int.one * int.MaxValue;
        private static Vector3Int maxCellPosition = Vector3Int.one * int.MinValue;

        private static int pvHashesAtBakeStart = -1;
        //private static APVRTContext s_TracingContext;
    }

    class BakingBatch
    {
        // Mapping for explicit invalidation, whether it comes from the auto finding of occluders or from the touch up volumes
        // TODO: This is not used yet. Will soon.
        public Dictionary<Vector3, bool> invalidatedPositions = new();

        public Dictionary<(int, int), float> customDilationThresh = new();
        public Dictionary<Vector3, Bounds> forceInvalidatedProbesAndTouchupVols = new();
    }

    class ProbeGIBaking
    {

    }
}
#endif