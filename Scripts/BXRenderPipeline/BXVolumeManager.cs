using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace BXRenderPipeline
{
    public sealed partial class BXVolumeManager
    {
        private static readonly ProfilerMarker k_ProfilerMarkerUpdate = new ProfilerMarker("BXVolumeManager.Update");
        private static readonly ProfilerMarker k_ProfilerMarkerReplaceData = new ProfilerMarker("BXVolumeManager.ReplaceData");
        private static readonly ProfilerMarker k_ProfilerMarkerEvaluateVolumeDefaultState = new ProfilerMarker("BXVolumeManager.EvaluateVolumeDefaultState");

        private static readonly Lazy<BXVolumeManager> s_Instance = new Lazy<BXVolumeManager>(() => new BXVolumeManager());

        public static BXVolumeManager instance = s_Instance.Value;

        public BXRenderSettings renderSettings { get; private set; }

        private readonly BXVolumeCollection m_VolumeCollection = new BXVolumeCollection();

        private readonly List<Collider> m_TempColliders = new(8);

        private BXRenderSettings m_RenderSettings;


        public bool isInitialized { get; private set; }

        public void Initialize()
        {
            Debug.Assert(!isInitialized);

            m_RenderSettings = new BXRenderSettings();
            renderSettings = m_RenderSettings;

            isInitialized = true;
        }

        public void Deinitialize()
		{
            Debug.Assert(isInitialized);
            m_RenderSettings.Dispose();
            m_RenderSettings = null;
            isInitialized = false;
		}

        public void Register(BXRenderSettingsVolume volume)
		{
            m_VolumeCollection.Register(volume, volume.gameObject.layer);
		}

        public void Unregister(BXRenderSettingsVolume volume)
		{
            m_VolumeCollection.Unregister(volume, volume.gameObject.layer);
		}

        internal void SetLayerDirty(int layer)
		{
            m_VolumeCollection.SetLayerIndexDirty(layer);
		}

        internal void UpdateVolumeLayer(BXRenderSettingsVolume volume, int prevLayer, int newLayer)
		{
            m_VolumeCollection.ChangeLayer(volume, prevLayer, newLayer);
		}

        private void OverrideData(BXRenderSettingsVolume volume, float interpFactor)
		{
            m_RenderSettings.Override(volume, interpFactor);
		}

        private bool CheckUpdateRequired()
		{
            if (m_VolumeCollection.count == 0)
                return false;

            return true;
		}

        public void Update(Transform trigger, LayerMask layerMask)
        {
            using var profilerScope = k_ProfilerMarkerUpdate.Auto();

            if (!isInitialized) return;

            if (!CheckUpdateRequired()) return;

            bool onlyGlobal = trigger == null;
            var triggerPos = onlyGlobal ? Vector3.zero : trigger.position;

            var volumes = GrabVolume(layerMask);

            Camera camera = null;

            if (!onlyGlobal)
                trigger.TryGetComponent<Camera>(out camera);

            int numVolumes = volumes.Count;
            if (numVolumes == 0) return;

            m_RenderSettings.RefreshComponents();
            for (int i = 0; i < numVolumes; ++i)
            {
                BXRenderSettingsVolume volume = volumes[i];
                if (volume == null) continue;
#if UNITY_EDITOR
                if (!IsVolumeRenderedByCamera(volume, camera)) continue;
#endif
                if (!volume.enabled || volume.weight <= 0f) continue;

				if (volume.isGlobal)
				{
                    OverrideData(volume, volume.weight);
                    continue;
				}

                if (onlyGlobal) continue;

                var colliders = m_TempColliders;
                volume.GetComponents(colliders);
                if (colliders.Count == 0) continue;

                float closeDistanceSqr = float.PositiveInfinity;

                int numColliders = colliders.Count;
                for(int c = 0; c < numColliders; ++c)
				{
                    var collider = colliders[c];
                    if (!collider.enabled) continue;

                    var closestPoint = collider.ClosestPoint(triggerPos);
                    var d = (closestPoint - triggerPos).sqrMagnitude;

                    if (d < closeDistanceSqr)
                        closeDistanceSqr = d;
				}
                colliders.Clear();

                float blendDistSqr = volume.blendDistance * volume.blendDistance;

                if (closeDistanceSqr > blendDistSqr) continue;

                float interpFactor = 1f;
                if (blendDistSqr > 0f)
                    interpFactor = 1f - (closeDistanceSqr / blendDistSqr);
                OverrideData(volume, interpFactor * volume.weight);
            }

            m_RenderSettings.CollectRenderComponents();
		}

        public bool Render(RenderFeatureStep step, CommandBuffer cmd, BXMainCameraRenderBase render)
		{
            return m_RenderSettings.Render(step, cmd, render);
		}

        public BXRenderSettingsVolume[] GetVolume(LayerMask layerMask)
		{
            var volumes = GrabVolume(layerMask);
            volumes.RemoveAll(v => v == null);
            return volumes.ToArray();
		}

        private List<BXRenderSettingsVolume> GrabVolume(LayerMask mask)
		{
            return m_VolumeCollection.GrabVolumes(mask);
		}

        static bool IsVolumeRenderedByCamera(BXRenderSettingsVolume volume, Camera camera)
		{
#if UNITY_2018_3_OR_NEWER && UNITY_EDITOR
            if (!volume.gameObject.scene.IsValid()) return true;
            return camera == null ? true : UnityEditor.SceneManagement.StageUtility.IsGameObjectRenderedByCamera(volume.gameObject, camera);
#else
            return true;
#endif
        }

    }
}
