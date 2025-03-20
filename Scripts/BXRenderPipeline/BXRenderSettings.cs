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
        public static readonly float standard_ev100 = Mathf.Log(standard_aperture * standard_aperture * 100f / (standard_shutter * standard_sensor_sensitvity), 2);
        public static readonly float standard_expourse = 1f / (1.2f * Mathf.Pow(2, standard_ev100));


        private Dictionary<Type, BXVolumeComponment> components;

        private BXVolumeComponment[] stepRenderComponents = new BXVolumeComponment[32 * (int)RenderFeatureStep.MAX];
        private int[] stepCounts = new int[(int)RenderFeatureStep.MAX];

        public BXRenderSettings()
        {
            components = new Dictionary<Type, BXVolumeComponment>();
            //var componentTypes = CoreUtils.GetAllTypesDerivedFrom<BXVolumeComponment>();
            //foreach(var type in componentTypes)
            //{
            //    components.Add(type, ScriptableObject.CreateInstance(type) as BXVolumeComponment);
            //}
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

                var now = components[componentType];
				if (!willAdd.enable)
				{
                    if (now.enableRuntime)
					{
                        if (interpFactor < 0.99f)
                        {
                            now.OnDisabling(interpFactor);
                        }
                        else
                        {
                            now.enableRuntime = false;
                            now.BeDisabled();
                        }
                    }
				}
                // willAdd enable
				else
				{
                    if (!now.enableRuntime)
					{
                        now.enableRuntime = true;
                        now.BeEnabled();
                    }

                    now.OverrideData(willAdd, interpFactor);
                }
            }
		}

        public void CollectRenderComponents()
		{
            for(int i = 0; i < stepCounts.Length; ++i)
			{
                stepCounts[i] = 0;
			}
            foreach(var pair in components)
			{
                if (!pair.Value.enableRuntime)
                    continue;

                RenderFeatureStep step = pair.Value.RenderFeatureStep;

                if (step == RenderFeatureStep.MAX)
                    continue;

                int stepInt = (int)step;
                int start = 32 * stepInt;

                start += stepCounts[stepInt];
                stepRenderComponents[start] = pair.Value;
                stepCounts[stepInt]++;
			}
		}

        public bool Render(RenderFeatureStep step, CommandBuffer cmd, BXMainCameraRenderBase render)
		{
            int stepInt = (int)step;
            int count = stepCounts[stepInt];
            if (count == 0)
                return false;

            int start = 32 * stepInt;
            for (int i = 0; i < count; ++i)
			{
                stepRenderComponents[start + i].OnRender(cmd, render);
			}
            return true;
		}

        public void Dispose()
        {
            components = null;
            stepRenderComponents = null;
            stepCounts = null;
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
