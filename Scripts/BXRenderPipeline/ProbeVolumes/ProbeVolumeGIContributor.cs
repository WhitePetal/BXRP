using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Profiling;

#if UNITY_EDITOR
using UnityEditor;
//using ProbeVolumeWithBoundsList = System.Collections.Generic.List<(BXRenderPipeline.ProbeVolume componenty, )>
#endif

namespace BXRenderPipeline
{
	internal struct GIContributor
	{
#if UNITY_EDITOR
		public struct TerrainContributor
		{
			public struct TreePrototype
			{
				public MeshRenderer component;
				public Matrix4x4 transform;
				public Bounds prefabBounds;

				public List<(Matrix4x4 transform, Bounds boundsWS)> instances;
			}

			public Terrain component;
			public Bounds boundsWithTrees;
			public Bounds boundsTerrainOnly;
			public TreePrototype[] treePrototypes;
		}

		public List<(Renderer component, Bounds bounds)> renderers;
		public List<TerrainContributor> terrains;

		public int Count => renderers.Count + terrains.Count;

		internal enum ContributorFilter
		{
			All,
			Scene,
			Selection
		};

		internal static bool ContributesGI(GameObject go) =>
			(GameObjectUtility.GetStaticEditorFlags(go) & StaticEditorFlags.ContributeGI) != 0;

		internal static Vector3[] m_Vertices = new Vector3[8];

		private static Bounds TransformBounds(Bounds bounds, Matrix4x4 transform)
		{
			Vector3 boundsMin = bounds.min, boundsMax = bounds.max;
			m_Vertices[0] = new Vector3(boundsMin.x, boundsMin.y, boundsMin.z);
			m_Vertices[1] = new Vector3(boundsMax.x, boundsMin.y, boundsMin.z);
			m_Vertices[2] = new Vector3(boundsMax.x, boundsMax.y, boundsMin.z);
			m_Vertices[3] = new Vector3(boundsMin.x, boundsMax.y, boundsMin.z);
			m_Vertices[4] = new Vector3(boundsMin.x, boundsMin.y, boundsMax.z);
			m_Vertices[5] = new Vector3(boundsMax.x, boundsMin.y, boundsMin.z);
			m_Vertices[6] = new Vector3(boundsMax.x, boundsMax.y, boundsMax.z);
			m_Vertices[7] = new Vector3(boundsMin.x, boundsMax.y, boundsMax.z);

			Vector3 min = transform.MultiplyPoint(m_Vertices[0]);
			Vector3 max = min;

			for(int i = 1; i < 8; ++i)
			{
				var point = transform.MultiplyPoint(m_Vertices[i]);
				min = Vector3.Min(min, point);
				max = Vector3.Max(max, point);
			}

			Bounds result = default;
			result.SetMinMax(min, max);
			return result;
		}

		internal static Matrix4x4 GetTreeInstanceTransform(Terrain terrain, TreeInstance tree)
		{
			var position = terrain.GetPosition() + Vector3.Scale(tree.position, terrain.terrainData.size);
			var rotation = Quaternion.Euler(0, tree.rotation * Mathf.Rad2Deg, 0);
			var scale = new Vector3(tree.widthScale, tree.heightScale, tree.widthScale);

			return Matrix4x4.TRS(position, rotation, scale);
		}

		public static GIContributor Find(ContributorFilter filter, Scene? scene = null)
		{
			if (filter == ContributorFilter.Scene && scene == null)
				return default;

			Profiler.BeginSample("GIContributors.Find");

			var contributors = new GIContributor()
			{
				renderers = new(),
				terrains = new(),
			};

			void PushRenderer(Renderer renderer)
			{
				if(!ContributesGI(renderer.gameObject) || !renderer.gameObject.TryGetComponent<MeshFilter>(out var _) || !renderer.gameObject.activeInHierarchy || !renderer.enabled || !renderer.isLOD0())
					return;

				var bounds = renderer.bounds;
				bounds.size += Vector3.one * 0.01f;
				contributors.renderers.Add((renderer, bounds));
			}

			void PushTerrain(Terrain terrain)
			{
				if (!ContributesGI(terrain.gameObject) || !terrain.gameObject.activeInHierarchy || !terrain.enabled || !terrain.terrainData == null)
					return;

				var terrainData = terrain.terrainData;
				var terrainBounds = terrainData.bounds;
				terrainBounds.center += terrain.GetPosition();
				terrainBounds.size += Vector3.one * 0.01f;

				var prototypes = terrainData.treePrototypes;
				var treePrototypes = new TerrainContributor.TreePrototype[prototypes.Length];
				for(int i = 0; i < prototypes.Length; ++i)
				{
					MeshRenderer renderer = null;

					var prefab = prototypes[i].prefab;
					if (prefab == null)
						continue;

					if(prefab.TryGetComponent<LODGroup>(out var lodGroup))
					{
						var groups = lodGroup.GetLODs();
						if (groups.Length != 0 && groups[0].renderers.Length != 0)
							renderer = groups[0].renderers[0] as MeshRenderer;
					}
					if (renderer == null)
						renderer = prefab.GetComponent<MeshRenderer>();

					if(renderer != null && renderer.enabled && ContributesGI(renderer.gameObject))
					{
						var tr = prefab.transform;
						// For some reson, tree instances are not affected by rotation and position of prefab root
						// But they are affected by scale, and by any other transform in the hierarchy
						var transform = Matrix4x4.TRS(tr.position, tr.rotation, Vector3.one).inverse * renderer.localToWorldMatrix;

						// Compute prefab bounds. This will be used to compute highest tree to expand terrain bounds
						// and to approximate the bounds of tree instances for culling during voxelization
						var prefabBounds = TransformBounds(renderer.localBounds, transform);

						treePrototypes[i] = new TerrainContributor.TreePrototype()
						{
							component = renderer,
							transform = transform,
							prefabBounds = prefabBounds,
							instances = new List<(Matrix4x4 transform, Bounds boundsWS)>(),
						};
					}
				}

				Vector3 totalMax = terrainBounds.max;
				foreach(var tree in terrainData.treeInstances)
				{
					var prototype = treePrototypes[tree.prototypeIndex];
					if (prototype.component == null)
						continue;

					// Approximate instance bounds since rotation can only be on y axis
					var transform = GetTreeInstanceTransform(terrain, tree);
					var boundsCenter = transform.MultiplyPoint(prototype.prefabBounds.center);
					var boundsSize = prototype.prefabBounds.size;
					float maxTreeWidth = Mathf.Max(boundsSize.x, boundsSize.z) * tree.widthScale * Mathf.Sqrt(2f);
					boundsSize = new Vector3(maxTreeWidth, boundsSize.y * tree.heightScale, maxTreeWidth);

					prototype.instances.Add((transform, new Bounds(boundsCenter, boundsSize)));
					totalMax.y = Mathf.Max(boundsCenter.y + boundsSize.y * 0.5f, totalMax.y);
				}

				var totalBounds = new Bounds();
				totalBounds.SetMinMax(terrainBounds.min, totalMax);

				contributors.terrains.Add(new TerrainContributor()
				{
					component = terrain,
					boundsWithTrees = totalBounds,
					boundsTerrainOnly = terrainBounds,
					treePrototypes = treePrototypes
				});
			}

			if(filter == ContributorFilter.Selection)
			{
				var transforms = Selection.transforms;
				foreach(var transform in transforms)
				{
					var childrens = transform.gameObject.GetComponentsInChildren<Transform>();
					foreach(var children in childrens)
					{
						if (children.gameObject.TryGetComponent(out Renderer renderer))
							PushRenderer(renderer);
						else if (children.gameObject.TryGetComponent(out Terrain terrain))
							PushTerrain(terrain);
					}
				}
			}
			else
			{
				var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.InstanceID);
				Profiler.BeginSample($"Find Renderers ({renderers.Length})");
				foreach(var renderer in renderers)
				{
					if (filter != ContributorFilter.Scene || renderer.gameObject.scene == scene)
						PushRenderer(renderer);
				}
				Profiler.EndSample();

				var terrains = Object.FindObjectsByType<Terrain>(FindObjectsSortMode.InstanceID);
				Profiler.BeginSample($"Find Terrains ({terrains.Length})");
				foreach(var terrain in terrains)
				{
					if (filter != ContributorFilter.Scene || terrain.gameObject.scene == scene)
						PushTerrain(terrain);
				}
				Profiler.EndSample();
			}

			Profiler.EndSample();
			return contributors;
		}
#endif
	}
}
