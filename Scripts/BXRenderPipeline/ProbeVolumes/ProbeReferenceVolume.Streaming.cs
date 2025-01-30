using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace BXRenderPipeline
{
	using Chunk = ProbeBrickPool.BrickChunkAlloc;

	public partial class ProbeReferenceVolume
	{
		internal class DiskStreamingRequest
		{
			private ReadHandle m_ReadHandle;
			private ReadCommandArray m_ReadCommandArray = new ReadCommandArray();
		 	private NativeArray<ReadCommand> m_ReadCommandBuffer;
			private int m_BytesWritten;

			public DiskStreamingRequest(int maxRequestCount)
			{
				m_ReadCommandBuffer = new NativeArray<ReadCommand>(maxRequestCount, Allocator.Persistent);
			}

			public unsafe void AddReadCommand(int offset, int size, byte* dest)
			{
				Debug.Assert(m_ReadCommandArray.CommandCount < m_ReadCommandBuffer.Length);

				m_ReadCommandBuffer[m_ReadCommandArray.CommandCount++] = new ReadCommand()
				{
					Buffer = dest,
					Offset = offset,
					Size = size
				};
				m_BytesWritten += size;
			}

			public unsafe int RunCommands(FileHandle file)
			{
				m_ReadCommandArray.ReadCommands = (ReadCommand*)m_ReadCommandBuffer.GetUnsafePtr();
				m_ReadHandle = AsyncReadManager.Read(file, m_ReadCommandArray);

				return m_BytesWritten;
			}

			public void Clear()
			{
				if (m_ReadHandle.IsValid())
					m_ReadHandle.JobHandle.Complete();
				m_ReadHandle = default;
				m_ReadCommandArray.CommandCount = 0;
				m_BytesWritten = 0;
			}

			public void Cancle()
			{
				if (m_ReadHandle.IsValid())
					m_ReadHandle.Cancel();
			}

			public void Wait()
			{
				if (m_ReadHandle.IsValid())
					m_ReadHandle.JobHandle.Complete();
			}

			public void Dispose()
			{
				m_ReadCommandBuffer.Dispose();
			}

			public ReadStatus GetStatus()
			{
				return m_ReadHandle.IsValid() ? m_ReadHandle.Status : ReadStatus.Complete;
			}
		}

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
			public int chunkSize { get; }
			public int chunkCount { get; }

			public CellStreamingScratchBuffer(int chunkCount, int chunkSize, bool allocatedGraphicsBuffers)
			{
				this.chunkCount = chunkCount;
				this.chunkSize = chunkSize;

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

			public ProbeVolumeBakingSet.PerScenarioDataInfo scenarioData { get; set; }
			public int poolIndex { get; set; }
			public bool streamSharedData { get; set; }

			public delegate void OnStreamingCompeleteDelegate(CellStreamingRequest request, CommandBuffer cmd);
			public OnStreamingCompeleteDelegate onStreamingCompelete = null;

			public DiskStreamingRequest cellDataStreamingRequest = new DiskStreamingRequest(1);
			public DiskStreamingRequest cellOptionalDataStreamingRequest = new DiskStreamingRequest(1);
			public DiskStreamingRequest cellSharedDataStreamingRequest = new DiskStreamingRequest(1);
			public DiskStreamingRequest cellProbeOcclusionDataStreamingRequest = new DiskStreamingRequest(1);
			public DiskStreamingRequest brickStreamingRequest = new DiskStreamingRequest(1);
			public DiskStreamingRequest supportStreamingRequest = new DiskStreamingRequest(1);

			public int bytesWritten;

			public bool IsStreaming()
			{
				return state == State.Pending || state == State.Active;
			}

			public void Cancel()
			{
				if (state == State.Active) 
				{
					brickStreamingRequest.Cancle();
					supportStreamingRequest.Cancle();
					cellDataStreamingRequest.Cancle();
					cellOptionalDataStreamingRequest.Cancle();
					cellSharedDataStreamingRequest.Cancle();
					cellProbeOcclusionDataStreamingRequest.Cancle();
				}

				state = State.Canceled;
			}

			public void WaitAll()
			{
				if (state == State.Active)
				{
					brickStreamingRequest.Wait();
					supportStreamingRequest.Wait();
					cellDataStreamingRequest.Wait();
					cellOptionalDataStreamingRequest.Wait();
					cellSharedDataStreamingRequest.Wait();
					cellProbeOcclusionDataStreamingRequest.Wait();
				}
			}

			public bool UpdateRequestState(DiskStreamingRequest request, ref bool isCompelete)
			{
				var status = request.GetStatus();
				if (status == ReadStatus.Failed)
					return false;

				isCompelete &= request.GetStatus() == ReadStatus.Complete;
				return true;
			}

			public void UpdateState()
			{
				if(state == State.Active)
				{
					bool isCompelte = true;
					bool success = UpdateRequestState(brickStreamingRequest, ref isCompelte);
					success &= UpdateRequestState(supportStreamingRequest, ref isCompelte);
					success &= UpdateRequestState(cellDataStreamingRequest, ref isCompelte);
					success &= UpdateRequestState(cellOptionalDataStreamingRequest, ref isCompelte);
					success &= UpdateRequestState(cellSharedDataStreamingRequest, ref isCompelte);
					success &= UpdateRequestState(cellProbeOcclusionDataStreamingRequest, ref isCompelte);

					if (!success)
					{
						Cancel(); // At least one of the requests failed. Cancel the others.
						state = State.Invalid;
					}
					else if (isCompelte)
					{
						state = State.Complete;
					}
				}
			}

			public void Clear()
			{
				cell = null;
				Reset();
			}

			public void Reset()
			{
				state = State.Pending;
				scratchBuffer = null;
				brickStreamingRequest.Clear();
				supportStreamingRequest.Clear();
				cellDataStreamingRequest.Clear();
				cellOptionalDataStreamingRequest.Clear();
				cellSharedDataStreamingRequest.Clear();
				cellProbeOcclusionDataStreamingRequest.Clear();
				bytesWritten = 0;
			}

			public void Dispose()
			{
				brickStreamingRequest.Dispose();
				supportStreamingRequest.Dispose();
				cellDataStreamingRequest.Dispose();
				cellOptionalDataStreamingRequest.Dispose();
				cellSharedDataStreamingRequest.Dispose();
				cellProbeOcclusionDataStreamingRequest.Dispose();
			}
		}

#if UNITY_EDITOR
		// By default on editor we load a lot of cells in one go to avoid having to mess with scene view
		// to see results, this value can still be changed via API.
		private bool m_LoadMaxCellsPerFrame = true;
#else
		private bool m_LoadMaxCellsPerFrame = false;
#endif

		/// <summary>
		/// Enable streaming as many cells per frame as possible.
		/// </summary>
		/// <param name="value">True to enable streaming as many cells per frame as possible.</param>
		public void EnableMaxCellStreaming(bool value)
		{
			m_LoadMaxCellsPerFrame = value;
		}

		private const int kMaxCellLoadedPerFrame = 10;
		private int m_NumberOfCellsLoadedPerFrame = 1;

		/// <summary>
		/// Set the number of cells that are loaded per frame when needed. This number is capped at 10.
		/// </summary>
		/// <param name="numberOfCells"></param>
		public void SetNumberOfCellsLoadedPerFrame(int numberOfCells)
		{
			m_NumberOfCellsLoadedPerFrame = Mathf.Min(kMaxCellLoadedPerFrame, Mathf.Max(1, numberOfCells));
		}

		public bool loadMaxCellsPerFrame
		{
			get => m_LoadMaxCellsPerFrame;
			set => m_LoadMaxCellsPerFrame = value;
		}

		private int m_NumberOfCellsBlendedPerFrame = 10000;
		/// <summary>
		/// Maximum number of cells that are blended per frame
		/// </summary>
		public int numberOfCellsBlendedPerFram
		{
			get => m_NumberOfCellsBlendedPerFrame;
			set => m_NumberOfCellsBlendedPerFrame = Mathf.Max(1, value);
		}

		private float m_TurnoverRate = 0.1f;
		/// <summary>
		/// Percentage of cells loaded in the blending pool that can be replaced by out of date cells
		/// </summary>
		public float turnoverRate
		{
			get => m_TurnoverRate;
			set => m_TurnoverRate = Mathf.Clamp01(value);
		}

		private DynamicArray<Cell> m_LoadedCells = new(); // List of currently loaded cells
		private DynamicArray<Cell> m_ToBeLoadedCells = new(); // List of currently unloaded cells
		private DynamicArray<Cell> m_WorseLoadedCells = new(); // Reduced list (N cells are processed per frame) of worse loaded cells.
		private DynamicArray<Cell> m_BestToBeLoadedCells = new();  // Reduced list (N cells are processed per frame) of best unloaded cells.
		private DynamicArray<Cell> m_TempCellToLoadList = new(); // Temp list of cells loaded during this frame.
		private DynamicArray<Cell> m_TempCellToUnloadList = new(); // Temp list of cells unloaded during this frame.

		private DynamicArray<Cell> m_LoadedBlendingCells = new();
		private DynamicArray<Cell> m_ToBeLoadedBlendingCells = new();
		private DynamicArray<Cell> m_TempBlendingCellToLoadList = new();
		private DynamicArray<Cell> m_TempBlendingCellToUnloadList = new();

		private Vector3 m_FrozenCameraPosition;
		private Vector3 m_FrozenCameraDirection;

		private const float kIndexFragmentationThreshold = 0.2f;
		private bool m_IndexDefragmentationInProgress;
		private ProbeBrickIndex m_DefragIndex;
		private ProbeGlobalIndirection m_DefragCellIndices;

		private DynamicArray<Cell> m_IndexDefragCells = new ();
		private DynamicArray<Cell> m_TempIndexDefragCells = new();

		internal float minStreamingScore;
		internal float maxStreamingScore;

		// Requests waiting to be run. Needed to preserve order of requests.
		private Queue<CellStreamingRequest> m_StreamingQueue = new Queue<CellStreamingRequest>();

		// List of active requests. Needed to query the result every frame.
		private List<CellStreamingRequest> m_ActiveStreamingRequests = new List<CellStreamingRequest>();

		private ObjectPool<CellStreamingRequest> m_StreamingRequestsPool = new ObjectPool<CellStreamingRequest>(null, (val) => val.Clear());

		private bool m_DiskStreamingUseCompute = false;

		private ProbeVolumeScratchBufferPool m_ScratchBufferPool;

		private CellStreamingRequest.OnStreamingCompeleteDelegate m_OnStreamingComplete;
		private CellStreamingRequest.OnStreamingCompeleteDelegate m_OnBlendingStreamingComplete;

		public bool probeOcclusion;
		public bool skyOcclusion;
		public bool skyOcclusionShadingDirection;

		private List<Chunk> m_TmpSrcChunks = new List<Chunk>();

		private void InitStreaming()
		{
			m_OnStreamingComplete = OnStreamingComplete;
			m_OnBlendingStreamingComplete = OnBlendingStreamingComplete;
		}

		private void OnStreamingComplete(CellStreamingRequest request, CommandBuffer cmd)
		{
			request.cell.streamingInfo.request = null;
			UpdatePoolAndIndex(request.cell, request.scratchBuffer, request.scratchBufferLayout, request.poolIndex, cmd);
		}

		private void OnBlendingStreamingComplete(CellStreamingRequest request, CommandBuffer cmd)
		{
			UpdatePool(cmd, request.cell.blendingInfo.chunkList, request.scratchBuffer, request.scratchBufferLayout, request.poolIndex);

			if (request.poolIndex == 0)
				request.cell.streamingInfo.blendingRequest0 = null;
			else
				request.cell.streamingInfo.blendingRequest1 = null;

			// Streaming of both scenario is over, we can update the index and start blending.
			if (request.cell.streamingInfo.blendingRequest0 == null && request.cell.streamingInfo.blendingRequest1 == null
				&& !request.cell.indexInfo.indexUpdated)
				UpdateCellIndex(request.cell);
		}

		private void PushDiskStreamingRequest(Cell cell, string scenario, int poolIndex, CellStreamingRequest.OnStreamingCompeleteDelegate onStreamingCompelete)
		{
			var streamingRequest = m_StreamingRequestsPool.Get();
			streamingRequest.cell = cell;
			streamingRequest.state = CellStreamingRequest.State.Pending;
			streamingRequest.scenarioData = m_CurrentBakingSet.scenarios[scenario];
			streamingRequest.poolIndex = poolIndex;
			streamingRequest.onStreamingCompelete = onStreamingCompelete;

			// Only stream shared data for a regular streaming request (index -1 : no streaming)
			// or the first scenario of the two blending scenarios (index 0)
			if (poolIndex == -1 || poolIndex == 0)
				streamingRequest.streamSharedData = true;

			if (probeVolumeDebug.verboseStreamingLog)
			{
				if (poolIndex == -1)
					LogStreaming($"Push streaming request for cell {cell.desc.index}.");
				else
					LogStreaming($"Push streaming request for blending cell {cell.desc.index}.");
			}

			switch (poolIndex)
			{
				case -1:
					cell.streamingInfo.request = streamingRequest;
					break;
				case 0:
					cell.streamingInfo.blendingRequest0 = streamingRequest;
					break;
				case 1:
					cell.streamingInfo.blendingRequest1 = streamingRequest;
					break;
			}

			// Enqueue request.
			m_StreamingQueue.Enqueue(streamingRequest);
		}

		private void CancelStreamingRequest(Cell cell)
		{
			m_Index.RemoveBricks(cell.indexInfo);
		}

		private int numberOfCellsLoadedPerFrame => m_LoadMaxCellsPerFrame ? cells.Count : m_NumberOfCellsLoadedPerFrame;


		private bool HasActiveStreamingRequest(Cell cell)
		{
			return diskStreamingEnabled && m_ActiveStreamingRequests.Exists(x => x.cell == cell);
		}

		[Conditional("UNITY_EDITOR")]
		[Conditional("DEVELOPMENT_BUILD")]
		private void LogStreaming(string log)
		{
			Debug.Log(log);
		}
	}
}
