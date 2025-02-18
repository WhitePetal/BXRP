#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BXRenderPipeline.LightTransport;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace BXRenderPipeline
{
    partial class AdaptiveProbeVolumes
    {
        /// <summary>
        /// Sky occlusion baker
        /// </summary>
        public abstract class SkyOcclusionBaker : IDisposable
        {
            /// <summary>
            /// The current baking step.
            /// </summary>
            public abstract ulong currentStep { get; }

            /// <summary>
            /// The total amount of step.
            /// </summary>
            public abstract ulong stepCount { get; }

            /// <summary>
            /// Array storing the sky occlusion per probe. Expects Layout DC, x, y, z.
            /// </summary>
            public abstract NativeArray<Vector4> occlusion { get; }

            /// <summary>
            /// Array storing the sky shading direction per probe.
            /// </summary>
            public abstract NativeArray<Vector3> shadingDirections { get; }

            /// <summary>
            /// This is called before the start of baking to allow allocating necessary resources.
            /// </summary>
            /// <param name="bakingSet">The baking set that is currently baked.</param>
            /// <param name="probePositions">The probe positions</param>
            public abstract void Initialize(ProbeVolumeBakingSet bakingSet, NativeArray<Vector3> probePositions);

            /// <summary>
            /// Run a step of sky occlusion baking. Baking is considered done when currentStep property equals stepCount.
            /// </summary>
            /// <returns>Return false if bake failed and should be stopped.</returns>
            public abstract bool Step();

            /// <summary>
            /// Performs necessary tasks to free allocated resources.
            /// </summary>
            public abstract void Dispose();

            internal NativeArray<uint> encodedDirections;
            internal void Encode() { encodedDirections = EncodeShadingDirection(shadingDirections); }

            private const int k_MaxProbeCountPerBatch = 65535;
            static readonly int _SkyShadingPrecomputedDirection = Shader.PropertyToID("_SkyShadingPrecomputedDirection");
            static readonly int _SkyShadingDirections = Shader.PropertyToID("_SkyShadingDirections");
            static readonly int _SkyShadingIndices = Shader.PropertyToID("_SkyShadingIndices");
            static readonly int _ProbeCount = Shader.PropertyToID("_ProbeCount");

            internal static NativeArray<uint> EncodeShadingDirection(NativeArray<Vector3> directions)
            {
                BXRenderPipeline.TryGetRenderCommonSettings(out var settings);
                var cs = settings.probeVolumeBakingResources.skyOcclusionCS;
                int kernel = cs.FindKernel("EncodeShadingDirection");

                ProbeVolumeConstantRuntimeResources.Initialize();
                var precomputedShadingDirections = ProbeReferenceVolume.instance.GetRuntimeResources().SkyPrecomputedDirections;

                int probeCount = directions.Length;
                int batchSize = Mathf.Min(k_MaxProbeCountPerBatch, probeCount);
                int batchCount = BXUtils.DivRoundUp(probeCount, k_MaxProbeCountPerBatch);

                var directionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, batchSize, Marshal.SizeOf<Vector3>());
                var encodedBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, batchSize, Marshal.SizeOf<uint>());

                var directionResults = new NativeArray<uint>(probeCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

                for(int batchIndex = 0; batchIndex < batchCount; ++batchIndex)
                {
                    int batchOffset = batchIndex * k_MaxProbeCountPerBatch;
                    int probeInBatch = Mathf.Min(probeCount - batchOffset, k_MaxProbeCountPerBatch);

                    directionBuffer.SetData(directions, batchOffset, 0, probeInBatch);

                    cs.SetBuffer(kernel, _SkyShadingPrecomputedDirection, precomputedShadingDirections);
                    cs.SetBuffer(kernel, _SkyShadingDirections, directionBuffer);
                    cs.SetBuffer(kernel, _SkyShadingIndices, encodedBuffer);

                    cs.SetInt(_ProbeCount, probeInBatch);
                    cs.Dispatch(kernel, BXUtils.DivRoundUp(probeCount, 64), 1, 1);

                    var batchResult = directionResults.GetSubArray(batchOffset, probeInBatch);
                    AsyncGPUReadback.RequestIntoNativeArray(ref batchResult, encodedBuffer, probeInBatch * sizeof(uint), 0).WaitForCompletion();
                }

                directionBuffer.Dispose();
                encodedBuffer.Dispose();

                return directionResults;
            }

            internal static uint EncodeSkyShadingDirection(Vector3 direction)
            {
                var precomputedDirections = ProbeVolumeConstantRuntimeResources.GetSkySamplingDirections();

                uint indexMax = 255;
                float bestDot = -10.0f;
                uint bestIndex = 0;

                for(uint index = 0; index < indexMax; ++index)
                {
                    float currentDot = Vector3.Dot(direction, precomputedDirections[index]);
                    if(currentDot > bestDot)
                    {
                        bestDot = currentDot;
                        bestIndex = index;
                    }
                }

                return bestIndex;
            }
        }

        class DefaultSkyOcclusion : SkyOcclusionBaker
        {
            private const int k_MaxProbeCountPerBatch = 128 * 1024;
            private const float k_SkyOcclusionOffsetRay = 0.015f;
            private const int k_SampleCountPerStep = 16;

            private static readonly int _SampleCount = Shader.PropertyToID("_SampleCount");
            private static readonly int _SampleId = Shader.PropertyToID("_SampleId");
            private static readonly int _MaxBounces = Shader.PropertyToID("_MaxBounces");
            private static readonly int _OffsetRay = Shader.PropertyToID("_OffsetRay");
            private static readonly int _ProbePositions = Shader.PropertyToID("_ProbePositions");
            private static readonly int _SkyOcclusionOut = Shader.PropertyToID("_SkyOcclusionOut");
            private static readonly int _SkyShadingOut = Shader.PropertyToID("_SkyShadingOut");
            private static readonly int _AverageAlbedo = Shader.PropertyToID("_AverageAlbedo");
            private static readonly int _BackFaceCulling = Shader.PropertyToID("_BackFaceCulling");
            private static readonly int _BakeSkyShadingDirection = Shader.PropertyToID("_BackSkyShadingDirection");
            private static readonly int _SobolBuffer = Shader.PropertyToID("_SobolMatricesBuffer");

            private int skyOcclusionBackFaceCulling;
            private float skyOcclusionAverageAlbedo;
            private int probeCount;
            private ulong step;

            // Input data
            private NativeArray<Vector3> probePositions;
            private int currentJob;
            private int sampleIndex;
            private int batchIndex;

            public BakeJob[] jobs;

            // Output buffers
            GraphicsBuffer occlusionOutputBuffer;
            GraphicsBuffer shadingDirectionBuffer;
            NativeArray<Vector4> occlusionResults;
            NativeArray<Vector3> directionResults;

            public override NativeArray<Vector4> occlusion => occlusionResults;
            public override NativeArray<Vector3> shadingDirections => directionResults;

            AccelStructAdapter m_AccelerationStructure;

            public override ulong currentStep => step;
            public override ulong stepCount => (ulong)probeCount;


            public override void Initialize(ProbeVolumeBakingSet bakingSet, NativeArray<Vector3> positions)
            {
                skyOcclusionAverageAlbedo = bakingSet.skyOcclusionAverageAlbedo;
                skyOcclusionBackFaceCulling = 0; // see PR #40707

                currentJob = 0;
                sampleIndex = 0;
                batchIndex = 0;

                step = 0;
                probeCount = bakingSet.skyOcclusion ? positions.Length : 0;
                probePositions = positions;

                if (stepCount == 0)
                    return;

                // Alocate array storing results
                occlusionResults = new NativeArray<Vector4>(probeCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                if (bakingSet.skyOcclusionShadingDirection)
                    directionResults = new NativeArray<Vector3>(probeCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

                // Create acceleration structure
            }

            private static AccelStructAdapter BuildAccelerationStructure()
            {
                //var accelStruct = 
                return null;
            }

            public override void Dispose()
            {
                throw new NotImplementedException();
            }

            public override bool Step()
            {
                throw new NotImplementedException();
            }
        }
    }
}


#endif