#pragma once

#include <d3d12.h>

#include <cstdint>
#include <memory>

class DescriptorAllocatorPage;

class DescriptorAllocation
{
public:
	/// <summary>
	/// Creates a NULL descriptor
	/// </summary>
	DescriptorAllocation();

	DescriptorAllocation(D3D12_CPU_DESCRIPTOR_HANDLE descriptor, uint32_t numHandles, uint32_t descriptorSize, std::shared_ptr<DescriptorAllocatorPage> page);

	/// The descriptor will automatically free the allocation
	~DescriptorAllocation();

	// Copies are not allowed
	DescriptorAllocation(const DescriptorAllocation&) = delete;
	DescriptorAllocation& operator=(const DescriptorAllocation&) = delete;

	// Move is allowed
	DescriptorAllocation(DescriptorAllocation&& allocation);
	DescriptorAllocation& operator=(DescriptorAllocation&& other);

	/// <summary>
	/// Check if this a valid descriptor
	/// </summary>
	/// <returns></returns>
	bool IsNull() const;

	/// <summary>
	/// Get a descriptor at a particular offset in the allocation
	/// </summary>
	/// <param name="offset"></param>
	/// <returns></returns>
	D3D12_CPU_DESCRIPTOR_HANDLE GetDescriptorHandle(uint32_t offset = 0) const;

	/// <summary>
	/// Get the number of (consecutive) handles for this allocation
	/// </summary>
	/// <returns></returns>
	uint32_t GetNumHandles() const;

	/// <summary>
	/// Get the heap that this allocation came from
	/// </summary>
	/// <returns></returns>
	std::shared_ptr<DescriptorAllocatorPage> GetDescriptorAllocatorPage() const;

private:
	/// <summary>
	/// Free the descriptor back to the heap it came from
	/// </summary>
	void Free();

	/// <summary>
	/// The base descriptor
	/// </summary>
	D3D12_CPU_DESCRIPTOR_HANDLE m_Descriptor;
	/// <summary>
	/// The number of descriptors in the allocation
	/// </summary>
	uint32_t m_NumHandles;
	/// <summary>
	/// The offset to the next descriptor
	/// </summary>
	uint32_t m_DescriptorSize;

	/// <summary>
	/// A pointer back to the original page where this allocation came from
	/// </summary>
	std::shared_ptr<DescriptorAllocatorPage> m_Page;
};