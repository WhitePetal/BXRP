using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXRenderPipeline.LightTransport
{
    public class BakeProgressState : IDisposable
    {
        public void Cancel()
        {

        }

        public float Progress()
        {
            return 0f;
        }

        public void SetTotalWorkSteps(UInt64 total)
        {

        }

        public void IncrementCompletedWorkSteps(UInt64 steps)
        {

        }

        public bool WasCancelled()
        {
            return false;
        }


        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
