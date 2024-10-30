using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXRenderPipeline
{
    public class BXRenderSettings
    {
        public const float standard_aperture = 22f;
        public const float standard_shutter = 1f / 125f;
        public const float standard_sensor_sensitvity = 100f;
        public readonly float standard_ev100;
        public readonly float standard_expourse;

        public float expourse;

		private Dictionary<string, BXVolumeComponment> componentsDic;
		private Dictionary<string, BXVolumeComponment> dstComDic;
		public List<BXVolumeComponment> componments;
		public BXVolumeComponment[] componmentsPool;

        public BXRenderSettings()
        {
            standard_ev100 = Mathf.Log(standard_aperture * standard_aperture * 100f / (standard_shutter * standard_sensor_sensitvity), 2);
            standard_expourse = 1f / (1.2f * Mathf.Pow(2, standard_ev100));
			componentsDic = new Dictionary<string, BXVolumeComponment>();
			componments = new List<BXVolumeComponment>();
        }

        public void Override(BXRenderSettingsVolume volume, float interpFactor)
		{
            float ev100 = volume.ev100;
            this.expourse = ConvertEV100ToExposure(ev100) / standard_expourse;

			//float factor;
			//for(int i = 0; i < volume.components.Count; ++i)
			//{
			//	var willAdd = volume.components[i];
			//	if(componentsDic.ContainsKey(willAdd.name)){
			//	componentsDic.Add(willAdd.name, willAdd);
			//	componmentsPool[i] = willAdd.CopyCreate();
			//}
			//for(int i = 0; i < componments.Count; ++i)
			//{
			//	var component = componments[i];
			//	if (componentsDic.ContainsKey(component.name))
			//	{
			//		componmentsPool
			//	}
			//}
		}

        private float ConvertEV100ToExposure(float ev100)
        {
            float maxLuminance = 1.2f * Mathf.Pow(2, ev100);
            return 1f / maxLuminance;
        }
    }
}
