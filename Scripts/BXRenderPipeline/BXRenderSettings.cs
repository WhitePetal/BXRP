using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace BXRenderPipeline
{
    public class BXRenderSettings
    {
        public const float standard_aperture = 12f;
        public const float standard_shutter = 1f / 125f;
        public const float standard_sensor_sensitvity = 100f;
        public readonly float standard_ev100;
        public readonly float standard_expourse;

		private Dictionary<Type, BXVolumeComponment> components;

        public BXRenderSettings()
        {
            standard_ev100 = Mathf.Log(standard_aperture * standard_aperture * 100f / (standard_shutter * standard_sensor_sensitvity), 2);
            standard_expourse = 1f / (1.2f * Mathf.Pow(2, standard_ev100));

            var componentTypes = CoreUtils.GetAllTypesDerivedFrom<BXVolumeComponment>();
            components = new Dictionary<Type, BXVolumeComponment>();
        }

        public void Override(BXRenderSettingsVolume volume, float interpFactor)
		{
            if (volume.components == null || volume.components.Count == 0) return;
            for(int i = 0; i < volume.components.Count; ++i)
            {
                var willAdd = volume.components[i];
                var componentType = willAdd.GetType();

                if (!components.ContainsKey(componentType))
                    components.Add(componentType, (BXVolumeComponment)ScriptableObject.CreateInstance(componentType));
                components[componentType].OverrideData(willAdd, interpFactor);
            }
		}

        public void RefreshComponents()
        {
            foreach(var pair in components)
            {
                pair.Value.RefreshData();
            }
        }

        public T GetComponent<T>() where T : BXVolumeComponment
        {
            Assert.IsTrue(components.ContainsKey(typeof(T)));
            return (T)components[typeof(T)];
        }
    }
}
