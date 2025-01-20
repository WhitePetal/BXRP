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
			Debug.Assert(s_SceneGUID != null, "Reflection for scene GUID failed");
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
			//public CellStreamingRequest
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

			//public CellInstanceDebugProbes debugProbes;

            public int CompareTo(Cell other)
            {
                throw new NotImplementedException();
            }
        }


		internal static int CellSize(int subdivisionLevel) => (int)Mathf.Pow(ProbeBrickPool.kBrickCellCount, subdivisionLevel);
	}
}
