#pragma once
#include "d3dx12.h"

#include <wrl.h>

#include <cstdint>
#include <memory>
#include <queue>
#include <functional>

using namespace Microsoft::WRL;

class CommandList;
class RootSignature;

class DynamicDescriptorHeap
{
public:
	DynamicDescriptorHeap(D3D12_DESCRIPTOR_HEAP_TYPE heapType, uint32_t numDescriptorsPerHeap = 1024);

	virtual ~DynamicDescriptorHeap();

	/// <summary>
	/// Stages a contiguous range of CPU visible descriptors.
	/// Descriptors are not copied to the GPU visible descriptor heap until
	/// the CommitStagedDescriptors function is called
	/// </summary>
	/// <param name="rootParameterIndex"></param>
	/// <param name="offset"></param>
	/// <param name="numDescriptors"></param>
	/// <param name="srcDescriptors"></param>
	void StageDescriptors(uint32_t rootParameterIndex, uint32_t offset, uint32_t numDescriptors, const
		D3D12_CPU_DESCRIPTOR_HANDLE srcDescriptors);

	/// <summary>
	/// Copy all of the staged descriptors to the GPU visible descriptor heap and 
	/// bind the descriptor heap and the descriptor tables to the command list.
	/// The passed-in function object is used to set the GPU visible descriptors 
	/// on the command list. Two possible functions are:
	///		* Before a draw: ID3D12GraphicsCommandList::SetGraphicsRootDescriptorTable
	///		* Before a dispatch: ID3D12GraphicsCommandList::SetComputeRootDescriptorTable
	/// Since the DynamicDescriptorHeap can't know which function will be used, it must 
	/// be passed as an argument to the function
	/// </summary>
	/// <param name="commandList"></param>
	/// <param name="setFunc"></param>
	void CommitStagedDescriptors(CommandList& commandList, std::function<void(ID3D12GraphicsCommandList*, UINT, D3D12_GPU_DESCRIPTOR_HANDLE)> setFunc);
	void CommitStagedDescriptorsForDraw(CommandList& commandList);
	void CommitStagedDescriptorsForDispatch(CommandList& commandList);

	/// <summary>
	/// Copies a single CPU visible descriptor to a GPU cisible descriptor heap.
	/// This is useful for the 
	///		* ID3D12GraphicsCommandList::ClearUnorderedAccessViewFloat
	///		* ID3D12GraphicsCommandList::ClearUnorderedAccessViewUint
	/// methods which require both a CPU and GPU visible descriptors for a UAV resource.
	/// </summary>
	/// <param name="commandList">The command list is required in case the GPU visible descriptor heap needs to be updated on the command list</param>
	/// <param name="cpuDescriptor">The CPU descriptor to copy into a GPU visible descriptor heap</param>
	/// <returns>The GPU visible descriptor</returns>
	D3D12_GPU_DESCRIPTOR_HANDLE CopyDescriptor(CommandList& commandList, D3D12_CPU_DESCRIPTOR_HANDLE cpuDescriptor);

	/// <summary>
	/// Parse the root signature to determine which root parameters contain 
	/// descriptor tables and determine the number of descriptors needed for 
	/// each table
	/// </summary>
	/// <param name="rootSignature"></param>
	void ParseRootSignature(const RootSignature& rootSignature);

	/// <summary>
	/// Reset used descriptors. This should only be done if any descriptors 
	/// that are being referenced by a command list has finished executing on the 
	/// command queue
	/// </summary>
	void Reset();

private:
	// Request a descriptor heap if one is available.
	ComPtr<ID3D12DescriptorHeap> RequestDescriptorHeap();
	// Create a new descriptor heap of no descriptor heap is available.
	ComPtr<ID3D12DescriptorHeap> CreateDescriptorHeap();

	// Compute the number of stale descriptors that need to be copied 
	// to GPU visible descriptor heap.
	uint32_t ComputeStaleDescriptorCount() const;

	/// <summary>
	/// The maximum number of descriptor tables per root signature.
	/// A 32-bit mask is used to keep track of the root parameter indices that 
	/// are descriptor tables.
	/// </summary>
	static const uint32_t MaxDescriptorTables = 32;

	/// <summary>
	/// A structure that represents a descriptor table entry in the root signature.
	/// </summary>
	struct DescriptorTableCache
	{
		DescriptorTableCache()
			: NumDescriptors(0)
			, BaseDescriptor(nullptr)
		{ }

		// The number of descriptors in this descriptor table
		uint32_t NumDescriptors;
		// The pointer to the descriptor in the descriptor handle cache
		D3D12_CPU_DESCRIPTOR_HANDLE* BaseDescriptor;
	};

	/// <summary>
	/// Describes the type of descriptors that can be stager using this 
	/// dynamic descriptor heap.
	/// Valid values are:
	///		* D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV
	///		* D3D12_DESCRIPTOR_HEAP_TYPE_SAMPLER
	/// This parameter also determines the type of GPU visible descriptor heap to create.
	/// </summary>
	D3D12_DESCRIPTOR_HEAP_TYPE m_DescriptorHeapType;
	/// <summary>
	/// The number of descriptors to allocate in new GPU visible descriptor heaps.
	/// </summary>
	uint32_t m_NumDescriptorsPerHeap;
	/// <summary>
	/// The increment size of a descriptor.
	/// </summary>
	uint32_t m_DescriptorHandleIncrementSize;
	/// <summary>
	/// The descriptor handle cache
	/// </summary>
	std::unique_ptr<D3D12_CPU_DESCRIPTOR_HANDLE[]> m_DescriptorHandleCache;
	/// <summary>
	/// Descriptor handle cache per descriptor table
	/// </summary>
	DescriptorTableCache m_DescriptorTableCache[MaxDescriptorTables];
	/// <summary>
	/// Each bit in the bit mask represents the index in the root signautre 
	/// that contains a descriptor table.
	/// </summary>
	uint32_t m_DescriptorTableBitMask;
	/// <summary>
	/// Each bit set in the bit mask represents a descriptor table 
	/// in the root signature that has changed since the last time the 
	/// descriptors were copied.
	/// </summary>
	uint32_t m_StaleDescriptorTableBitMask;

	using DescriptorHeapPool = std::queue<ComPtr<ID3D12DescriptorHeap>>;

	DescriptorHeapPool m_DescriptorHeapPool;
	DescriptorHeapPool m_AvailableDescriptorHeaps;

	ComPtr<ID3D12DescriptorHeap> m_CurrentDescriptorHeap;
	CD3DX12_GPU_DESCRIPTOR_HANDLE m_CurrentGPUDescriptorHandle;
	CD3DX12_CPU_DESCRIPTOR_HANDLE m_CurrentCPUDescriptorHandle;

	uint32_t m_NumFreeHandles;
};