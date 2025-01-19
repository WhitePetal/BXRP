using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;

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
	}
}