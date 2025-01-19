using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BXRenderPipeline
{
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
			//float minBrickSize = proberenferenceVolume
		}
#endif
	}
}
