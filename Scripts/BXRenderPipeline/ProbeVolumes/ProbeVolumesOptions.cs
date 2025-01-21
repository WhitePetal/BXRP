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
    [Serializable /**, VolumeComponentMenu("Lighting/Adaptive Probe Volumes Options"), SupportedOnRenderPipeline **/]
    public sealed class ProbeVolumesOptions : BXVolumeComponment
    {
        [Range(0f, 2f)]
        public float normalBias = 0.05f;


        public override void OverrideData(BXVolumeComponment component, float interpFactor)
        {
            //throw new System.NotImplementedException();
        }

        public override void RefreshData()
        {
            //throw new System.NotImplementedException();
        }
    }
}