using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace BXRenderPipeline
{
	[ExecuteAlways]
	[AddComponentMenu("Rendering/BXRenderPipeline/RenderSettingsVolume")]
    public class BXRenderSettingsVolume : MonoBehaviour, IVolume
    {
        [SerializeField, FormerlySerializedAs("isGlobal")]
        private bool m_IsGlobal = true;

		public bool isGlobal 
        {
            get => m_IsGlobal;
            set => m_IsGlobal = value;
        }

		[Delayed]
        public float priortiy = 0f;
        public float blendDistance = 0f;
        [Range(0f, 1f)]
		public float weight = 1f;

		internal List<Collider> m_Colliders = new List<Collider>();

        public List<Collider> colliders => m_Colliders;

        private int m_PreviousLayer;
        private float m_PreviousPriority;

		[HideInInspector]
		public List<BXVolumeComponment> components;

		private void OnEnable()
		{
            m_PreviousLayer = gameObject.layer;
			BXVolumeManager.instance.Register(this);
		}

		private void OnDisable()
		{
            BXVolumeManager.instance.Unregister(this);
		}

		private void Update()
		{
			UpdateLayer();
			UpdatePriority();
#if UNITY_EDITOR
			GetComponents(m_Colliders);
#endif
		}

        internal void UpdateLayer()
		{
			int layer = gameObject.layer;
			if (layer == m_PreviousLayer) return;

			BXVolumeManager.instance.UpdateVolumeLayer(this, m_PreviousLayer, layer);
			m_PreviousLayer = layer;
		}

		internal void UpdatePriority()
		{
			if (!(Math.Abs(priortiy - m_PreviousPriority) > Mathf.Epsilon)) return;

			BXVolumeManager.instance.SetLayerDirty(gameObject.layer);
			m_PreviousPriority = priortiy;
		}

		private void OnValidate()
		{
			blendDistance = Mathf.Max(blendDistance, 0f);
		}

		public bool Has(Type componmentType)
		{
			if (components == null || components.Count == 0) return false;
			for(int i = 0; i < components.Count; ++i)
			{
				if (components[i].GetType() == componmentType)
					return true;
			}
			return false;
		}
	}
}
