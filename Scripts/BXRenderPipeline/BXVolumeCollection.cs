using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace BXRenderPipeline
{
    internal class BXVolumeCollection
    {
        internal const int k_MaxLayerCount = 32;

        readonly Dictionary<int, List<BXRenderSettingsVolume>> m_SortedVolumes = new();

        readonly List<BXRenderSettingsVolume> m_Volumes = new();

        readonly Dictionary<int, bool> m_SortNeeded = new ();

        public int count => m_Volumes.Count;

        public bool Register(BXRenderSettingsVolume volume, int layer)
		{
            if (volume == null)
                throw new ArgumentNullException(nameof(volume), "The volume to register is null");

            if (m_Volumes.Contains(volume)) return false;

            m_Volumes.Add(volume);

            foreach(var kvp in m_SortedVolumes)
			{
                if ((kvp.Key & (1 << layer)) != 0 && !kvp.Value.Contains(volume))
                    kvp.Value.Add(volume);
			}

            SetLayerIndexDirty(layer);
            return true;
		}

        public bool Unregister(BXRenderSettingsVolume volume, int layer)
		{
            if (volume == null)
                throw new ArgumentNullException(nameof(volume), "The volume to unregister is null");

            m_Volumes.Remove(volume);

            foreach (var kvp in m_SortedVolumes)
            {
                if ((kvp.Key & (1 << layer)) == 0)
                    continue;

                kvp.Value.Remove(volume);
            }

            SetLayerIndexDirty(layer);

            return true;
        }

        public bool ChangeLayer(BXRenderSettingsVolume volume, int previousLayerIndex, int currentLayerIndex)
		{
            if (volume == null)
                throw new ArgumentNullException(nameof(volume), "The volume to change layer is null");

            Assert.IsTrue(previousLayerIndex >= 0 && previousLayerIndex <= k_MaxLayerCount, "Invalid layer bit");
            Unregister(volume, previousLayerIndex);

            return Register(volume, currentLayerIndex);
        }

        internal static void SortByPriority(List<BXRenderSettingsVolume> volumes)
		{
            for(int i = 1; i < volumes.Count; ++i)
			{
                var temp = volumes[i];
                int j = i - 1;

                while(j >= 0 && volumes[j].priortiy > temp.priortiy)
				{
                    volumes[j + 1] = volumes[j];
                    j--;
				}

                volumes[j + 1] = temp;
			}
		}

        public List<BXRenderSettingsVolume> GrabVolumes(LayerMask mask)
		{
            List<BXRenderSettingsVolume> list;

            if(!m_SortedVolumes.TryGetValue(mask, out list))
			{
                list = new List<BXRenderSettingsVolume>();

                var numVolumes = m_Volumes.Count;
                for(int i = 0; i < numVolumes; ++i)
				{
                    var volume = m_Volumes[i];
                    if ((mask & (1 << volume.gameObject.layer)) == 0) continue;
                    list.Add(volume);
                    m_SortNeeded[mask] = true;
				}

                m_SortedVolumes.Add(mask, list);
			}

            if(m_SortNeeded.TryGetValue(mask, out var sortNeeded) && sortNeeded)
			{
                m_SortNeeded[mask] = false;
                SortByPriority(list);
			}

            return list;
		}

        public void SetLayerIndexDirty(int layerIndex)
		{
            Assert.IsTrue(layerIndex >= 0 && layerIndex <= k_MaxLayerCount, "Invalid layer bit");

            foreach(var kvp in m_SortedVolumes)
			{
                var mask = kvp.Key;

                if ((mask & (1 << layerIndex)) != 0)
                    m_SortNeeded[mask] = true;
			}
		}
    }
}
