using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BXRenderPipeline.LightTransport
{
    public interface IProbeIntegrator : IDisposable
    {
        public enum ResultType : uint
        {
            Success = 0,
            Cancelled,
            JobFailed,
            OutOfMemory,
            InvalidInput,
            LowLevelAPIFailure,
            IOFailed,
            Undefined
        }

        public struct Result
        {
            public ResultType type;
            public String message;


            public Result(ResultType _type, String _message)
            {
                type = _type;
                message = _message;
            }

            public override string ToString()
            {
                if (message.Length == 0)
                    return $"Result type: '{type}'";
                else
                    return $"Result type: '{type}', message: '{message}'";
            }
        }

        public void Prepare(IDeviceContext context, IWorld world, BufferSlice<Vector3> positions, float pushoff, int bounceCount);
        public void SetProgressReporter(BakeProgressState progress);
        public Result IntegrateDirectRadiance(IDeviceContext context, int positionOffset, int positionCount, int sampleCount,
            bool ignoreEnvironment, BufferSlice<SphericalHarmonicsL2> radianceEstimateOut);
        public Result IntegrateIndirectRadiance(IDeviceContext context, int positionOffset, int positionCount, int sampleCount,
            bool ignoreEnvironment, BufferSlice<SphericalHarmonicsL2> radianceEstimateOut);
        public Result IntegrateValidity(IDeviceContext context, int positionOffset, int positionCount, int sampleCount, BufferSlice<float> validityEstimateOut);
        public Result IntegrateOcclusion(IDeviceContext context, int positionOffset, int positionCount, int sampleCount,
            int maxLightsPerProbe, BufferSlice<int> perProbeLightIndices, BufferSlice<float> probeOcclusionEstimateOut);
    }
}