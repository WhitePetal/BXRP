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
		public float skyOcclusionShadingDirection;
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
		internal struct CellDesc
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

		internal static int CellSize(int subdivisionLevel) => (int)Mathf.Pow(ProbeBrickPool.kBrickCellCount, subdivisionLevel);

		private static ProbeReferenceVolume _instance = new ProbeReferenceVolume();

		/// <summary>
		/// Get the instance of the probe reference volume (singleton)
		/// </summary>
		public static ProbeReferenceVolume instance => _instance;

		private ProbeBrickIndex m_Index;

		private bool m_ProbeReferenceVolumeInit;

		internal float indexFragmentationRate { get => m_ProbeReferenceVolumeInit ? m_Index.fragmentationRate : 0; }

		private int m_MaxSubdivision;
		private float m_MinBrickSize;
		private float m_MaxBrickSize;
		private Vector3 m_ProbeOffset;

		internal int GetMaxSubdivision() => m_MaxSubdivision;
		internal float MinBrickSize() => m_MinBrickSize;
		internal float MaxBrickSize() => m_MaxBrickSize;
		internal Vector3 ProbeOffset() => m_ProbeOffset;

		private bool m_IsInitialized;
		private bool m_EnabledBySRP;
		private bool m_SupportScenarioBlending;
		private bool m_SupportGPUStreaming;
		private bool m_SupportDiskStreaming;
		private bool m_ForceNoDiskStreaming;

		internal bool supportScenarioBlending => m_SupportScenarioBlending;
		internal bool gpuStreamingEnabled => m_SupportGPUStreaming;
		internal bool diskStreamingEnabled => m_SupportDiskStreaming && !m_ForceNoDiskStreaming;

		/// <summary>
		/// This Probe Volume is initialized
		/// </summary>
		public bool isInitialized => m_IsInitialized;
		internal bool enabledBySRP => m_EnabledBySRP;

		private ProbeVolumeBakingSet m_CurrentBakingSet = null;

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

		internal Dictionary<int, Cell> cells = new Dictionary<int, Cell>();

		private ProbeVolumeSHBands m_SHBands;
	}
}
