#pragma once
#include <DescriptorAllocation.h>

#include <d3d12.h>
#include <d3dx12.h>

#include <wrl.h>

#include <map>
#include <memory>
#include <mutex>
#include <queue>

class DescriptorAllocatorPage : public std::enable_shared_from_this<DescriptorAllocatorPage>
{
public:
	DescriptorAllocatorPage(D3D12_DESCRIPTOR_HEAP_TYPE type, uint32_t numDescriptors);
	~DescriptorAllocatorPage();

	D3D12_DESCRIPTOR_HEAP_TYPE GetHeapType() const;

	/// <summary>
	/// Check to see if this descriptor page has a contiguous block of rescriptors
	/// large enough to satisfy the request
	/// </summary>
	/// <param name="numDescriptors"></param>
	/// <returns></returns>
	bool HasSpace(uint32_t numDescriptors) const;

	/// <summary>
	/// Get the number of available handles in the heap
	/// </summary>
	/// <returns></returns>
	uint32_t NumFreeHandles() const;

	/// <summary>
	/// Allocate a number of descriptors from this descriptor heap.
	/// If the allocation can't be satisfied, then a null descriptor is returned
	/// </summary>
	/// <param name="numDescriptors"></param>
	/// <returns></returns>
	DescriptorAllocation Allocate(uint32_t numDescriptors);

	/// <summary>
	/// Return a descriptor back to the heap
	/// </summary>
	/// <param name="descriptorHandle"></param>
	/// <param name="frameNumber">
	/// Stale descriptors are not freed directly, but put on a stale allocations queue.
	/// Stale allocations are returned to the heap using the 
	/// DescriptorAllocatorPage::ReleaseStaleAllocations method
	/// </param>
	void Free(DescriptorAllocation&& descriptorHandle, uint64_t frameNumber);

	/// <summary>
	/// Returned the stale descriptors back to the descriptor heap
	/// </summary>
	/// <param name="frameNumber"></param>
	void ReleaseStaleDescriptors(uint64_t frameNumber);

protected:
	/// <summary>
	/// Compute the offset of the descriptor handle from the start of the heap
	/// </summary>
	/// <param name="handle"></param>
	/// <returns></returns>
	uint32_t ComputeOffset(D3D12_CPU_DESCRIPTOR_HANDLE handle);

	/// <summary>
	/// Adds a new block to the free list
	/// </summary>
	/// <param name="offset"></param>
	/// <param name="numDescriptors"></param>
	void AddNewBlock(uint32_t offset, uint32_t numDescriptors);

	/// <summary>
	/// Free a block of descriptors
	/// This will also merge free blocks in the free list to from larger blocks that
	/// can be reused
	/// </summary>
	/// <param name="offset"></param>
	/// <param name="numDescriptors"></param>
	void FreeBlock(uint32_t offset, uint32_t numDescriptors);

	// The offset (in descriptors) within the decriptor heap
	using OffsetType = uint32_t;
	// The number of descriptors that are available
	using SizeType = uint32_t;

	struct FreeBlockInfo;
	/// <summary>
	/// A map that lists the free blocks by the offset within the descriptor heap
	/// </summary>
	using FreeListByOffset = std::map<OffsetType, FreeBlockInfo>;
	/// <summary>
	/// A map that lists the free blocks by size
	/// Needs to be a multimap since multiple blocks can have the same size
	/// </summary>
	using FreeListBySize = std::multimap<SizeType, FreeListByOffset::iterator>;

	struct FreeBlockInfo
	{
		FreeBlockInfo(SizeType size)
			: Size(size)
		{

		}

		SizeType Size;
		FreeListBySize::iterator FreeListBySizeIt;
	};

	struct StaleDescriptorInfo
	{
		StaleDescriptorInfo(OffsetType offset, SizeType size, uint64_t frame)
			: Offset(offset)
			, Size(size)
			, FrameNumber(frame)
		{

		}

		/// <summary>
		/// The offset within the descriptor heap
		/// </summary>
		OffsetType Offset;
		/// <summary>
		/// The number of descriptors
		/// </summary>
		SizeType Size;
		/// <summary>
		/// The frame number that the descriptor was freed
		/// </summary>
		uint64_t FrameNumber;
	};

	using StaleDescriptorQueue = std::queue<StaleDescriptorInfo>;

	FreeListByOffset m_FreeListByOffset;
	FreeListBySize m_FreeListBySize;
	StaleDescriptorQueue m_StaleDescriptors;

	Microsoft::WRL::ComPtr<ID3D12DescriptorHeap> m_DescriptorHeap;
	D3D12_DESCRIPTOR_HEAP_TYPE m_HeapType;
	CD3DX12_CPU_DESCRIPTOR_HANDLE m_BaseDescriptor;
	uint32_t m_DescriptorHandleIncrementSize;
	uint32_t m_NumDescriptorsInHeap;
	uint32_t m_NumFreeHandles;

	std::mutex m_AllocationMutex;
};