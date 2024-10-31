using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXRenderPipeline
{
    public class BXBloomComponent : BXVolumeComponment
    {
        [Range(0f, 10f)]
        public float threshold = 1f;
        [Range(0f, 1f)]
        public float threshold_knne = 0.7f;
        [Range(0f, 10f)]
        public float bloom_strength = 1f;
        [Range(0f, 1f)]
        public float bright_clamp = 1f;

        [HideInInspector]
        public float threshold_runtime;
        [HideInInspector]
        public float threshold_knne_runtime;
        [HideInInspector]
        public float bloom_strength_runtime;
        [HideInInspector]
        public float bright_clamp_runtime;


        public override void OverrideData(BXVolumeComponment component, float interpFactor)
        {
            var target = component as BXBloomComponent;
            threshold_runtime = Mathf.Lerp(threshold, target.threshold, interpFactor);
            threshold_knne_runtime = Mathf.Lerp(threshold_knne, target.threshold_knne, interpFactor);
            bloom_strength_runtime = Mathf.Lerp(bloom_strength, target.bloom_strength, interpFactor);
            bright_clamp_runtime = Mathf.Lerp(bright_clamp, target.bright_clamp, interpFactor);
        }

        public override void RefreshData()
        {
            threshold_runtime = threshold;
            threshold_knne_runtime = threshold_knne;
            bloom_strength_runtime = bloom_strength;
            bright_clamp_runtime = bright_clamp;
        }
    }
}
