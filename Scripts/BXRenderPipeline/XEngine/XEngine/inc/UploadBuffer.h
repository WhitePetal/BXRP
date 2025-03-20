#pragma once

#include <StorageDefines.h>

#include <wrl.h>
#include <d3d12.h>

#include <memory>
#include <deque>

class UploadBuffer
{
public:
	/// <summary>
	/// Create  a memory page
	/// </summary>
	/// <param name="pageSize">The size to use to allocate new pages in GPU memory</param>
	explicit UploadBuffer(size_t pageSize = _2MB);

	// Use to upload data to the GPU
	struct Allocation
	{
		void* CPU;
		D3D12_GPU_VIRTUAL_ADDRESS GPU;
	};

	/// <summary>
	/// The maxium size of an allocation is the size of a single page
	/// </summary>
	/// <returns></returns>
	size_t GetPagesSize() const
	{
		return m_PageSize;
	}

	/// <summary>
	/// Allocate memory in an Upload heap
	/// An allocation must not exceed the size of a page
	/// Use a memcpy or similar method to copy the
	/// buffer data to CPU pointer in the Allocation
	/// structure returned from this function
	/// </summary>
	/// <param name="sizeInBytes"></param>
	/// <param name="alignment"></param>
	/// <returns></returns>
	Allocation Allocate(size_t sizeInBytes, size_t alignment);

	/// <summary>
	/// Release all allocated pages.
	/// This should only be done when the command list is
	/// finished excuting on the CommandQueue
	/// </summary>
	void Reset();

private:
	struct Page
	{
	public:
		Page(size_t sizeInBytes);
		~Page();

		/// <summary>
		/// Check to see if the page has room to satisfy the requested allocation
		/// </summary>
		/// <param name="sizeInBytes"></param>
		/// <param name="alignment"></param>
		/// <returns></returns>
		bool HasSpace(size_t sizeInBytes, size_t alignment, size_t* alignedSize, size_t* alignedOffset) const;

		/// <summary>
		/// Allocate memory from the page
		/// Throws std::bad_alloc if the allocation size is larger that
		/// the page size or the size of the allocation exceeds the
		/// remaining space in the page
		/// </summary>
		/// <param name="sizeInBytes"></param>
		/// <param name="alignment"></param>
		/// <returns></returns>
		Allocation Allocate(size_t alignedSize, size_t alignedOffset);

		/// <summary>
		/// Reset the page for reuse
		/// </summary>
		void Reset();

	private:
		Microsoft::WRL::ComPtr<ID3D12Resource> m_Resource;

		void* m_CPUPtr;
		D3D12_GPU_VIRTUAL_ADDRESS m_GPUPtr;

		size_t m_PageSize;
		size_t m_Offset;
	};

	/// <summary>
	/// A pool of memory pages
	/// </summary>
	using PagePool = std::deque<std::shared_ptr<Page>>;

	/// <summary>
	/// Request a page from the pool of available pages
	/// or create a new page if there are no available pages
	/// </summary>
	/// <returns></returns>
	std::shared_ptr<Page> RequestPage();

	PagePool m_PagePool;
	PagePool m_AvailablePages;

	std::shared_ptr<Page> m_CurrentPage;

	/// <summary>
	/// The size of each page of memory
	/// </summary>
	size_t m_PageSize;
};