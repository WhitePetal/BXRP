using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace BXRenderPipeline
{
	internal class ProbeBrickPool
	{
		internal static readonly int _Out_L0_L1Rx = Shader.PropertyToID("_Out_L0_L1Rx");
		internal static readonly int _Out_L1G_L1Ry = Shader.PropertyToID("_Out_L1G_L1Ry");
		internal static readonly int _Out_L1B_L1Rz = Shader.PropertyToID("_Out_L1B_L1Rz");
		internal static readonly int _Out_Shared = Shader.PropertyToID("_Out_Shared");
		internal static readonly int _Out_ProbeOcclusion = Shader.PropertyToID("_Out_ProbeOcclusion");
		internal static readonly int _Out_SkyOcclusionL0L1 = Shader.PropertyToID("_Out_SkyOcclusionL0L1");
		internal static readonly int _Out_SkyShadingDirectionIndices = Shader.PropertyToID("_Out_SkyShadingDirectionIndices");
		internal static readonly int _Out_L2_0 = Shader.PropertyToID("_Out_L2_0");
		internal static readonly int _Out_L2_1 = Shader.PropertyToID("_Out_L2_1");
		internal static readonly int _Out_L2_2 = Shader.PropertyToID("_Out_L2_2");
		internal static readonly int _Out_L2_3 = Shader.PropertyToID("_Out_L2_3");
		internal static readonly int _ProbeVolumeScratchBufferLayout = Shader.PropertyToID(nameof(ProbeReferenceVolume.CellStreamingScratchBufferLayout));
		internal static readonly int _ProbeVolumeScratchBuffer = Shader.PropertyToID("_ScratchBuffer");

		internal static int DivRoundUp(int x, int y) => (x + y - 1) / y;

		const int kChunkSizeInBricks = 128;

		[DebuggerDisplay("Chunk ({x}, {y}, {z})")]
		public struct BrickChunkAlloc
		{
			public int x, y, z;

			internal int flattenIndex(int sx, int sy) { return z * (sx * sy) + y * sx + x; }
		}

		public struct DataLocation
		{
			internal Texture TexL0_L1rx;

			internal Texture TexL1_G_ry;
			internal Texture TexL1_B_rz;

			internal Texture TexL2_0;
			internal Texture TexL2_1;
			internal Texture TexL2_2;
			internal Texture TexL2_3;

			internal Texture TexProbeOcclusion;

			internal Texture TexValidity;
			internal Texture TexSkyOcclusion;
			internal Texture TexSkyShadingDirectionIndices;

			internal int width;
			internal int height;
			internal int depth;

			internal void Cleanup()
			{
				CoreUtils.Destroy(TexL0_L1rx);

				CoreUtils.Destroy(TexL1_G_ry);
				CoreUtils.Destroy(TexL1_B_rz);

				CoreUtils.Destroy(TexL2_0);
				CoreUtils.Destroy(TexL2_1);
				CoreUtils.Destroy(TexL2_2);
				CoreUtils.Destroy(TexL2_3);

				CoreUtils.Destroy(TexProbeOcclusion);

				CoreUtils.Destroy(TexValidity);
				CoreUtils.Destroy(TexSkyOcclusion);
				CoreUtils.Destroy(TexSkyShadingDirectionIndices);

				TexL0_L1rx = null;

				TexL1_G_ry = null;
				TexL1_B_rz = null;

				TexL2_0 = null;
				TexL2_1 = null;
				TexL2_2 = null;
				TexL2_3 = null;

				TexProbeOcclusion = null;

				TexValidity = null;
				TexSkyOcclusion = null;
				TexSkyShadingDirectionIndices = null;
			}
		}

		internal const int kBrickCellCount = 3;
		internal const int kBrickProbeCountPerDim = kBrickCellCount + 1;
		internal const int kBrickProbeCountTotal = kBrickProbeCountPerDim * kBrickProbeCountPerDim * kBrickProbeCountPerDim;
		internal const int kChunkProbeCountPerDim = kChunkSizeInBricks * kBrickProbeCountPerDim;

		internal int estimatedVMemCost { get; private set; }

		private const int kMaxPoolWidth = 1 << 11; // 2048 texels is a d3d11 limit for tex3d in all dimensions

		internal DataLocation m_Pool; // internal to access it from blending pool only

		private BrickChunkAlloc m_NextFreeChunk;
		private Stack<BrickChunkAlloc> m_FreeList;
		private int m_AvailableChunkCount;

		private ProbeVolumeSHBands m_SHBands;
		private bool m_ContainsValidity;
		private bool m_ContainsProbeOcclusion;
		private bool m_ContainsRenderingLayer;
		private bool m_ContainsSkyOcclusion;
		private bool m_ContainsSkyShadingDirection;

		private static ComputeShader s_DataUploadCS;
		private static int s_DataUploadKernel;
		private static ComputeShader s_DataUploadL2CS;
		private static int s_DataUploadL2Kernel;
		private static LocalKeyword s_DataUpload_Shared;
		private static LocalKeyword s_DataUpload_ProbeOcclusion;
		private static LocalKeyword s_DataUpload_SkyOcclusion;
		private static LocalKeyword s_DataUpload_SkyShadingDirection;

		internal static void Initialize()
		{
			if (!SystemInfo.supportsComputeShaders)
				return;

			if(BXRenderPipeline.TryGetRenderCommonSettings(out var settings))
			{
				s_DataUploadCS = settings.probeVolumeRuntimeResources.probeVolumeUploadDataCS;
				s_DataUploadL2CS = settings.probeVolumeRuntimeResources.probeVolumeUploadDataL2CS;
			}

			if(s_DataUploadCS != null)
			{
				s_DataUploadKernel = s_DataUploadCS.FindKernel("UploadData");
				s_DataUpload_Shared = new LocalKeyword(s_DataUploadCS, "PROBE_VOLUMES_SHARED_DATA");
				s_DataUpload_ProbeOcclusion = new LocalKeyword(s_DataUploadCS, "PROBE_VOLUMES_PROBE_OCCLUSION");
				s_DataUpload_SkyOcclusion = new LocalKeyword(s_DataUploadCS, "PROBE_VOLUMES_SKY_OCCLUSION");
				s_DataUpload_SkyShadingDirection = new LocalKeyword(s_DataUploadCS, "PROBE_VOLUMES_SKY_SHADING_DIRECTION");
			}

			if(s_DataUploadL2CS != null)
			{
				s_DataUploadL2CS.FindKernel("UploadDataL2");
			}
		}

		internal Texture GetValidityTexture()
		{
			return m_Pool.TexValidity;
		}

		internal Texture GetSkyOcclusionTexture()
		{
			return m_Pool.TexSkyOcclusion;
		}

		internal Texture GetSkyShadingDirectionIndicesTexture()
		{
			return m_Pool.TexSkyShadingDirectionIndices;
		}

		internal Texture GetProbeOcclusionTexture()
		{
			return m_Pool.TexProbeOcclusion;
		}

		internal ProbeBrickPool(ProbeVolumeTextureMemoryBudget memoryBudget, ProbeVolumeSHBands shBands, bool allocateValidityData = false, bool allocateRenderingLayerData = false, bool allocateSkyOcclusion = false, bool allocateSkyShadingData = false, bool allocateProbeOcclusionData = false)
		{
			Profiler.BeginSample("Create ProbeBrickPool");
			m_NextFreeChunk.x = m_NextFreeChunk.y = m_NextFreeChunk.z = 0;

			m_SHBands = shBands;
			m_ContainsValidity = allocateValidityData;
			m_ContainsRenderingLayer = allocateRenderingLayerData;
			m_ContainsSkyOcclusion = allocateSkyOcclusion;
			m_ContainsSkyShadingDirection = allocateSkyShadingData;
			m_ContainsProbeOcclusion = allocateProbeOcclusionData;

			m_FreeList = new Stack<BrickChunkAlloc>(256);

			DerivePoolSizeFromBudget(memoryBudget, out int width, out int height, out int depth);
			AllocatePool(width, height, depth);

			m_AvailableChunkCount = (m_Pool.width / (kChunkSizeInBricks * kBrickProbeCountPerDim)) * (m_Pool.height / kBrickProbeCountPerDim) * (m_Pool.depth / kBrickProbeCountPerDim);

			Profiler.EndSample();
		}

		internal void AllocatePool(int width, int height, int depth)
		{
			m_Pool = CreateDataLocation(width * height * depth, false, m_SHBands, "APV", true,
				m_ContainsValidity, m_ContainsRenderingLayer, m_ContainsSkyOcclusion, m_ContainsSkyShadingDirection, m_ContainsProbeOcclusion,
				out int estimatedCost);

			estimatedVMemCost = estimatedCost;
		}

		public int GetRemainingChunkCount()
		{
			return m_AvailableChunkCount;
		}

		internal void EnsureTextureValidity()
		{
			// We assume that if a texture is null, all of them are. In any case we reboot them altogether.
			if(m_Pool.TexL0_L1rx == null)
			{
				m_Pool.Cleanup();
				AllocatePool(m_Pool.width, m_Pool.height, m_Pool.depth);
			}
		}

		internal bool EnsureTextureValidity(bool renderingLayers, bool skyOcclusion, bool skyDirection, bool probeOcclusion)
		{
			if(m_ContainsRenderingLayer != renderingLayers || m_ContainsSkyOcclusion != skyOcclusion || m_ContainsSkyShadingDirection != skyDirection || m_ContainsProbeOcclusion != probeOcclusion)
			{
				m_Pool.Cleanup();

				m_ContainsRenderingLayer = renderingLayers;
				m_ContainsSkyOcclusion = skyOcclusion;
				m_ContainsSkyShadingDirection = skyDirection;
				m_ContainsProbeOcclusion = probeOcclusion;

				AllocatePool(m_Pool.width, m_Pool.height, m_Pool.depth);

				return false;
			}

			return true;
		}

		private static void DerivePoolSizeFromBudget(ProbeVolumeTextureMemoryBudget memoryBudget, out int width, out int height, out int depth)
		{
			// TODO: This is fairly simplistic for now and relies on the enum to have the value set to the desired numbers,
			// might change the heuristic later on.
			width = (int)memoryBudget;
			height = (int)memoryBudget;
			depth = kBrickProbeCountPerDim;
		}

		public static DataLocation CreateDataLocation(int numProbes, bool compressed, ProbeVolumeSHBands shBands, string name, bool allocateRenderTexture,
			bool allocateValidityData, bool allocateRenderLayers, bool allocateSkyOcclusionData, bool allocateSkyShadingDirectionData, bool allocateProbeOcclusionData,
			out int allocatedBytes)
		{
			Vector3Int locSize = ProbeCountToDataLocSize(numProbes);
			int width = locSize.x;
			int height = locSize.y;
			int depth = locSize.z;

			DataLocation loc;
			var L0Format = GraphicsFormat.R16G16B16A16_SFloat;
			var L1L2Format = compressed ? GraphicsFormat.RGBA_BC7_UNorm : GraphicsFormat.R8G8B8A8_UNorm;

			var ValidityFormat = allocateRenderLayers ?
				// for 32 bits we use a float format but it's an uint
				GraphicsFormat.R32_SFloat :
				// NOTE: Platforms that do not support Sample nor LoadStore for R8_UNorm need to fallback to RGBA8_UNorm since that format should be supported for both (e.g. GLES3.x)
				SystemInfo.IsFormatSupported(GraphicsFormat.R8_UNorm, FormatUsage.Sample | FormatUsage.LoadStore) ? GraphicsFormat.R8_UNorm : GraphicsFormat.R8G8B8A8_UNorm;

			allocatedBytes = 0;
			loc.TexL0_L1rx = CreateDataTexture(width, height, depth, L0Format, $"{name}_TexL0_L1rx", allocateRenderTexture, ref allocatedBytes);
			loc.TexL1_G_ry = CreateDataTexture(width, height, depth, L1L2Format, $"{name}_TexL1_G_ry", allocateRenderTexture, ref allocatedBytes);
			loc.TexL1_B_rz = CreateDataTexture(width, height, depth, L1L2Format, $"{name}_TexL1_B_rz", allocateRenderTexture, ref allocatedBytes);
			if (shBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
			{
				loc.TexL2_0 = CreateDataTexture(width, height, depth, L1L2Format, $"{name}_TexL2_0", allocateRenderTexture, ref allocatedBytes);
				loc.TexL2_1 = CreateDataTexture(width, height, depth, L1L2Format, $"{name}_TexL2_1", allocateRenderTexture, ref allocatedBytes);
				loc.TexL2_2 = CreateDataTexture(width, height, depth, L1L2Format, $"{name}_TexL2_2", allocateRenderTexture, ref allocatedBytes);
				loc.TexL2_3 = CreateDataTexture(width, height, depth, L1L2Format, $"{name}_TexL2_3", allocateRenderTexture, ref allocatedBytes);
			}
			else
			{
				loc.TexL2_0 = null;
				loc.TexL2_1 = null;
				loc.TexL2_2 = null;
				loc.TexL2_3 = null;
			}

			if (allocateValidityData)
				loc.TexValidity = CreateDataTexture(width, height, depth, ValidityFormat, $"{name}_Validity", allocateRenderTexture, ref allocatedBytes);
			else
				loc.TexValidity = null;

			if (allocateSkyOcclusionData)
				loc.TexSkyOcclusion = CreateDataTexture(width, height, depth, GraphicsFormat.R16G16B16A16_SFloat, $"{name}_SkyOcclusion", allocateRenderTexture, ref allocatedBytes);
			else
				loc.TexSkyOcclusion = null;

			if (allocateSkyShadingDirectionData)
				loc.TexSkyShadingDirectionIndices = CreateDataTexture(width, height, depth, GraphicsFormat.R8_UNorm, $"{name}_SkyShadingDirectionIndices", allocateRenderTexture, ref allocatedBytes);
			else
				loc.TexSkyShadingDirectionIndices = null;

			if (allocateProbeOcclusionData)
				loc.TexProbeOcclusion = CreateDataTexture(width, height, depth, GraphicsFormat.R8G8B8A8_UNorm, $"{name}_ProbeOcclusion", allocateRenderTexture, ref allocatedBytes);
			else
				loc.TexProbeOcclusion = null;

			loc.width = width;
			loc.height = height;
			loc.depth = depth;

			return loc;
		}

		internal static Vector3Int ProbeCountToDataLocSize(int numProbes)
		{
			Debug.Assert(numProbes != 0);
			Debug.Assert(numProbes % kBrickProbeCountTotal == 0);

			int numBricks = numProbes / kBrickProbeCountTotal;
			int poolWidth = kMaxPoolWidth / kBrickProbeCountPerDim;

			int width, height, depth;
			depth = (numBricks + poolWidth * poolWidth - 1) / (poolWidth * poolWidth);
			if (depth > 1)
				width = height = poolWidth;
			else
			{
				height = (numBricks + poolWidth - 1) / poolWidth;
				if (height > 1)
					width = poolWidth;
				else
					width = numBricks;
			}

			width *= kBrickProbeCountPerDim;
			height *= kBrickProbeCountPerDim;
			depth *= kBrickProbeCountPerDim;

			return new Vector3Int(width, height, depth);
		}

		public static Texture CreateDataTexture(int width, int height, int depth, GraphicsFormat format, string name, bool allocateRenderTexture, ref int allocatedBytes)
		{
			allocatedBytes += EstimateMemoryCost(width, height, depth, format);

			Texture texture;
			if (allocateRenderTexture)
			{
				texture = new RenderTexture(new RenderTextureDescriptor()
				{
					width = width,
					height = height,
					volumeDepth = depth,
					graphicsFormat = format,
					mipCount = 1,
					enableRandomWrite = SystemInfo.supportsComputeShaders,
					dimension = TextureDimension.Tex3D,
					msaaSamples = 1
				});
			}
			else
			{
				texture = new Texture3D(width, height, depth, format, TextureCreationFlags.None, 1);
			}

			texture.hideFlags = HideFlags.HideAndDontSave;
			texture.name = name;

			if (allocateRenderTexture)
				(texture as RenderTexture).Create();

			return texture;
		}

		private static int EstimateMemoryCost(int width, int height, int depth, GraphicsFormat format)
		{
			int elementSize = format == GraphicsFormat.R16G16B16A16_SFloat ? 8 : format == GraphicsFormat.R8G8B8A8_UNorm ? 4 : 1;

			return (width * height * depth) * elementSize;
		}

		internal static int GetChunkSizeInBrick()
		{
			return kChunkSizeInBricks;
		}

		internal static int GetChunkSizeInProbe()
		{
			return kChunkSizeInBricks * kBrickProbeCountTotal;
		}

		internal int GetPoolWidth() { return m_Pool.width; }
		internal int GetPoolHeight() { return m_Pool.height; }
		internal Vector3Int GetPoolDimensions() { return new Vector3Int(m_Pool.width, m_Pool.height, m_Pool.depth); }

		internal void GetRuntimeResources(ref ProbeReferenceVolume.RuntimeResources rr)
		{
			rr.L0_L1rx = m_Pool.TexL0_L1rx as RenderTexture;

			rr.L1_G_ry = m_Pool.TexL1_G_ry as RenderTexture;
			rr.L1_B_rz = m_Pool.TexL1_B_rz as RenderTexture;

			rr.L2_0 = m_Pool.TexL2_0 as RenderTexture;
			rr.L2_1 = m_Pool.TexL2_1 as RenderTexture;
			rr.L2_2 = m_Pool.TexL2_2 as RenderTexture;
			rr.L2_3 = m_Pool.TexL2_3 as RenderTexture;

			rr.ProbeOcclusion = m_Pool.TexProbeOcclusion as RenderTexture;

			rr.Validity = m_Pool.TexValidity as RenderTexture;
			rr.SkyOcclusionL0L1 = m_Pool.TexSkyOcclusion as RenderTexture;
			rr.SkyShadingDirectionIndices = m_Pool.TexSkyShadingDirectionIndices as RenderTexture;
		}
	}
}