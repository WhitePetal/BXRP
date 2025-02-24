#include <chrono>

#include <Helpers.h>
#include <CommandQueue.h>

CommandQueue::CommandQueue(ComPtr<ID3D12Device2> device, D3D12_COMMAND_LIST_TYPE type)
{
}

CommandQueue::~CommandQueue()
{
}

ComPtr<ID3D12GraphicsCommandList2> CommandQueue::GetCommandList()
{
	return ComPtr<ID3D12GraphicsCommandList2>();
}

uint64_t CommandQueue::ExecuteCommandList(ComPtr<ID3D12GraphicsCommandList2> commandList)
{
	return uint64_t();
}

uint64_t CommandQueue::Signal()
{
	uint64_t fenceValueForSignal = ++m_FenceValue;
	ThrowIfFaild(m_d3d12CommandQueue->Signal(m_d3d12Fence.Get(), fenceValueForSignal));

	return fenceValueForSignal;
}

bool CommandQueue::IsFenceComplete(uint64_t fenceValue)
{
	return m_d3d12Fence->GetCompletedValue() >= fenceValue;
}

void CommandQueue::WaitForFenceValue(uint64_t fenceValue)
{
	if (m_d3d12Fence->GetCompletedValue() < fenceValue)
	{
		ThrowIfFaild(m_d3d12Fence->SetEventOnCompletion(fenceValue, m_FenceEvent));
		::WaitForSingleObject(m_FenceEvent, DWORD_MAX);
	}
}

void CommandQueue::Flush()
{
	WaitForFenceValue(Signal());
}

ComPtr<ID3D12CommandQueue> CommandQueue::GetD3D12CommandQueue() const
{
	return ComPtr<ID3D12CommandQueue>();
}
