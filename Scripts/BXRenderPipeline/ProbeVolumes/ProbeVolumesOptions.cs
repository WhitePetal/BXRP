using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXRenderPipeline
{
    /// <summary>
    /// A volume component that holds settings for the Adaptive Probe Volumes System per-camera options.
    /// </summary>
    [Serializable]
    public sealed class ProbeVolumesOptions : BXVolumeComponment
    {
        public override void OverrideData(BXVolumeComponment component, float interpFactor)
        {
            throw new System.NotImplementedException();
        }

        public override void RefreshData()
        {
            throw new System.NotImplementedException();
        }
    }
}