using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXRenderPipeline
{
    public class BXExpourseComponent : BXVolumeComponment
    {
        public float aperture = 22f;
        public float shutter = 1f / 125f;
        public float sensor_sensitvity = 100f;

        public float ev100Runtime;
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
