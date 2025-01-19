using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using Unity.Mathematics;
using Unity.IO.LowLevel.Unsafe;
using Unity.Collections;
using BXRenderPipeline.HighDefinition;

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
		}

		internal static int GetCellSizeInBricks(int simplificationLevels) => (int)Mathf.Pow(ProbeBrickPool.kBrickCellCount, simplificationLevels);
		internal static int GetMaxSubdivision(int simplificationLevels) => simplificationLevels + 1; // we add one for the top subdiv level which is the same size as a cell
		internal static float GetMinBrickSize(float minDistanceBetweenProbes) => Mathf.Max(0.01f, minDistanceBetweenProbes);
	}
}
