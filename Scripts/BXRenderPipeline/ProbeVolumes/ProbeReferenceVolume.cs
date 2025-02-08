using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using UnityEngine.SceneManagement;
using System;
using Unity.Mathematics;
using Unity.Collections;

#if UNITY_EDITOR
using System.Linq.Expressions;
using UnityEditor;
#endif

using Brick = BXRenderPipeline.ProbeBrickIndex.Brick;
using Chunk = BXRenderPipeline.ProbeBrickPool.BrickChunkAlloc;
using System.Diagnostics;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;
using UnityEngine.Experimental.Rendering;

namespace BXRenderPipeline
{
	internal static class SceneExtensions
	{
		private static PropertyInfo s_SceneGUID = typeof(Scene).GetProperty("guid", BindingFlags.NonPublic | BindingFlags.Instance);
		public static string GetGUID(this Scene scene)
		{
			UnityEngine.Debug.Assert(s_SceneGUID != null, "Reflection for scene GUID failed");
			return (string)s_SceneGUID.GetValue(scene);
		}
	}

	/// <summary>
	/// Initialization parameters for the probe volume system
	/// </summary>
	public struct ProbeVolumeSystemParameters
	{
		/// <summary>
		/// The memory budget determining the size of the textures containing SH data
		/// </summary>
		public ProbeVolumeTextureMemoryBudget memoryBudget;

		/// <summary>
		/// The memory budget determinming the size of the textures used for blending between scenarios
		/// </summary>
		public ProbeVolumeBlendingTextureMemoryBudget blendingMemoryBudget;

		/// <summary>
		/// The <see cref="ProbeVolumeSHBands"/>
		/// </summary>
		public ProbeVolumeSHBands shBands;

		/// <summary>
		/// True if APV should support lighting scenarios
		/// </summary>
		public bool supportScenarios;

		/// <summary>
		/// True if APV should support lighting scenario blending
		/// </summary>
		public bool supportScenarioBlending;

		/// <summary>
		/// True if APV should support streaming of cell data to the GPU
		/// </summary>
		public bool supportGPUStreaming;

		/// <summary>
		/// True if APV should support streaming of cell data from disk
		/// </summary>
		public bool supportDiskStreaming;

		/// <summary>
		/// The shader used to visualize the probes in the debug views.
		/// </summary>
		[Obsolete("This field is not used anymore.")]
		public Shader probeDebugShader;

		/// <summary>
		/// The shader used to visualize the way probes are sampled for a single pixel in the debug view.
		/// </summary>
		[Obsolete("This field is not used anymore.")]
		public Shader probeSamplingDebugShader;

		/// <summary>
		/// The debug texture used to display probe weight in the debug view.
		/// </summary>
		[Obsolete("This field is not used anymore.")]
		public Texture probeSamplingDebugTexture;
		
		/// <summary>
		/// The debug mesh used to visualize the way probes are sampled for a single pixel in the debug view
		/// </summary>
		[Obsolete("This field is not used anymore.")]
		public Mesh probeSamplingDebugMesh;

		/// <summary>
		/// The shader used to visualize probes virtual offset in the debug view.
		/// </summary>
		[Obsolete("This field is not used anymore.")]
		public Shader offsetDebugShader;

		/// <summary>
		/// The shader used to visualize APV fragmentation.
		/// </summary>
		[Obsolete("This field is not used anymore.")]
		public Shader fragmentationDebugShader;

		/// <summary>
		/// The compute shader used to interpolate between two lighting scenarios.
		/// Set to null if blending is not supported
		/// </summary>
		[Obsolete("This field is not used anymore.")]
		public ComputeShader scenarioBlendingShader;

		/// <summary>
		/// The compute shader used to upload streamed data to the GPU.
		/// </summary>
		[Obsolete("This field is not used anymore.")]
		public ComputeShader streamingUploadShader;

		/// <summary>
		/// The <see cref="ProbeVolumeSceneData"/>
		/// </summary>
		[Obsolete("This field is not used anymore.")]
		public ProbeVolumeSceneData sceneData;

		/// <summary>
		/// True if APV is able to show runtime debug information.
		/// </summary>
		[Obsolete("This field is not used anymore. Used with the current Shader Stripping Settings. #from(2023.3)")]
		public bool supportsRuntimeDebug;
	}

	internal struct ProbeVolumeShadingParameters
	{
		public float normalBias;
		public float viewBias;
		public bool scaleBiasByMinDistanceBetweenProbe;
		public float samplingNoise;
		public float weight;
		public APVLeakReductionMode leakReductionMode;
		public int frameIndexForNoise;
		public float reflNormalizationLowerClamp;
		public float reflNormalizationUpperClamp;
		public float skyOcclusionIntensity;
		public bool skyOcclusionShadingDirection;
		public int regionCount;
		public uint4 regionLayerMasks;
		public Vector3 worldOffset;
	}

	/// <summary>
	/// Possible values for the probe volume memory budget (determines the size of the textures used)
	/// </summary>
	[Serializable]
	public enum ProbeVolumeTextureMemoryBudget
	{
		/// <summary>
		/// Low Budget
		/// </summary>
		MemoryBudgetLow = 512,
		/// <summary>
		/// Medium Budget
		/// </summary>
		MemoryBudgetMedium = 1024,
		/// <summary>
		/// High Budget
		/// </summary>
		MemoryBudgetHigh = 2048
	}

	/// <summary>
	/// Possible values for the probe volume scenario blending memory budget (determines the sizof the textures used)
	/// </summary>
	[Serializable]
	public enum ProbeVolumeBlendingTextureMemoryBudget
	{
		/// <summary>
		/// Low Budget
		/// </summary>
		MemoryBudgetLow = 128,
		/// <summary>
		/// Mendium Budget
		/// </summary>
		MemoryBudgetMedium = 256,
		/// <summary>
		/// High Budget
		/// </summary>
		MemoryBudgetHigh = 512
	}

	/// <summary>
	/// Number of Spherical Harmonics bands that are used with Probe Volumes
	/// </summary>
	[Serializable]
	public enum ProbeVolumeSHBands
	{
		/// <summary>
		/// Up to the L1 band of Spherical Harmonics
		/// </summary>
		SphericalHarmonicsL1 = 1,
		/// <summary>
		/// Up to the L2 band of Spherical Harmonics
		/// </summary>
		SphericalHarmonicsL2 = 2
	}

	/// <summary>
	/// The reference volume for the Adaptive Probe Volums system. This defines the structure in which volume assets are loaded into. There must be only one, hence why it follow a singletion pattern.
	/// </summary>
	public partial class ProbeReferenceVolume
	{
		[Serializable]
		internal struct IndirectionEntryInfo
		{
			public Vector3Int positionInBricks;
			public int minSubdiv;
			public Vector3Int minBrickPos;
			public Vector3Int maxBrickPosPlusOne;
			public bool hasMinMax; // should be removed, only kept for migration
			public bool hasOnlyBiggerBricks; // True if it has only bricks that are bigger than the entry itself
		}

		[Serializable]
		internal class CellDesc
		{
			public Vector3Int position;
			public int index;
			public int probeCount;
			public int minSubdiv;
			public int indexChunkCount;
			public int shChunkCount;
			public int bricksCount;

			// This is data that is generated at bake time to not having to re-analyzing the content of the cell for the indirection buffer.
			// This is not technically part of the descriptor of the cell but it needs to be here becaus it's computed at bake time and needs
			// to be serialized with the rest of the cell
			public IndirectionEntryInfo[] indirectionEntryInfo;

			public override string ToString()
			{
				return $"Index = {index} position = {position}";
			}
		}

		internal class CellData
		{
			// Shared Data
			public NativeArray<byte> validityNeighMaskData;
			public NativeArray<ushort> skyOcclusionDataL0L1 { get; internal set; }
			public NativeArray<byte> skyShadingDirectionIndices { get; internal set; }

			// Scenario Data
			public struct PerScenarioData
			{
				// L0/L1 Data
				public NativeArray<ushort> shL0L1RxData;
				public NativeArray<byte> shL1GL1RyData;
				public NativeArray<byte> shL1BL1RzData;

				// Optional L2 Data
				public NativeArray<byte> shL2Data_0;
				public NativeArray<byte> shL2Data_1;
				public NativeArray<byte> shL2Data_2;
				public NativeArray<byte> shL2Data_3;

				// 4 unorm per probe, 1 for each occluded light
				public NativeArray<byte> probeOcclusion;
			}

			public Dictionary<string, PerScenarioData> scenarios = new Dictionary<string, PerScenarioData>();

			// Brick data
			public NativeArray<Brick> bricks { get; internal set; }

			// Support Data
			public NativeArray<Vector3> probePositions { get; internal set; }
			public NativeArray<float> touchupVolumeInteraction { get; internal set; } // only used by as specific debug view
			public NativeArray<Vector3> offsetVectors { get; internal set; }
			public NativeArray<float> validity { get; internal set; }
			public NativeArray<byte> layer { get; internal set; } // Only used by a specific debug view
			
			public void CleanupPerScenarioData(in PerScenarioData data)
			{
				if (data.shL0L1RxData.IsCreated)
				{
					data.shL0L1RxData.Dispose();
					data.shL1GL1RyData.Dispose();
					data.shL1BL1RzData.Dispose();
				}

				if (data.shL2Data_0.IsCreated)
				{
					data.shL2Data_0.Dispose();
					data.shL2Data_1.Dispose();
					data.shL2Data_2.Dispose();
					data.shL2Data_3.Dispose();
				}

				if (data.probeOcclusion.IsCreated)
				{
					data.probeOcclusion.Dispose();
				}
			}

			public void Cleanup(bool cleanScenarioList)
			{
				// GPU Data. Will not exist if disk streaming is enabled.
				if (validityNeighMaskData.IsCreated)
				{
					validityNeighMaskData.Dispose();
					validityNeighMaskData = default;

					foreach (var scenario in scenarios.Values)
						CleanupPerScenarioData(scenario);
				}

				// When using disk streaming, we don't want to clear this list as it's the only place where we know whick scenarios are available for the cell
				// This is ok because the scenario data isn't instantiated here
				if (cleanScenarioList)
					scenarios.Clear();

				// Bricks and support data. May not exist with disk streaming.
				if (bricks.IsCreated)
				{
					bricks.Dispose();
					bricks = default;
				}

				if (skyOcclusionDataL0L1.IsCreated)
				{
					skyOcclusionDataL0L1.Dispose();
					skyOcclusionDataL0L1 = default;
				}

				if (skyShadingDirectionIndices.IsCreated)
				{
					skyShadingDirectionIndices.Dispose();
					skyShadingDirectionIndices = default;
				}

				if (probePositions.IsCreated)
				{
					probePositions.Dispose();
					probePositions = default;
				}

				if (touchupVolumeInteraction.IsCreated)
				{
					touchupVolumeInteraction.Dispose();
					touchupVolumeInteraction = default;
				}

				if (validity.IsCreated)
				{
					validity.Dispose();
					validity = default;
				}

				if (layer.IsCreated)
				{
					layer.Dispose();
					layer = default;
				}

				if (offsetVectors.IsCreated)
				{
					offsetVectors.Dispose();
					offsetVectors = default;
				}
			}
		}

		internal class CellPoolInfo
		{
			public List<Chunk> chunkList = new List<Chunk>();
			public int shChunkCount;

			public void Clear()
			{
				chunkList.Clear();
			}
		}

		internal class CellIndexInfo
		{
			public int[] flatIndicesInGlobalIndirection = null;
			public ProbeBrickIndex.CellIndexUpdateInfo updateInfo;
			public bool indexUpdated;
			public IndirectionEntryInfo[] indirectionEntryInfo;
			public int indexChunkCount;

			public void Clear()
			{
				flatIndicesInGlobalIndirection = null;
				updateInfo = default(ProbeBrickIndex.CellIndexUpdateInfo);
				indexUpdated = false;
				indirectionEntryInfo = null;
			}
		}

		internal class CellBlendingInfo
		{
			public List<Chunk> chunkList = new List<Chunk>();
			public float blendingScore;
			public float blendingFactor;
			public bool blending;

			public void MarkUpToDate() => blendingScore = float.MaxValue;
			public bool IsUpToDate() => blendingScore == float.MaxValue;
			public void ForceReupload() => blendingFactor = -1f;
			public bool ShouldReupload() => blendingFactor == -1f;
			public void Prioritize() => blendingFactor = -2f;
			public bool ShouldPrioritize() => blendingFactor == -2f;

			public void Clear()
			{
				chunkList.Clear();
				blendingScore = 0;
				blendingFactor = 0;
				blending = false;
			}
		}

		internal class CellStreamingInfo
		{
			public CellStreamingRequest request = null;
			public CellStreamingRequest blendingRequest0 = null;
			public CellStreamingRequest blendingRequest1 = null;
			public float streamingScore;

			public bool IsStreaming()
			{
				return request != null && request.IsStreaming();
			}

			public bool IsBlendingStreaming()
            {
				return blendingRequest0 != null && blendingRequest0.IsStreaming()
					|| blendingRequest1 != null && blendingRequest1.IsStreaming();
            }

			public void Clear()
            {
				request = null;
				blendingRequest0 = null;
				blendingRequest1 = null;
				streamingScore = 0;
            }
		}

		[DebuggerDisplay("Index = {desc.index} Loaded = {loaded}")]
		internal class Cell : IComparable<Cell>
		{
			// Baked data (cell descriptor and baked probe data read from disk).
			public CellDesc desc;
			public CellData data;
			// Runtime info.
			public CellPoolInfo poolInfo = new CellPoolInfo();
			public CellIndexInfo indexInfo = new CellIndexInfo();
			public CellBlendingInfo blendingInfo = new CellBlendingInfo();
			public CellStreamingInfo streamingInfo = new CellStreamingInfo();

			public int referenceCount = 0;
			public bool loaded; // "Loaded" means the streaming system decided the cell should be loaded. It does not mean it's ready for GPU consumption (because of blending or dist streaming)

			public CellData.PerScenarioData scenario0;
			public CellData.PerScenarioData scenario1;
			public bool hasTwoScenarios;

			public CellInstancedDebugProbes debugProbes;

			public int CompareTo(Cell other)
            {
				if (streamingInfo.streamingScore < other.streamingInfo.streamingScore)
					return -1;
				else if (streamingInfo.streamingScore > other.streamingInfo.streamingScore)
					return 1;
				else
					return 0;
            }

			public bool UpdateCellScenarioData(string scenario0, string scenario1)
            {
				if(!data.scenarios.TryGetValue(scenario0, out this.scenario0))
                {
					return false;
                }

				hasTwoScenarios = false;

                if (!string.IsNullOrEmpty(scenario1))
                {
					if (data.scenarios.TryGetValue(scenario1, out this.scenario1))
						hasTwoScenarios = true;
                }

				return true;
            }

			public void Clear()
            {
				desc = null;
				data = null;
				poolInfo.Clear();
				indexInfo.Clear();
				blendingInfo.Clear();
				streamingInfo.Clear();

				referenceCount = 0;
				loaded = false;
				scenario0 = default;
				scenario1 = default;
				hasTwoScenarios = false;

				debugProbes = null;
			}
        }

		internal struct Volume : IEquatable<Volume>
		{
			internal Vector3 corner;
			internal Vector3 X; // the vectors are NOT normalized, their length determines the size of the box
			internal Vector3 Y;
			internal Vector3 Z;

			internal float maxSubdivisionMultiplier;
			internal float minSubdivisionMultiplier;

			public Volume(Matrix4x4 trs, float maxSubdivision, float minSubdivision)
			{
				X = trs.GetColumn(0);
				Y = trs.GetColumn(1);
				Z = trs.GetColumn(2);
				corner = (Vector3)trs.GetColumn(3) - 0.5f * X - 0.5f * Y - 0.5f * Z;

				this.maxSubdivisionMultiplier = maxSubdivision;
				this.minSubdivisionMultiplier = minSubdivision;
			}

			public Volume(Vector3 corner, Vector3 X, Vector3 Y, Vector3 Z, float maxSubdivision = 1f, float minSubdivision = 0f)
			{
				this.corner = corner;
				this.X = X;
				this.Y = Y;
				this.Z = Z;
				this.maxSubdivisionMultiplier = maxSubdivision;
				this.minSubdivisionMultiplier = minSubdivision;
			}

			public Volume(Volume copy)
			{
				this.corner = copy.corner;
				this.X = copy.X;
				this.Y = copy.Y;
				this.Z = copy.Z;
				this.maxSubdivisionMultiplier = copy.maxSubdivisionMultiplier;
				this.minSubdivisionMultiplier = copy.minSubdivisionMultiplier;
			}

			public Volume(Bounds bounds)
			{
				var size = bounds.size;
				this.corner = bounds.center - size * 0.5f;
				X = new Vector3(size.x, 0f, 0f);
				Y = new Vector3(0f, size.y, 0f);
				Z = new Vector3(0f, 0f, size.z);

				maxSubdivisionMultiplier = minSubdivisionMultiplier = 0f;
			}

			public Bounds CalculateAABB()
			{
				Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
				Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

				for(int x = 0; x < 2; ++x)
				{
					for(int y = 0; y < 2; ++y)
					{
						for(int z = 0; z < 2; ++z)
						{
							Vector3 dir = new Vector3(x, y, z);
							Vector3 pt = corner + X * dir.x + Y * dir.y + Z * dir.z;
							min = Vector3.Min(min, pt);
							max = Vector3.Max(max, pt);
						}
					}
				}

				return new Bounds((min + max) * 0.5f, max - min);
			}

			public void CalculateCenterAndSize(out Vector3 center, out Vector3 size)
			{
				size = new Vector3(X.magnitude, Y.magnitude, Z.magnitude);
				center = corner + 0.5f * X + 0.5f * Y + 0.5f * Z;
			}

			public void Transform(Matrix4x4 trs)
			{
				corner = trs.MultiplyPoint(corner);
				X = trs.MultiplyVector(X);
				Z = trs.MultiplyVector(Y);
				Z = trs.MultiplyVector(Z);
			}

			public override string ToString()
			{
				return $"Corner: {corner}, X: {X}, Y: {Y}, Z: {Z}, MaxSubdiv: {maxSubdivisionMultiplier}";
			}

			public bool Equals(Volume other)
			{
				return corner == other.corner
					&& X == other.X
					&& Y == other.Y
					&& Z == other.Z
					&& minSubdivisionMultiplier == other.minSubdivisionMultiplier
					&& maxSubdivisionMultiplier == other.maxSubdivisionMultiplier;
			}
		}

		internal struct RefVolTransform
        {
			public Vector3 posWS;
			public Quaternion rot;
			public float scale;
        }

		/// <summary>
		/// The resources that are bound to the runtime shaders for sampling Adaptive Probe Volume data.
		/// </summary>
		public struct RuntimeResources
		{
			/// <summary>
			/// Index data to fetch the correct location in the Texture3D.
			/// </summary>
			public ComputeBuffer index;
			/// <summary>
			/// Indices of the various index buffers for each cell.
			/// </summary>
			public ComputeBuffer cellIndices;
			/// <summary>
			/// Texture containing Spherical Harmonics L0 band data and first coefficient of L1_R.
			/// </summary>
			public RenderTexture L0_L1rx;
			/// <summary>
			/// Texture containing the second channel of Spherical Harmonics L1 band data and second coefficient of L1_R.
			/// </summary>
			public RenderTexture L1_G_ry;
			/// <summary>
			/// Texture containing the second channel of Spherical Harmonics L1 band data and third coefficient of L1_R.
			/// </summary>
			public RenderTexture L1_B_rz;
			/// <summary>
			/// Texture containing the first coefficient of Spherical Harmonics L2 band data and first channel of the fifth.
			/// </summary>
			public RenderTexture L2_0;
			/// <summary>
			/// Texture containing the second coefficient of Spherical Harmonics L2 band data and second channel of the fifth.
			/// </summary>
			public RenderTexture L2_1;
			/// <summary>
			/// Texture containing the third coefficient of Spherical Harmonics L2 band data and third channel of the fifth.
			/// </summary>
			public RenderTexture L2_2;
			/// <summary>
			/// Texture containing the fourth coefficient of Spherical Harmonics L2 band data.
			/// </summary>
			public RenderTexture L2_3;

			/// <summary>
			/// Texture containing 4 light occlusion coefficients for each probe.
			/// </summary>
			public RenderTexture ProbeOcclusion;

			/// <summary>
			/// Texture containing packed validity binary data for the neighbourhood of each probe. Only used when L1. Otherwise this info is stored
			/// in the alpha channel of L2_3.
			/// </summary>
			public RenderTexture Validity;

			/// <summary>
			/// Texture containing Sky Occlusion SH data (only L0 and L1 band)
			/// </summary>
			public RenderTexture SkyOcclusionL0L1;

			/// <summary>
			/// Texture containing Sky shading direction indices
			/// </summary>
			public RenderTexture SkyShadingDirectionIndices;

			/// <summary>
			/// Precomputed table of shading directions for sky occlusion shading.
			/// </summary>
			public ComputeBuffer SkyPrecomputedDirections;

			/// <summary>
			/// Precomputed table of sampling mask for quality leak reduction
			/// </summary>
			public ComputeBuffer QualityLeakReductionData;
		}

		private bool m_IsInitialized = false;
		private bool m_SupportScenarios = false;
		private bool m_SupportScenarioBlending = false;
		private bool m_ForceNoDiskStreaming = false;
		private bool m_SupportDiskStreaming = false;
		private bool m_SupportGPUStreaming = false;
		private bool m_UseStreamingAssets = true;

		private float m_MinBrickSize;
		private int m_MaxSubdivision;

		private Vector3 m_ProbeOffset;

		private ProbeBrickPool m_Pool;
		private ProbeBrickIndex m_Index;
		private ProbeGlobalIndirection m_CellIndices;
		private ProbeBrickBlendingPool m_BlendingPool;

		private List<Chunk> m_TmpSrcChunks = new List<Chunk>();
		private float[] m_PositionOffsets = new float[ProbeBrickPool.kBrickProbeCountPerDim];
		private Bounds m_CurrGlobalBounds = new Bounds();

		internal Bounds globalBounds
        {
            get
            {
				return m_CurrGlobalBounds;
            }
            set
            {
				m_CurrGlobalBounds = value;
            }
        }

		internal Dictionary<int, Cell> cells = new Dictionary<int, Cell>();
		private ObjectPool<Cell> m_CellPool = new ObjectPool<Cell>(x => x.Clear(), null, false);

		private ProbeBrickPool.DataLocation m_TemporaryDataLocation;
		private int m_TemporaryDataLocationMemCoast;

#pragma warning disable 618
		[Obsolete("This field is only kept for migration purpose.")]
		internal ProbeVolumeSceneData sceneData; // Kept for migration
#pragma warning restore 618

		// We need to keep track the area, in cells, that is currently loaded. The index buffer will cover even unloaded areas, but we want to avoid sampling outside those areas.
		private Vector3Int minLoadedCellPos = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
		private Vector3Int maxLoadedCellPos = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);

		/// <summary>
		/// The input to the retrieveExtraDataAction action.
		/// </summary>
		public struct ExtraDataActionInput
        {
			// Empty, but defined to make this future proof without having to change public API
		}

		/// <summary>
        /// An action that is used by the SRP to retrieve extra data that was baked together with the bake
        /// </summary>
		public Action<ExtraDataActionInput> retrieveExtraDataAction;

		/// <summary>
		/// An action that is used by the SRP to perform checks every frame during baking.
		/// </summary>
		public Action checksDuringBakeAction = null;

		// Information of the probe volume scenes that is being loaded (if one is pending)
		private Dictionary<string, (ProbeVolumeBakingSet, List<int>)> m_PendingScenesToBeLoaded = new Dictionary<string, (ProbeVolumeBakingSet, List<int>)>();

		// Information on probes we need to remove.
		private Dictionary<string, List<int>> m_PendingScenesToBeUnloaded = new Dictionary<string, List<int>>();
		// Information of the probe volume scenes that is being loaded (if one is pending)
		private List<string> m_ActiveScenes = new List<string>();

		private ProbeVolumeBakingSet m_CurrentBakingSet = null;

		private bool m_NeedLoadAsset = false;
		private bool m_ProbeReferenceVolumeInit = false;
		private bool m_EnabledBySRP;
		private bool m_VertexSampling = false;

		/// <summary>Is Probe Volume initialized.</summary>
		public bool isInitialized => m_IsInitialized;
		internal bool enabledBySRP => m_EnabledBySRP;
		internal bool vertexSampling => m_VertexSampling;

		internal bool hasUnloadedCells => m_ToBeLoadedCells.size != 0;

		internal bool supportLightingScenarios => m_SupportScenarios;
		internal bool supportScenarioBlending => m_SupportScenarioBlending;
		internal bool gpuStreamingEnabled => m_SupportGPUStreaming;
		internal bool diskStreamingEnabled => m_SupportDiskStreaming && !m_ForceNoDiskStreaming;

		/// <summary>
		/// Whether APV stores occlusion for mixed lights.
		/// </summary>
		public bool probeOcclusion
        {
			get => m_CurrentBakingSet ? m_CurrentBakingSet.bakedProbeOcclusion : false;
        }

		/// <summary>
		/// Whether APV handles sky dynamically (with baked sky occlusion) or fully statically.
		/// </summary>
		public bool skyOcclusion
		{
			get => m_CurrentBakingSet ? m_CurrentBakingSet.bakedSkyOcclusion : false;
		}

		/// <summary>
		/// Bake sky shading direction.
		/// </summary>
		public bool skyOcclusionShadingDirection
		{
			get => m_CurrentBakingSet ? m_CurrentBakingSet.bakedSkyShadingDirection : false;
		}

		private bool useRenderingLayers => m_CurrentBakingSet.bakedMaskCount != 1;

		private bool m_NeedsIndexRebuild = false;
		private bool m_HasChangeIndex = false;

		private int m_CBShaderID = Shader.PropertyToID("ShaderVariablesProbeVolumes");

		private ProbeVolumeTextureMemoryBudget m_MemoryBudget;
		private ProbeVolumeBlendingTextureMemoryBudget m_BlendingMemoryBudget;
		private ProbeVolumeSHBands m_SHBands;

		/// <summary>
		/// The <see cref="ProbeVolumeSHBands"/>
		/// </summary>
		public ProbeVolumeSHBands shBands => m_SHBands;

		internal bool clearAssetsOnVolumeClear = false;

		/// <summary>
		/// The active baking set.
		/// </summary>
		public ProbeVolumeBakingSet currentBakingSet => m_CurrentBakingSet;

		/// <summary>The active lighting scenario.</summary>
		public string lightingScenario
		{
			get => m_CurrentBakingSet ? m_CurrentBakingSet.lightingScenario : null;
			set
			{
				SetActiveScenario(value);
			}
		}

		/// <summary>The lighting scenario APV is blending toward.</summary>
		public string otherScenario
		{
			get => m_CurrentBakingSet ? m_CurrentBakingSet.otherScenario : null;
		}

		/// <summary>The blending factor currently used to blend probe data. A value of 0 means blending is not active.</summary>
		public float scenarioBlendingFactor
		{
			get => m_CurrentBakingSet ? m_CurrentBakingSet.scenarioBlendingFactor : 0.0f;
			set
			{
				if (m_CurrentBakingSet != null)
					m_CurrentBakingSet.BlendLightingScenario(m_CurrentBakingSet.otherScenario, value);
			}
		}

		internal static string GetSceneGUID(Scene scene) => scene.GetGUID();

		internal void SetActiveScenario(string scenario, bool verbose = true)
		{
			if (m_CurrentBakingSet != null)
				m_CurrentBakingSet.SetActiveScenario(scenario, verbose);
		}

		/// <summary>
		/// Allows smooth transitions between two lighting scenarios. This only affects the runtime data used for lighting.
		/// </summary>
		/// <param name="otherScenario"></param>
		/// <param name="blendingFactor"></param>
		public void BlendLightingScenario(string otherScenario, float blendingFactor)
        {
			if (m_CurrentBakingSet != null)
				m_CurrentBakingSet.BlendLightingScenario(otherScenario, blendingFactor);
        }

		internal static string defaultLightingScenario = "Default";

		/// <summary>
		/// Get the memory budget for the Probe Volume system.
		/// </summary>
		public ProbeVolumeTextureMemoryBudget memoryBudget => m_MemoryBudget;

		private static ProbeReferenceVolume _instance = new ProbeReferenceVolume();

		internal List<ProbeVolumePerSceneData> perSceneDataList
		{
			get;
			private set;
		} = new List<ProbeVolumePerSceneData>();

		internal void RegisterPerSceneData(ProbeVolumePerSceneData data)
        {
            if (!perSceneDataList.Contains(data))
            {
				perSceneDataList.Add(data);

				// Registration can happen before APV (or even the current pipeline) is initialized, so in this case we need to delay the init.
				if (m_IsInitialized)
					data.Initialize();
            }
        }

		public void SetActiveScene(Scene scene)
        {
			if (TryGetPerSceneData(GetSceneGUID(scene), out var perSceneData))
				SetActiveBakingSet(perSceneData.serializedBakingSet);
        }

		/// <summary>
        /// Set the currently active baking set
        /// Can be used when loading additively two scenes belonging to different baking sets to control which one is active
        /// </summary>
        /// <param name="bakingSet"></param>
		public void SetActiveBakingSet(ProbeVolumeBakingSet bakingSet)
        {
			if (m_CurrentBakingSet == bakingSet)
				return;

			foreach (var data in perSceneDataList)
				data.QueueSceneRemoval();

			UnloadBakingSet();
			SetBakingSetAsCurrent(bakingSet);

			if(m_CurrentBakingSet != null)
            {
				foreach (var data in perSceneDataList)
					data.QueueSceneLoading();
            }
        }

		private void SetBakingSetAsCurrent(ProbeVolumeBakingSet bakingSet)
		{
			m_CurrentBakingSet = bakingSet;

			// Can happen when you have only one scene loaded and you remove it from any baking set.
			if (m_CurrentBakingSet != null)
			{
				// Delay first time init to after baking set is loaded to ensure we allocate what's needed
				InitProbeReferenceVolume();

				m_CurrentBakingSet.Initialize(m_UseStreamingAssets);
				m_CurrGlobalBounds = m_CurrentBakingSet.globalBounds;
				SetSubdivisionDimensions(bakingSet.minBrickSize, bakingSet.maxSubdivision, bakingSet.bakedProbeOffset);

				m_NeedsIndexRebuild = true;
			}
		}

		internal void RegisterBakingSet(ProbeVolumePerSceneData data)
		{
			if (m_CurrentBakingSet == null)
			{
				SetBakingSetAsCurrent(data.serializedBakingSet);
			}
		}

		internal void UnloadBakingSet()
		{
			// Need to make sure everything is unloaded before killing the baking set ref (we need it to unload cell CPU data).
			PerformPendingOperations();

			if (m_CurrentBakingSet != null)
				m_CurrentBakingSet.Cleanup();
			m_CurrentBakingSet = null;
			m_CurrGlobalBounds = new Bounds();

			// Restart pool from zero to avoid unnecessary memory consumption when going from a big to a small scene.
			if(m_ScratchBufferPool != null)
            {
				m_ScratchBufferPool.Cleanup();
				m_ScratchBufferPool = null;
            }

		}

		internal void UnregisterPerSceneData(ProbeVolumePerSceneData data)
		{
			perSceneDataList.Remove(data);
			if (perSceneDataList.Count == 0)
				UnloadBakingSet();
		}

		internal bool TryGetPerSceneData(string sceneGUID, out ProbeVolumePerSceneData perSceneData)
        {
			foreach(var data in perSceneDataList)
            {
				if(GetSceneGUID(data.gameObject.scene) == sceneGUID)
                {
					perSceneData = data;
					return true;
                }
            }

			perSceneData = null;
			return false;
        }

		internal float indexFragmentationRate { get => m_ProbeReferenceVolumeInit ? m_Index.fragmentationRate : 0; }

		/// <summary>
		/// Get the instance of the probe reference volume (singleton)
		/// </summary>
		public static ProbeReferenceVolume instance => _instance;

		/// <summary>
		/// Initialize the Probe Volume system
		/// </summary>
		/// <param name="parameters">Initialization parameters.</param>
		public void Initialize(in ProbeVolumeSystemParameters parameters)
        {
            if (m_IsInitialized)
            {
				Debug.LogError("Probe Volume System has already been initialized.");
				return;
            }

			bool getSettings = BXRenderPipeline.TryGetRenderCommonSettings(out var settings);
			var probeVolumeSettings = settings?.probeVolumeGlobalSettings;

			m_MemoryBudget = parameters.memoryBudget;
			m_BlendingMemoryBudget = parameters.blendingMemoryBudget;
			m_SupportScenarios = parameters.supportScenarios;
			m_SupportScenarioBlending = parameters.supportScenarios && parameters.supportScenarioBlending && SystemInfo.supportsComputeShaders && m_BlendingMemoryBudget != 0;
			m_SHBands = parameters.shBands;
			m_UseStreamingAssets = getSettings ? !probeVolumeSettings.probeVolumeDisableStreamingAssets : false;
#if UNITY_EDITOR
			// In editor we can always use Streaming Assets. This optimizes memory usage for editing.
			m_UseStreamingAssets = true;
#endif
			m_SupportGPUStreaming = parameters.supportGPUStreaming;
			// GPU Streaming is required for Dikst Streaming
			var streamingUploadCS = settings?.probeVolumeRuntimeResources.probeVolumeUploadDataCS;
			var streamingUploadL2CS = settings?.probeVolumeRuntimeResources.probeVolumeUploadDataL2CS;
			// For now this condition is redundant with m_SupportDiskStreaming but we plan to support disk streaming without compute int the furture
			// So we need to split the conditions to plan for that
			m_DiskStreamingUseCompute = SystemInfo.supportsComputeShaders && streamingUploadCS != null && streamingUploadL2CS != null;
			InitializeDebug();
			ProbeVolumeConstantRuntimeResources.Initialize();
			ProbeBrickPool.Initialize();
			ProbeBrickBlendingPool.Initialize();
			InitStreaming();

			m_IsInitialized = true;
			m_NeedsIndexRebuild = true;
#pragma warning disable 618
			sceneData = parameters.sceneData;
#pragma warning restore 618

#if UNITY_EDITOR
			UnityEditor.SceneManagement.EditorSceneManager.sceneSaving += ProbeVolumeBakingSet.OnSceneSaving;
			ProbeVolumeBakingSet.SyncBakingSets();
#endif
			m_EnabledBySRP = true;

			foreach (var data in perSceneDataList)
				data.Initialize();
		}

		/// <summary>
		/// Communicate to the Probe Volume system whether the SRP enables Probe Volume.
		/// It is important to keep in mind that this is not used by the system for anything else but book-keeping,
		/// the SRP is still responsible to disable anything Probe volume related on SRP side.
		/// </summary>
		/// <param name="srpEnablesPV">The value of the new enabled</param>
		public void SetEnableStateFromSRP(bool srpEnablesPV)
        {
			m_EnabledBySRP = srpEnablesPV;
        }

		/// <summary>
		/// Communicate to the Probe Volume system whether the SRP uses per vertex sampling
		/// </summary>
		/// <param name="value">True for vertex sampling, false for pixel sampling</param>
		public void SetVertexSamplingEnable(bool value)
        {
			m_VertexSampling = value;
        }

		// This is used for steps such as dilation that require the maximum order allowed to be loaded at all times. Should really never be used as a general purpose function.
		internal void ForceSHBand(ProbeVolumeSHBands shBands)
        {
			m_SHBands = shBands;

			DeinitProbeReferenceVolume();

			foreach (var data in perSceneDataList)
				data.Initialize();

			PerformPendingOperations();
        }

		internal void ForceNoDiskStreaming(bool state)
        {
			m_ForceNoDiskStreaming = state;
        }

		/// <summary>
        /// Cleanup the Probe Volume system
        /// </summary>
		public void Cleanup()
        {
			CoreUtils.SafeRelease(m_EmptyIndexBuffer);
			ProbeVolumeConstantRuntimeResources.Cleanup();
        }




		internal static int CellSize(int subdivisionLevel) => (int)Mathf.Pow(ProbeBrickPool.kBrickCellCount, subdivisionLevel);

		internal float BrickSize(int subdivisionLevel) => m_MinBrickSize * CellSize(subdivisionLevel);
		internal float MinBrickSize() => m_MinBrickSize;
		internal float MaxBrickSize() => BrickSize(m_MaxSubdivision - 1);
		internal float GetDistanceBetweenProbes(int subdivisionLevel) => BrickSize(subdivisionLevel) / 3.0f;
		internal float MinDistanceBetweenProbes() => GetDistanceBetweenProbes(0);
		internal int GetMaxSubdivision() => m_MaxSubdivision;
		internal int GetMaxSubdivision(float multiplier) => Mathf.CeilToInt(m_MaxSubdivision * multiplier);
		internal Vector3 ProbeOffset() => m_ProbeOffset;

		// IMPORTANT! IF THIS VALUE CHANGES DATA NEEDS TO BE REBAKED
		internal int GetGlobalIndirectionEntryMaxSubdiv() => ProbeGlobalIndirection.kEntryMaxSubdivLevel;

		internal int GetEntrySubdivLevel() => Mathf.Min(ProbeGlobalIndirection.kEntryMaxSubdivLevel, m_MaxSubdivision - 1);
		internal float GetEntrySize() => BrickSize(GetEntrySubdivLevel());

		/// <summary>
        /// Returns whether any brick data has been loaded.
        /// </summary>
        /// <returns>True if brick data is present, otherwise false.</returns>
		public bool DataHasBeenLoaded() => m_LoadedCells.size != 0;











		internal void SetMaxSubdivision(int maxSubdivision)
		{
			int newValue = Math.Min(maxSubdivision, ProbeBrickIndex.kMaxSubdivisionLevels);
			if (newValue != m_MaxSubdivision)
			{
				m_MaxSubdivision = newValue;
				InitializeGlobalIndirection();
			}
		}

		private void UpdatePool(List<Chunk> chunkList, CellData.PerScenarioData data, NativeArray<byte> validityNeighMaskData,
			NativeArray<ushort> skyOcclusionDataL0L1, NativeArray<byte> skyShadingDirectionIndices,
			int chunkIndex, int poolIndex)
		{
			var chunkSizeInProbes = ProbeBrickPool.GetChunkSizeInProbeCount();

			UpdateDataLocationTexture(m_TemporaryDataLocation.TexL0_L1rx, data.shL0L1RxData.GetSubArray(chunkIndex * chunkSizeInProbes * 4, chunkSizeInProbes * 4));
			UpdateDataLocationTexture(m_TemporaryDataLocation.TexL1_G_ry, data.shL1GL1RyData.GetSubArray(chunkIndex * chunkSizeInProbes * 4, chunkSizeInProbes * 4));
			UpdateDataLocationTexture(m_TemporaryDataLocation.TexL1_B_rz, data.shL1BL1RzData.GetSubArray(chunkIndex * chunkSizeInProbes * 4, chunkSizeInProbes * 4));

			if (m_SHBands == ProbeVolumeSHBands.SphericalHarmonicsL2 && data.shL2Data_0.Length > 0)
			{
				UpdateDataLocationTexture(m_TemporaryDataLocation.TexL2_0, data.shL2Data_0.GetSubArray(chunkIndex * chunkSizeInProbes * 4, chunkSizeInProbes * 4));
				UpdateDataLocationTexture(m_TemporaryDataLocation.TexL2_1, data.shL2Data_1.GetSubArray(chunkIndex * chunkSizeInProbes * 4, chunkSizeInProbes * 4));
				UpdateDataLocationTexture(m_TemporaryDataLocation.TexL2_2, data.shL2Data_2.GetSubArray(chunkIndex * chunkSizeInProbes * 4, chunkSizeInProbes * 4));
				UpdateDataLocationTexture(m_TemporaryDataLocation.TexL2_3, data.shL2Data_3.GetSubArray(chunkIndex * chunkSizeInProbes * 4, chunkSizeInProbes * 4));
			}

			if (probeOcclusion && data.probeOcclusion.Length > 0)
			{
				UpdateDataLocationTexture(m_TemporaryDataLocation.TexProbeOcclusion, data.probeOcclusion.GetSubArray(chunkIndex * chunkSizeInProbes * 4, chunkSizeInProbes * 4));
			}

			if (poolIndex == -1) // shared data that don't need to be updated per scenario
			{
				if (validityNeighMaskData.Length > 0)
				{
					if (m_CurrentBakingSet.bakedMaskCount == 1)
						UpdateDataLocationTextureMask(m_TemporaryDataLocation.TexValidity, validityNeighMaskData.GetSubArray(chunkIndex * chunkSizeInProbes, chunkSizeInProbes));
					else
						UpdateDataLocationTexture(m_TemporaryDataLocation.TexValidity, validityNeighMaskData.Reinterpret<uint>(1).GetSubArray(chunkIndex * chunkSizeInProbes, chunkSizeInProbes));
				}

				if (skyOcclusion && skyOcclusionDataL0L1.Length > 0)
				{
					UpdateDataLocationTexture(m_TemporaryDataLocation.TexSkyOcclusion, skyOcclusionDataL0L1.GetSubArray(chunkIndex * chunkSizeInProbes * 4, chunkSizeInProbes * 4));
				}

				if (skyOcclusionShadingDirection && skyShadingDirectionIndices.Length > 0)
				{
					UpdateDataLocationTexture(m_TemporaryDataLocation.TexSkyShadingDirectionIndices, skyShadingDirectionIndices.GetSubArray(chunkIndex * chunkSizeInProbes, chunkSizeInProbes));
				}
			}

			// New data format only uploads one chunk at a time (we need predictable chunk size)
			var srcChunks = GetSourceLocations(1, ProbeBrickPool.GetChunkSizeInBrickCount(), m_TemporaryDataLocation);

			// Update pool textures with incoming SH data and ignore any potential frame latency related issues for now.
			if (poolIndex == -1)
				m_Pool.Update(m_TemporaryDataLocation, srcChunks, chunkList, chunkIndex, m_SHBands);
		}

		private void UpdateDataLocationTexture<T>(Texture output, NativeArray<T> input) where T : struct
		{
			var outputNativeArray = (output as Texture3D).GetPixelData<T>(0);
			Debug.Assert(outputNativeArray.Length >= input.Length);
			outputNativeArray.GetSubArray(0, input.Length).CopyFrom(input);
			(output as Texture3D).Apply();
		}

		private void UpdateDataLocationTextureMask(Texture output, NativeArray<byte> input)
		{
			// On some platforms, single channel unorm format isn't supported, so validity uses 4 channel unorm format.
			// Then we can't directly copy the data, but need to account for the 3 unused channels.
			uint numComponents = GraphicsFormatUtility.GetComponentCount(output.graphicsFormat);
			if (numComponents == 1)
			{
				UpdateDataLocationTexture(output, input);
			}
			else
			{
				Debug.Assert(output.graphicsFormat == GraphicsFormat.R8G8B8A8_UNorm);
				var outputNativeData = (output as Texture3D).GetPixelData<(byte, byte, byte, byte)>(0);
				Debug.Assert(outputNativeData.Length >= input.Length);
				for (int i = 0; i < input.Length; ++i)
				{
					outputNativeData[i] = (input[i], input[i], input[i], input[i]);
				}
				(output as Texture3D).Apply();
			}
		}

		private void UpdatePoolAndIndex(Cell cell, CellStreamingScratchBuffer dataBuffer, CellStreamingScratchBufferLayout layout, int poolIndex, CommandBuffer cmd)
		{
			if (diskStreamingEnabled)
			{
				if (m_DiskStreamingUseCompute)
				{
					Debug.Assert(dataBuffer.buffer != null);
					UpdatePool(cmd, cell.poolInfo.chunkList, dataBuffer, layout, poolIndex);
				}
				else
				{
					int chunkCount = cell.poolInfo.chunkList.Count;
					int offsetAdjustment = -2 * (chunkCount * 4 * sizeof(uint)); // NOTE: account for offsets adding "2 * (chunkCount * 4 * sizeof(uint))" in the calculations from ProbeVolumeScratchBufferPool::GetOrCreateScratchBufferLayout()

					CellData.PerScenarioData data = default;
					data.shL0L1RxData = dataBuffer.stagingBuffer.GetSubArray(layout._L0L1rxOffset + offsetAdjustment, chunkCount * layout._L0Size).Reinterpret<ushort>(sizeof(byte));
					data.shL1GL1RyData = dataBuffer.stagingBuffer.GetSubArray(layout._L1GryOffset + offsetAdjustment, chunkCount * layout._L1Size);
					data.shL1BL1RzData = dataBuffer.stagingBuffer.GetSubArray(layout._L1BrzOffset + offsetAdjustment, chunkCount * layout._L1Size);

					NativeArray<byte> validityNeighMaskData = dataBuffer.stagingBuffer.GetSubArray(layout._ValidityOffset + offsetAdjustment, chunkCount * layout._ValiditySize);

					if (m_SHBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
					{
						data.shL2Data_0 = dataBuffer.stagingBuffer.GetSubArray(layout._L2_0Offset + offsetAdjustment, chunkCount * layout._L2Size);
						data.shL2Data_1 = dataBuffer.stagingBuffer.GetSubArray(layout._L2_1Offset + offsetAdjustment, chunkCount * layout._L2Size);
						data.shL2Data_2 = dataBuffer.stagingBuffer.GetSubArray(layout._L2_2Offset + offsetAdjustment, chunkCount * layout._L2Size);
						data.shL2Data_3 = dataBuffer.stagingBuffer.GetSubArray(layout._L2_3Offset + offsetAdjustment, chunkCount * layout._L2Size);
					}

					if (probeOcclusion && layout._ProbeOcclusionSize > 0)
					{
						data.probeOcclusion = dataBuffer.stagingBuffer.GetSubArray(layout._ProbeOcclusionOffset + offsetAdjustment, chunkCount * layout._ProbeOcclusionSize);
					}

					NativeArray<ushort> skyOcclusionData = default;
					if (skyOcclusion && layout._SkyOcclusionSize > 0)
					{
						skyOcclusionData = dataBuffer.stagingBuffer.GetSubArray(layout._SkyOcclusionOffset + offsetAdjustment, chunkCount * layout._SkyOcclusionSize).Reinterpret<ushort>(sizeof(byte));
					}

					NativeArray<byte> skyOcclusionDirectionData = default;
					if (skyOcclusion && skyOcclusionShadingDirection && layout._SkyShadingDirectionSize > 0)
					{
						skyOcclusionDirectionData = dataBuffer.stagingBuffer.GetSubArray(layout._SkyShadingDirectionOffset + offsetAdjustment, chunkCount * layout._SkyShadingDirectionSize);
					}

					for (int chunkIndex = 0; chunkIndex < chunkCount; ++chunkIndex)
					{
						UpdatePool(cell.poolInfo.chunkList, data, validityNeighMaskData, skyOcclusionData, skyOcclusionDirectionData, chunkIndex, poolIndex);
					}
				}
			}
			else
			{
				// In order not to pre-allocate for the worse case, we update the texture by smaller chunks with a preallocated DataLoc
				for (int chunkIndex = 0; chunkIndex < cell.poolInfo.chunkList.Count; ++chunkIndex)
				{
					UpdatePool(cell.poolInfo.chunkList, cell.scenario0, cell.data.validityNeighMaskData, cell.data.skyOcclusionDataL0L1, cell.data.skyShadingDirectionIndices, chunkIndex, poolIndex);
				}
			}

			// Index may already be updated when simply switching scenarios.
			if (!cell.indexInfo.indexUpdated)
			{
				UpdateCellIndex(cell);
			}
		}

		private void UpdatePool(CommandBuffer cmd, List<Chunk> chunkList, CellStreamingScratchBuffer dataBuffer, CellStreamingScratchBufferLayout layout, int poolIndex)
		{
			// Update pool textures with incoming SH data and ignore any potential frame latency related issues for now.
			if (poolIndex == -1)
				m_Pool.Update(cmd, dataBuffer, layout, chunkList, true, m_Pool.GetValidityTexture(), m_SHBands,
					skyOcclusion, m_Pool.GetSkyOcclusionTexture(), skyOcclusionShadingDirection, m_Pool.GetSkyShadingDirectionIndicesTexture(), probeOcclusion);
			else
				m_BlendingPool.Update(cmd, dataBuffer, layout, chunkList, m_SHBands, poolIndex,
					m_Pool.GetValidityTexture(), skyOcclusion, m_Pool.GetSkyOcclusionTexture(), skyOcclusionShadingDirection, m_Pool.GetSkyShadingDirectionIndicesTexture(), probeOcclusion);
		}

		private void UpdateCellIndex(Cell cell)
		{
			cell.indexInfo.indexUpdated = true;

			// Build Index
			var bricks = cell.data.bricks;
			m_Index.AddBricks(cell.indexInfo, bricks, cell.poolInfo.chunkList, ProbeBrickPool.GetChunkSizeInBrickCount(), m_Pool.GetPoolWidth(), m_Pool.GetPoolHeight());

			// Update indirection buffer
			m_CellIndices.UpdateCell(cell.indexInfo);
		}

		// Currently only used for 1 chunk at a time but kept in case we need more in the future.
		private List<Chunk> GetSourceLocations(int count, int chunkSize, ProbeBrickPool.DataLocation dataLoc)
		{
			var c = new Chunk();
			m_TmpSrcChunks.Clear();
			m_TmpSrcChunks.Add(c);

			// currently this code assumes that the texture width is a multiple of the allocation chunk size
			for (int j = 1; j < count; ++j)
			{
				c.x += chunkSize * ProbeBrickPool.kBrickProbeCountPerDim;
				if (c.x >= dataLoc.width)
				{
					c.x = 0;
					c.y += ProbeBrickPool.kBrickProbeCountPerDim;
					if (c.z >= dataLoc.height)
					{
						c.y = 0;
						c.z += ProbeBrickPool.kBrickProbeCountPerDim;
					}
				}
				m_TmpSrcChunks.Add(c);
			}

			return m_TmpSrcChunks;
		}

		internal void SetSubdivisionDimensions(float minBrickSize, int maxSubdiv, Vector3 offset)
        {
			m_MinBrickSize = minBrickSize;
			SetMaxSubdivision(maxSubdiv);
			m_ProbeOffset = offset;
        }

		internal void InitializeGlobalIndirection()
        {
			// Current baking set can be null at init and we still need the buffers to valid.
			var minCellPosition = m_CurrentBakingSet ? m_CurrentBakingSet.minCellPosition : Vector3Int.zero;
			var maxCellPosition = m_CurrentBakingSet ? m_CurrentBakingSet.maxCellPosition : Vector3Int.zero;
			if (m_CellIndices != null)
				m_CellIndices.Cleanup();
			m_CellIndices = new ProbeGlobalIndirection(minCellPosition, maxCellPosition, Mathf.Max(1, (int)Mathf.Pow(3, m_MaxSubdivision - 1)));
            if (m_SupportGPUStreaming)
            {
				if (m_DefragCellIndices != null)
					m_DefragCellIndices.Cleanup();
				m_DefragCellIndices = new ProbeGlobalIndirection(minCellPosition, maxCellPosition, Mathf.Max(1, (int)Mathf.Pow(3, m_MaxSubdivision - 1)));
            }
		}

		internal void AddPendingSceneLoading(string sceneGUID, ProbeVolumeBakingSet bakingSet)
        {
            if (m_PendingScenesToBeLoaded.ContainsKey(sceneGUID))
            {
				m_PendingScenesToBeLoaded.Remove(sceneGUID);
            }

			// User might have loaded other scenes with probe volumes but not belonging to the "single scene" baking set.
			if (bakingSet == null && m_CurrentBakingSet != null && m_CurrentBakingSet.singleSceneMode)
				return;

			if(bakingSet.chunkSizeInBricks != ProbeBrickPool.GetChunkSizeInBrickCount())
            {
				Debug.LogError($"Trying to load Adaptive Probe Volumes data ({bakingSet.name}) baked with an older incompatible version of APV. Please rebake your data.");
				return;
            }

			if(m_CurrentBakingSet != null && bakingSet != m_CurrentBakingSet)
            {
				// Trying to load data for a scene from a different baking set than currently loaded ones.
				// This should not throw an error, but it's not supported
				return;
			}

			// If we don't have any loaded asset yet, we need to verify the other queued assets.
			// Only need to check one entry here, they should all have the same baking set by construction.
			if(m_PendingScenesToBeLoaded.Count != 0)
            {
				foreach(var toBeLoadedBakingSet in m_PendingScenesToBeLoaded.Values)
                {
					if(bakingSet != toBeLoadedBakingSet.Item1)
                    {
						Debug.LogError($"Trying to load Adaptive Probe Volumes data for a scene from a different baking set from other scenes that are being loaded." +
							$"Please make sure all loaded scenes are in the same baking set.");
						return;
                    }

					break;
                }
            }

			m_PendingScenesToBeLoaded.Add(sceneGUID, (bakingSet, m_CurrentBakingSet.GetSceneCellIndexList(sceneGUID)));
			m_NeedLoadAsset = true;
		}

		internal void AddPendingSceneRemoval(string sceneGUID)
        {
			if (m_PendingScenesToBeLoaded.ContainsKey(sceneGUID))
				m_PendingScenesToBeLoaded.Remove(sceneGUID);
			if (m_ActiveScenes.Contains(sceneGUID) && m_CurrentBakingSet != null)
				m_PendingScenesToBeUnloaded.TryAdd(sceneGUID, m_CurrentBakingSet.GetSceneCellIndexList(sceneGUID));
        }

		internal void UnloadAllCells()
		{

		}

		// This one is internal for baking purpose only.
		// Calling this from "outside" will not properly update Loaded/ToBeLoadedCells arrays and thus will break the state of streaming.
		internal bool LoadCell(Cell cell, bool ignoreErrorLog = false)
		{
			// First try to allocate pool memory. This is what is most likely to fail.
			return false;
		}

		internal void UnloadBlendingCell(Cell cell)
		{

		}

		internal bool AddBlendingBricks(Cell cell)
		{
			return false;
		}
	}
}
