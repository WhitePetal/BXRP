#pragma once

#include <DescriptorAllocation.h>

#include "d3dx12.h"

#include <cstdint>
#include <mutex>
#include <memory>
#include <set>
#include <vector>

class DescriptorAllocatorPage;

class DescriptorAllocator
{
public:
	DescriptorAllocator(D3D12_DESCRIPTOR_HEAP_TYPE type, uint32_t numDescriptorsPerHeap = 256);
	virtual ~DescriptorAllocator();

	/// <summary>
	/// Allocate a number of contiguous descriptors from a GPU visible descriptor heap
	/// </summary>
	/// <param name="numDescriptors">
	/// The number of contiguous descriptors to allocate
	/// can't be more than the number of descriptor per descriptor heap
	/// </param>
	/// <returns></returns>
	DescriptorAllocation Allocate(uint32_t numDescriptors = 1);

	/// <summary>
	/// When the frame has completed, the stale descriptors can be released
	/// </summary>
	/// <param name="frameNumber"></param>
	void ReleaseStaleDescriptors(uint64_t frameNumber);

private:
	using DescriptorHeapPool = std::vector<std::shared_ptr<DescriptorAllocatorPage>>;

	/// <summary>
	/// Create a new heap with a specific number of descriptors
	/// </summary>
	/// <returns></returns>
	std::shared_ptr<DescriptorAllocatorPage> CreateAllocatorPage();

	D3D12_DESCRIPTOR_HEAP_TYPE m_HeapType;
	uint32_t m_NumDescriptorsPerHeap;

	DescriptorHeapPool m_HeapPool;

	/// <summary>
	/// Indices of the available heaps in the heap pool
	/// </summary>
	std::set<size_t> m_AvailableHeaps;

	std::mutex m_AllocationMutex;
};