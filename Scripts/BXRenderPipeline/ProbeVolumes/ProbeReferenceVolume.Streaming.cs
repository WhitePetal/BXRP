using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace BXRenderPipeline
{
	public partial class ProbeReferenceVolume
	{
		[GenerateHLSL(needAccessors = false, generateCBuffer = true)]
		internal struct CellStreamingScratchBufferLayout
		{
			public int _SharedDestChunksOffset;
			public int _L0L1rxOffset;
			public int _L1GryOffset;
			public int _L1BrzOffset;
			public int _ValidityOffset;
			public int _ProbeOcclusionOffset;
			public int _SkyOcclusionOffset;
			public int _SkyShadingDirectionOffset;
			public int _L2_0Offset;
			public int _L2_1Offset;
			public int _L2_2Offset;
			public int _L2_3Offset;

			public int _L0Size;
			public int _L0ProbeSize; // In bytes
			public int _L1Size;
			public int _L1ProbeSize; // In bytes
			public int _ValiditySize;
			public int _ValidityProbeSize; // In bytes
			public int _ProbeOcclusionSize;
			public int _ProbeOcclusionProbeSize; // In bytes
			public int _SkyOcclusionSize;
			public int _SkyOcclusionProbeSize; // In bytes
			public int _SkyShadingDirectionSize;
			public int _SkyShadingDirectionProbeSize; // In bytes
			public int _L2Size;
			public int _L2ProbeSize; // In bytes

			public int _ProbeCountInChunkLine;
			public int _ProbeCountInChunkSlice;
		}

		internal class CellStreamingScratchBuffer
		{
			private int m_CurrentBuffer;
			private GraphicsBuffer[] m_GraphicsBuffers;

			// The GraphicsBUffer is double buffer because the data upload shader might still be running
			// when we start a new streaming request
			// We could have double buffered at the CellStreamingScratchBuffer level itself but it would consume more memory (native+graphics buffer x2)
			public GraphicsBuffer buffer => m_GraphicsBuffers[m_CurrentBuffer];
			public NativeArray<byte> stagingBuffer; // Contains data streamed from disk. To be copied into the graphics buffer
			public int chunkCount { get; }

			public CellStreamingScratchBuffer(int chunkCount, int chunkSize, bool allocatedGraphicsBuffers)
			{
				this.chunkCount = chunkCount;

				// With a stride of 4 (one uint)
				// Number of elements for chunk data: chunkCount * chunkSize / 4
				// Number of elements for dest chunk data (Vector4Int) : chunkCount * 4 * 4 / 4
				var bufferSize = chunkCount * chunkSize / 4 + chunkCount * 4;

				// Account for additional padding needed
				bufferSize += 2 * chunkCount * sizeof(uint);

				if (allocatedGraphicsBuffers)
				{
					for (int i = 0; i < 2; ++i)
						m_GraphicsBuffers[i] = new GraphicsBuffer(GraphicsBuffer.Target.Raw, GraphicsBuffer.UsageFlags.LockBufferForWrite, bufferSize, sizeof(uint));
				}

				m_CurrentBuffer = 0;

				stagingBuffer = new NativeArray<byte>(bufferSize * sizeof(uint), Allocator.Persistent);
			}

			public void Swap()
			{
				m_CurrentBuffer = (m_CurrentBuffer + 1) % 2;
			}

			public void Dispose()
			{
				for (int i = 0; i < 2; ++i)
					m_GraphicsBuffers[i].Dispose();
				stagingBuffer.Dispose();
			}
		}

		[DebuggerDisplay("Index = {cell.desc.index} State = {state}")]
		internal class CellStreamingRequest
		{
			public enum State
			{
				Pending,
				Active,
				Canceled,
				Invalid,
				Complete
			}

			public Cell cell { get; set; }
			public State state { get; set; }
			public CellStreamingScratchBuffer scratchBuffer { get; set; }
			public CellStreamingScratchBufferLayout scratchBufferLayout { get; set; }

			//public ProbeVolumeBakingSet
		}

#if UNITY_EDITOR
		// By default on editor we load a lot of cells in one go to avoid having to mess with scene view
		// to see results, this value can still be changed via API.
		private bool m_LoadMaxCellsPerFrame = true;
#else
		private bool m_LoadMaxCellsPerFrame = false;
#endif

		private int m_NumberOfCellsBlendedPerFrame = 10000;

		public bool loadMaxCellsPerFrame
        {
			get => m_LoadMaxCellsPerFrame;
			set => m_LoadMaxCellsPerFrame = value;
        }

		private const int kMaxCellLoadedPerFrame = 10;
		private int m_NumberOfCellsLoadedPerFrame = 1;
		private float m_TurnoverRate = 0.1f;

		private int numberOfCellsLoadedPerFrame => m_LoadMaxCellsPerFrame ? cells.Count : m_NumberOfCellsLoadedPerFrame;

		/// <summary>
		/// Maximum number of cells that are blended per frame
		/// </summary>
		public int numberOfCellsBlendedPerFram
		{
			get => m_NumberOfCellsBlendedPerFrame;
			set => m_NumberOfCellsBlendedPerFrame = Mathf.Max(1, value);
		}

		/// <summary>
		/// Percentage of cells loaded in the blending pool that can be replaced by out of date cells
		/// </summary>
		public float turnoverRate
		{
			get => m_TurnoverRate;
			set => m_TurnoverRate = Mathf.Clamp01(value);
		}

		/// <summary>
		/// Set the number of cells that are loaded per frame when needed. This number is capped at 10.
		/// </summary>
		/// <param name="numberOfCells"></param>
		public void SetNumberOfCellsLoadedPerFrame(int numberOfCells)
        {
			m_NumberOfCellsLoadedPerFrame = Mathf.Min(kMaxCellLoadedPerFrame, Mathf.Max(1, numberOfCells));
        }
	}
}
