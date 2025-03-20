#include <UploadBuffer.h>

#include <Application.h>
#include <Helpers.h>
#include <Math.h>

#include <d3dx12.h>

#include <new> // for std::bad_alloc

UploadBuffer::UploadBuffer(size_t pageSize) : m_PageSize(pageSize)
{

}

UploadBuffer::Allocation UploadBuffer::Allocate(size_t sizeInBytes, size_t alignment)
{
	if (sizeInBytes > m_PageSize)
	{
		throw std::bad_alloc();
	}

	size_t alignedSize;
	size_t alignedOffset;
	if (!m_CurrentPage || !m_CurrentPage->HasSpace(sizeInBytes, alignment, &alignedSize, &alignedOffset))
	{
		m_CurrentPage = RequestPage();
	}

	return m_CurrentPage->Allocate(alignedSize, alignedOffset);
}

std::shared_ptr<UploadBuffer::Page> UploadBuffer::RequestPage()
{
	std::shared_ptr<Page> page;

	if (!m_AvailablePages.empty())
	{
		page = m_AvailablePages.front();
		m_AvailablePages.pop_front();
	}
	else
	{
		page = std::make_shared<Page>(m_PageSize);
		m_PagePool.push_back(page);
	}

	return page;
}

void UploadBuffer::Reset()
{
	m_CurrentPage = nullptr;
	// Reset all available pages
	m_AvailablePages = m_PagePool;

	for (auto page : m_AvailablePages)
	{
		// Reset the page for new allocations
		page->Reset();
	}
}

UploadBuffer::Page::Page(size_t sizeInBytes)
	: m_PageSize(sizeInBytes)
	, m_Offset(0)
	, m_CPUPtr(nullptr)
	, m_GPUPtr(D3D12_GPU_VIRTUAL_ADDRESS(0))
{
	auto device = Application::Get().GetDevice();

	ThrowIfFaild(device->CreateCommittedResource(
		&CD3DX12_HEAP_PROPERTIES(D3D12_HEAP_TYPE_UPLOAD),
		D3D12_HEAP_FLAG_NONE,
		&CD3DX12_RESOURCE_DESC::Buffer(m_PageSize),
		D3D12_RESOURCE_STATE_GENERIC_READ,
		nullptr,
		IID_PPV_ARGS(&m_Resource)
	));

	m_GPUPtr = m_Resource->GetGPUVirtualAddress();
	m_Resource->Map(0, nullptr, &m_CPUPtr);
}

UploadBuffer::Page::~Page()
{
	m_Resource->Unmap(0, nullptr);
	m_CPUPtr = nullptr;
	m_GPUPtr = D3D12_GPU_VIRTUAL_ADDRESS(0);
}

bool UploadBuffer::Page::HasSpace(size_t sizeInBytes, size_t alignment, size_t* alignedSize, size_t* alignedOffset) const
{
	*alignedSize = Math::AlignUp(sizeInBytes, alignment);
	*alignedOffset = Math::AlignUp(m_Offset, alignment);

	return *alignedOffset + *alignedSize <= m_PageSize;
}

UploadBuffer::Allocation UploadBuffer::Page::Allocate(size_t alignedSize, size_t alignedOffset)
{
	m_Offset = alignedOffset;

	Allocation allocation;
	allocation.CPU = static_cast<uint8_t*>(m_CPUPtr) + m_Offset;
	allocation.GPU = m_GPUPtr + m_Offset;

	m_Offset += alignedSize;

	return allocation;
}

void UploadBuffer::Page::Reset()
{
	m_Offset = 0;
}