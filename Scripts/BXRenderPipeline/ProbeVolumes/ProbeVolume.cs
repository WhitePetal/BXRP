using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BXRenderPipeline
{
	/// <summary>
	/// A marker to determine what area of the scene is considered by the Probe Volumes system
	/// </summary>
	[ExecuteAlways]
	[AddComponentMenu("BXRenderPipeline/Adaptive Probe Volume")]
	public partial class ProbeVolume : MonoBehaviour
	{
		/// <summary>
		/// Indicates which renderers should be considerer for the Probe Volume bounds when baking
		/// </summary>
		public enum Mode
		{
			/// <summary>
			/// Encapsulate all renderers in the bgaking set
			/// </summary>
			Global,
			/// <summary>
			/// Encapsulate all renderers in the scene
			/// </summary>
			Scene,
			/// <summary>
			/// Encapsulate all renderers in the bounding box
			/// </summary>
			Local
		}

		/// <summary>
		/// If is a global bolume
		/// </summary>
		[Tooltip("When set to Global this Probe Volume considers all renderers Contribute Global Illumination enabled. Local only considers renderers in the scene.\nThis list updates every time the Scene is saved or the lighting is baked")]
		public Mode mode = Mode.Local;

		/// <summary>
		/// The size
		/// </summary>
		public Vector3 size = new Vector3(10, 10, 10);

		/// <summary>
		/// Override the renderer filters
		/// </summary>
		[HideInInspector, Min(0)]
		public bool overrideRendererFilters = false;

		/// <summary>
		/// The minimum renderer bounding box volume size. The value is used to discard small renderers when the overrideMinRendererVolumeSize is enabled
		/// </summary>
		[HideInInspector, Min(0)]
		public float minRendererVolumeSize = 0.1f;

		/// <summary>
		/// The <see cref="LayerMask"/>
		/// </summary>
		public LayerMask objectLayerMask = -1;

		/// <summary>
		/// The lowest subdivision level override
		/// </summary>
		[HideInInspector]
		public int lowestSubdivLevelOverride = 0;

		/// <summary>
		/// The highest subdivision level override
		/// </summary>
		[HideInInspector]
		public int highestSubdivLevelOverride = ProbeBrickIndex.kMaxSubdivisionLevels;

		/// <summary>
		/// If the subdivision levels need to be overriden
		/// </summary>
		[HideInInspector]
		public bool overridesSubdivLevels = false;

		[SerializeField]
		internal bool mightNeedRebaking = false;

		[SerializeField]
		internal Matrix4x4 cachedTransform;
		[SerializeField]
		internal int cachedHashCode;

		/// <summary>
		/// Whether spaces with no renderers need to be filled with bricks at highest subdivision level
		/// </summary>
		[Tooltip("Whether Unity should fill empty space between renderers with bricks at the highest subdivision level")]
		public bool fillEmptrySpaces = false;

#if UNITY_EDITOR
		/// <summary>
		/// Returns the extents of the volume
		/// </summary>
		/// <returns></returns>
		public Vector3 GetExtents()
		{
			return size;
		}

		public Matrix4x4 GetVolume()
		{
			return Matrix4x4.TRS(transform.position, transform.rotation, GetExtents());
		}

		internal Bounds ComputeBounds(GIContributor.ContributorFilter filter, Scene? scene = null)
		{
			Bounds bounds = new Bounds();
			bool foundABound = false;

			void ExpandBounds(Bounds bound)
			{
				if (!foundABound)
				{
					bounds = bound;
					foundABound = true;
				}
				else
				{
					bounds.Encapsulate(bound);
				}
			}

			var contributors = GIContributor.Find(filter, scene);
			foreach (var renderer in contributors.renderers)
				ExpandBounds(renderer.bounds);
			foreach (var terrain in contributors.terrains)
				ExpandBounds(terrain.boundsWithTrees);

			return bounds;
		}

		internal void UpdateGlobalVolume(GIContributor.ContributorFilter filter)
		{
			float minBrickSize = ProbeReferenceVolume.instance.MinBrickSize();
			var bounds = ComputeBounds(filter, gameObject.scene);
			transform.position = bounds.center;
			size = Vector3.Max(bounds.size + new Vector3(minBrickSize, minBrickSize, minBrickSize), Vector3.zero);
        }

		internal void OnLightingDataAssetCleared()
        {
			mightNeedRebaking = true;
        }

		internal void OnBakeCompleted()
        {
			// We cache the data of last baked completed.
			cachedTransform = gameObject.transform.worldToLocalMatrix;
			cachedHashCode = GetHashCode();
			mightNeedRebaking = false;
        }

        public override int GetHashCode()
        {
			int hash = 17;

            unchecked
            {
				hash = hash * 23 + size.GetHashCode();
				hash = hash * 23 + gameObject.transform.worldToLocalMatrix.GetHashCode();
				hash = hash * 23 + overridesSubdivLevels.GetHashCode();
				hash = hash * 23 + highestSubdivLevelOverride.GetHashCode();
				hash = hash * 23 + lowestSubdivLevelOverride.GetHashCode();
				hash = hash * 23 + overrideRendererFilters.GetHashCode();
                if (overrideRendererFilters)
                {
					hash = hash * 23 + minRendererVolumeSize.GetHashCode();
					hash = hash * 23 + objectLayerMask.value.GetHashCode();
                }
				hash = hash * 23 + fillEmptrySpaces.GetHashCode();
            }

			return hash;
        }

		internal void GetSubdivisionOverride(int maxSubdivisionLevel, out int minLevel, out int maxLevel)
        {
            if (overridesSubdivLevels)
            {
				maxLevel = Mathf.Min(highestSubdivLevelOverride, maxSubdivisionLevel);
				minLevel = Mathf.Min(lowestSubdivLevelOverride, maxLevel);
            }
            else
            {
				maxLevel = maxSubdivisionLevel;
				minLevel = 0;
            }
        }

		// Momentarily moving the gizmo rendering for bricks and cells to Probe Volume itself,
		// only the first probe volume in the scene will render them. The reason is that we dont have any
		// other non-hidden component related to APV.
		#region APVGizm0

		internal static List<ProbeVolume> instances = new();


        #endregion
#endif
    }
}
