#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXRenderPipelineEditor.LightBaking
{
    internal static partial class LightBaker
    {
        public enum ResultType : uint
        {
            Success = 0,
            Cancelled,
            JobFailed,
            OutOfMemory,
            InvalidInput,
            LowLevelAPIFailure,
            FailedCreatingJobQueue,
            IOFailed,
            ConnectedToBaker,
            Undefined
        }

        public struct Result
        {
            public ResultType type;
            public string message;
            public override string ToString()
            {
                if (message.Length == 0)
                    return $"Result type: '{type}'";
                return $"Result type: '{type}', message: '{message}'";
            }
        }
    }
}
#endif