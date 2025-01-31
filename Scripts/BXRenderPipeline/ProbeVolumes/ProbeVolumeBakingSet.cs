using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using Unity.Mathematics;
using Unity.IO.LowLevel.Unsafe;
using Unity.Collections;

using CellDesc = BXRenderPipeline.ProbeReferenceVolume.CellDesc;
using CellData = BXRenderPipeline.ProbeReferenceVolume.CellData;

namespace BXRenderPipeline
{
	internal class LogarithmicAttribute : PropertyAttribute
	{
		public int min;
		public int max;

		public LogarithmicAttribute(int min, int max)
		{
			this.min = min;
			this.max = max;
		}
	}

	/// <summary>
	/// An Asset which holds a set of settings to use with a <see cref="ProbeReferenceVolume"/>
	/// </summary>
	public sealed partial class ProbeVolumeBakingSet : ScriptableObject, ISerializationCallbackReceiver
	{
		internal enum Version
		{
			Initial,
			RemoveProbeVolumeSceneData,
			AssetsAlwaysReferenced
		}
		
		[Serializable]
		internal class PerScenarioDataInfo
		{
			private bool m_HasValidData;

			public int sceneHas;
			public ProbeVolumeStreamableAsset cellDataAsset; // Contains L0 L1 SH data
			public ProbeVolumeStreamableAsset cellOptionalDataAsset; // Contains L2 SH data
			public ProbeVolumeStreamableAsset cellProbeOcclusionDataAsset; // Contains per-probe occlusion for up to to 4 lights in range [0;1]

			public void Initialize(ProbeVolumeSHBands shBands)
			{
				m_HasValidData = ComputeHasValidData(shBands);
			}

			public bool IsValid()
			{
				return cellDataAsset != null && cellDataAsset.IsValid(); // If cellDataAsset is valid optional data (if available) should always be valid
			}

			public bool HasValidData(ProbeVolumeSHBands shBands)
			{
#if UNITY_EDITOR
				return ComputeHasValidData(shBands);
#else
				return m_HasValidData;
#endif
			}

			public bool ComputeHasValidData(ProbeVolumeSHBands shBands)
			{
				return cellDataAsset.FileExists() && (shBands == ProbeVolumeSHBands.SphericalHarmonicsL1 || cellOptionalDataAsset.FileExists());
			}
		}

		[Serializable]
		internal struct CellCounts
		{
			public int bricksCount;
			public int chunksCount;

			public void Add(CellCounts o)
			{
				bricksCount += o.bricksCount;
				chunksCount += o.chunksCount;
			}
		}

		// Baking Set Data
		[SerializeField]
		internal bool singleSceneMode = true;
		[SerializeField]
		internal bool dialogNoProbeVolumeInSetShown = false;
		[SerializeField]
		internal ProbeVolumeBakingProcessSettings settings;

		internal bool hasDilation => settings.dilationSettings.enableDilation && settings.dilationSettings.dilationDistance > 0f;

		// We keep a separate list with only the guids for the sake of convenience when iterating from outside this class
		[SerializeField]
		private List<string> m_SceneGUIDs = new List<string>();
		[SerializeField, Obsolete("This is now contained in the SceneBakeData structure"), FormerlySerializedAs("scenesToNotBake")]
		internal List<string> obsoleteScenesToNotBake = new List<string>();
		[SerializeField, FormerlySerializedAs("lightingScenarios")]
		internal List<string> m_LightingScenarios = new List<string>();

		/// <summary>
		/// The list of scene GUIDs
		/// </summary>
		public IReadOnlyList<string> sceneGUIDs => m_SceneGUIDs;
		/// <summary>
		/// The list of lighting scenarios
		/// </summary>
		public IReadOnlyList<string> lightingScenarios => m_LightingScenarios;

		// List of cell descriptors
		[SerializeField]
		internal SerializedDictionary<int, CellDesc> cellDescs = new SerializedDictionary<int, CellDesc>();

		internal Dictionary<int, CellData> cellDataMap = new Dictionary<int, CellData>();
		private List<int> m_TotalIndexList = new List<int>();

		[Serializable]
		private struct SerializedPerSceneCellList
		{
			public string sceneGUID;
			public List<int> cellList;
		}
		[SerializeField]
		private List<SerializedPerSceneCellList> m_SerializedPerSceneCellList;

		// Can't use SerializedDictionary here because we can't serialize a List of List T_T
		internal Dictionary<string, List<int>> perSceneCellLists = new Dictionary<string, List<int>>();

		// Assets containing actual cell data (SH, Validity, ect)
		// This data will be streamed from disk to the GPU
		[SerializeField]
		internal ProbeVolumeStreamableAsset cellSharedDataAsset = null; // Contains validity data
		[SerializeField]
		internal SerializedDictionary<string, PerScenarioDataInfo> scenarios = new SerializedDictionary<string, PerScenarioDataInfo>();
		// This data will be streamed from disk but is only needed in CPU memory
		[SerializeField]
		internal ProbeVolumeStreamableAsset cellBricksDataAsset; // Contains bricks data
		[SerializeField]
		internal ProbeVolumeStreamableAsset cellSupportDataAsset = null; // Contains debug data

		[SerializeField]
		internal int chunkSizeInBricks;
		[SerializeField]
		internal Vector3Int maxCellPosition;
		[SerializeField]
		internal Vector3Int minCellPosition;
		[SerializeField]
		internal Bounds globalBounds;
		[SerializeField]
		internal int bakedSimplificationLevels = -1;
		[SerializeField]
		internal float bakedMinDistanceBetweenProbes = -1f;
		[SerializeField]
		internal bool bakedProbeOcclusion = false;
		[SerializeField]
		internal int bakedSkyOcclusionValue = -1;
		[SerializeField]
		internal int bakedSkyShadingDirectionValue = -1;
		[SerializeField]
		internal Vector3 bakedProbeOffset = Vector3.zero;
		[SerializeField]
		internal int bakedMaskCount = 1;
		[SerializeField]
		internal uint4 bakedLayerMasks;
		[SerializeField]
		internal int maxSHChunkCount = -1; // Maximun number of SH chunk for a cell in this set
		[SerializeField]
		internal int L0ChunkSize;
		[SerializeField]
		internal int L1ChunkSize;
		[SerializeField]
		internal int L2TextureChunkSize; // Optional. Size of the chunk for one texture (4 textures for all data)
		[SerializeField]
		internal int ProbeOcclusionChunkSize; // Optional. Size of the chunk for one texture
		[SerializeField]
		internal int sharedValidityMaskChunkSize; // Shared
		[SerializeField]
		internal int sharedSkyOcclusionL0L1ChunkSize;
		[SerializeField]
		internal int sharedSkyShadingDirectionIndicesChunkSize;
		[SerializeField]
		internal int sharedDataChunkSize;
		[SerializeField]
		internal int supportPositionChunkSize;
		[SerializeField]
		internal int supportValidityChunkSize;
		[SerializeField]
		internal int supportTouchupChunkSize;
		[SerializeField]
		internal int supportLayerMaskChunkSize;
		[SerializeField]
		internal int supportOffsetsChunkSize;
		[SerializeField]
		internal int supportDataChunkSize;

		internal bool bakedSkyOcclusion
		{
			get => bakedSkyOcclusionValue <= 0 ? false : true;
			set => bakedSkyOcclusionValue = value ? 1 : 0;
		}

		internal bool bakedSkyShadingDirection
		{
			get => bakedSkyShadingDirectionValue <= 0 ? false : true;
			set => bakedSkyShadingDirectionValue = value ? 1 : 0;
		}

		[SerializeField]
		internal string lightingScenario = ProbeReferenceVolume.defaultLightingScenario;
		private string m_OtherScenario = null;
		private float m_ScenarioBlendingFactor = 0f;

		internal string otherScenario => m_OtherScenario;
		internal float scenarioBlendingFactor => m_ScenarioBlendingFactor;

		private ReadCommandArray m_ReadCommandArray = new ReadCommandArray();
		private NativeArray<ReadCommand> m_ReadCommandBuffer = new NativeArray<ReadCommand>();
		private Stack<NativeArray<byte>> m_ReadOperationScratchBuffers = new Stack<NativeArray<byte>>();
		private List<int> m_PrunedIndexList = new List<int>();
		private List<int> m_PrunedScenarioIndexList = new List<int>();

		internal const int k_MaxSkyOcclusionBakingSamples = 8192;

		// Baking Profile
		[SerializeField]
		private Version version = CoreUtils.GetLastEnumValue<Version>();

		[SerializeField]
		internal bool freezePlacement = false;

		/// <summary>
		/// Offset on world origin used during baking. Can be used to have cells on positions that are not multiples of the probe spacing
		/// </summary>
		[SerializeField]
		public Vector3 probeOffset = Vector3.zero;

		/// <summary>
		/// How many levels contains the probes hierachical structure
		/// </summary>
		[Range(2, 5)]
		public int simplificationLevels = 3;

		/// <summary>
		/// The size of a Cell in number of bricks
		/// </summary>
		public int cellSizeInBricks => GetCellSizeInBricks(bakedSimplificationLevels);

		/// <summary>
		/// The minimum distance between two probes in meters
		/// </summary>
		[Min(0.1f)]
		public float minDistanceBetweenProbes = 1f;

		/// <summary>
		/// Maximum subdivision in the structure
		/// </summary>
		public int maxSubdivision => GetMaxSubdivision(bakedSimplificationLevels);

		/// <summary>
		/// Minimum size of a brick in meters
		/// </summary>
		public float minBrickSize => GetMinBrickSize(bakedMinDistanceBetweenProbes);

		/// <summary>
		/// Size of the cell in meters
		/// </summary>
		public float cellSizeInMeters => (float)cellSizeInBricks * minBrickSize;

		/// <summary>
		/// Layer mask filter for all renderers
		/// </summary>
		public LayerMask renderersLayerMask = -1;

		/// <summary>
		/// Specifies the minium bounding box volume of renderers to consider placing probes around
		/// </summary>
		[Min(0f)]
		public float minRendererVolumeSize = 0.1f;

		/// <summary>
		/// Specifies whether the baking set will have sky handled dynamically
		/// </summary>
		public bool skyOcclusion = false;

		/// <summary>
		/// Controls the number of bounces per light path for dynamic sky baking.
		/// </summary>
		[Range(0, 5)]
		public int skyOcclusionBakingBounces = 2;

		/// <summary>
		/// Average albedo for dynamic sky bounces
		/// </summary>
		[Range(0f, 1f)]
		public float skyOcclusionAverageAlbedo = 0.6f;

		/// <summary>
		/// Sky Occlusion backface culling
		/// </summary>
		public bool skyOcclusionBackFaceCulling = false;

		/// <summary>
		/// Bake sky shading direction
		/// </summary>
		public bool skyOcclusionShadingDirection = false;

		[Serializable]
		internal struct ProbeLayerMask
		{
			public RenderingLayerMask mask;
			public string name;
		}

		[SerializeField]
		internal bool useRenderingLayers = false;
		[SerializeField]
		internal ProbeLayerMask[] renderingLayerMasks;

		internal uint4 ComputeRegionMasks()
		{
			uint4 masks = 0;
			if (!useRenderingLayers || renderingLayerMasks == null)
				masks.x = 0xFFFFFFFF;
			else
			{
				for (int i = 0; i < renderingLayerMasks.Length; ++i)
					masks[i] = renderingLayerMasks[i].mask;
			}
			return masks;
		}

		internal static int GetCellSizeInBricks(int simplificationLevels) => (int)Mathf.Pow(ProbeBrickPool.kBrickCellCount, simplificationLevels);
		internal static int GetMaxSubdivision(int simplificationLevels) => simplificationLevels + 1; // we add one for the top subdiv level which is the same size as a cell
		internal static float GetMinBrickSize(float minDistanceBetweenProbes) => Mathf.Max(0.01f, minDistanceBetweenProbes);

		private bool m_HasSupportData = false;
		private bool m_SharedDataIsValid = false;
		private bool m_UseStreamingAsset = true;

        public void OnValidate()
        {
			singleSceneMode &= m_SceneGUIDs.Count <= 1;

			if (m_LightingScenarios.Count == 0)
				// TODO:NEED IMPLEMENT
				//m_LightingScenarios = new List<string>() { ProbeReferenceVolume.defaultLightingScenario };

			settings.Upgrade();
        }

        private void OnEnable()
        {
			Migrate();

			m_HasSupportData = ComputeHasSupportData();
			m_SharedDataIsValid = ComputeHasValidSharedData();
        }

		internal void Migrate()
        {
			if(version != CoreUtils.GetLastEnumValue<Version>())
            {
#pragma warning disable 618 // Type or member is obsolete
				if(version < Version.RemoveProbeVolumeSceneData)
                {
#if UNITY_EDITOR
					// TODO: NEED IMPLEMENT
					//var sceneData = ProbeReferenceVolume.instance.sceeneData;
					//if (sceneData == null)
					//	return;

					//foreach(var scene in m_SceneGUIDs)
					//               {
					//	SceneBakeData newSceneData = new SceneBakeData();

				//}
#endif
				}
			}
        }

		// For functions below:
		// In editor users can delete asset at any moment, so we need to compute the result from scratch all the time
		// In builds however, we want to avoid the expensive I/O operations
		private bool ComputeHasValidSharedData()
        {
			return cellSharedDataAsset != null && cellSharedDataAsset.FileExists() && cellBricksDataAsset.FileExists();
        }

		internal bool HasValidSharedData()
		{
#if UNITY_EDITOR
			return ComputeHasValidSharedData();
#else
			return m_SharedDataIsValid;
#endif
		}

		// Return true if baking settings are incompatible with already baked data
		internal bool CheckCompatibleCellLayout()
		{
			return simplificationLevels == bakedSimplificationLevels &&
				minDistanceBetweenProbes == bakedMinDistanceBetweenProbes &&
				skyOcclusion == bakedSkyOcclusion &&
				skyOcclusionShadingDirection == bakedSkyShadingDirection &&
				settings.virtualOffsetSettings.useVritualOffset == (supportOffsetsChunkSize != 0) &&
				useRenderingLayers == (bakedMaskCount != 1);
		}

		private bool ComputeHasSupportData()
        {
			return cellSupportDataAsset != null && cellSupportDataAsset.IsValid() && cellSupportDataAsset.FileExists();
        }

		internal bool HasSupportData()
		{
#if UNITY_EDITOR
			return ComputeHasSupportData();
#else
			return m_HasSupportData;
#endif
		}

		/// <summary>
		/// Tests if the baking set data has already been baked.
		/// </summary>
		/// <param name="scenario">The name of the scenario to test. If null or if scenarios are disabled, the function will test for the default scenario.</param>
		/// <returns>True if the baking set data has been baked.</returns>
		public bool HasBakedData(string scenario = null)
		{
			if (scenario == null)
				return scenarios.ContainsKey(ProbeReferenceVolume.defaultLightingScenario);

			if (!ProbeReferenceVolume.instance.supportLightingScenarios && scenario != ProbeReferenceVolume.defaultLightingScenario)
				return false;

			return scenario.Contains(scenario);
		}

		internal void SetActiveScenario(string scenario, bool verbose = true)
		{
			if (lightingScenario == scenario)
				return;

			if (!m_LightingScenarios.Contains(scenario))
			{
				if(verbose)
					Debug.LogError($"Scenario '{scenario}' does not exist.");
				return;
			}

			if (!scenarios.ContainsKey(scenario))
			{
				// We don't return here as it's still valid to enable a scenario that wasn't baked in the editor.
				if (verbose)
					Debug.LogError($"Scenario '{scenario}' has not been baked.");
			}

			lightingScenario = scenario;
			m_ScenarioBlendingFactor = 0f;

			if (ProbeReferenceVolume.instance.supportScenarioBlending)
			{
				// Trigger blending system to replace old cells with the one from the new active scenario.
				// Although we technically don't need blending for that, it is better than unloading all cells
				// because it will replace them progressively. There is no real performance cost to using blending
				// rather than regular load thanks to the bypassBlending branch in AddBlendingBricks.
				ProbeReferenceVolume.instance.ScenarioBlendingChanged(true);
			}
			else
			{
				ProbeReferenceVolume.instance.UnloadAllCells();
			}

#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(this);
#endif
		}

		internal void BlendLightingScenario(string otherScenario, float blendingFactor)
		{
			throw new NotImplementedException();
		}

        public void OnBeforeSerialize()
        {
            throw new NotImplementedException();
        }

        public void OnAfterDeserialize()
        {
            throw new NotImplementedException();
        }

		internal int GetChunkGPUMemory(ProbeVolumeSHBands shBands)
		{
			// One L0 Chunk, Two L1 Chunks, 1 shared chunk which may contain sky occlusion
			int size = L0ChunkSize + 2 * L1ChunkSize + sharedDataChunkSize;

			// 4 Optional L2 Chunks
			if (shBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
				size += 4 * L2TextureChunkSize;

			// Optional probe occlusion
			if (bakedProbeOcclusion)
				size += ProbeOcclusionChunkSize;

			return size;
		}
	}
}
