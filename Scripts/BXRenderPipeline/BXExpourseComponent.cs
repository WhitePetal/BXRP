using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BXRenderPipeline
{
	[BXVolumeComponentMenu("PostProcess/Expourse")]
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

        public override RenderFeatureStep RenderFeatureStep => RenderFeatureStep.BeforeRender;

		public override void BeDisabled()
		{
			
		}

		public override void BeEnabled()
		{
            expourseRuntime = BXRenderSettings.standard_expourse;
		}

		public override void OnDisabling(float interpFactor)
		{
            expourseRuntime = Mathf.Lerp(expourseRuntime, BXRenderSettings.standard_expourse, interpFactor);
		}

		public override void OnRender(CommandBuffer cmd, BXMainCameraRenderBase render)
		{
            cmd.SetGlobalFloat(BXShaderPropertyIDs._ReleateExpourse_ID, expourseRuntime / BXRenderSettings.standard_expourse);
        }

		public override void OverrideData(BXVolumeComponment component, float interpFactor)
        {
            if (!component.enable)
                return;
            if (!enable)
                enable = true;
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
