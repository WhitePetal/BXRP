using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXRenderPipeline
{
    public class BXExpourseComponent : BXVolumeComponment
    {
        [Header("光圈")]
        public float aperture = 22f;
        [Header("快门")]
        public float shutter = 1f / 125f;
        [Header("感光度")]
        public float sensor_sensitvity = 100f;

        [HideInInspector]
        public float ev100Runtime;
        [HideInInspector]
        public float expourseRuntime;

        public override void OverrideData(BXVolumeComponment component, float interpFactor)
        {
            var target = component as BXExpourseComponent;
            float ev100Target = ComputeEV100(target.aperture, target.shutter, target.sensor_sensitvity);
            float expourseTarget = ComputeExpourse(ev100Target);

            ev100Runtime = Mathf.Lerp(ev100Runtime, ev100Target, interpFactor);
            expourseRuntime = Mathf.Lerp(expourseRuntime, expourseTarget, interpFactor);

            //Debug.Log("exp: " + expourseRuntime + " === " + interpFactor);
        }

        public override void RefreshData()
        {
            ev100Runtime = ComputeEV100(aperture, shutter, sensor_sensitvity);
            expourseRuntime = ComputeExpourse(ev100Runtime);
        }

        private float ComputeEV100(float aperture, float shutter, float sensor_sensitvity)
        {
            return Mathf.Log(aperture * aperture * 100f / (shutter * sensor_sensitvity), 2);
        }

        private float ComputeExpourse(float ev100)
        {
            return 1f / (1.2f * Mathf.Pow(2, ev100));
        }
    }
}
