#pragma once

#include <d3d12.h>
#include <wrl.h>

#include <cstdint>
#include <queue>

using namespace Microsoft::WRL;

class CommandQueue
{
public:
	CommandQueue(D3D12_COMMAND_LIST_TYPE type);
	virtual ~CommandQueue();

	/// <summary>
	/// Get a available command list from the command queue.
	/// </summary>
	/// <returns></returns>
	ComPtr<ID3D12GraphicsCommandList4> GetCommandList();

	/// <summary>
	/// Execute a command list
	/// </summary>
	/// <param name="commandList">the fence value to wait for this command list</param>
	/// <returns></returns>
	uint64_t ExecuteCommandList(ComPtr<ID3D12GraphicsCommandList4> commandList);

	/// <summary>
	/// Make CommandQueue Signal a fence
	/// </summary>
	/// <returns></returns>
	uint64_t Signal();

	/// <summary>
	/// Is all commands compelete before the fence sign 
	/// </summary>
	/// <param name="fenceValue"></param>
	/// <returns></returns>
	bool IsFenceComplete(uint64_t fenceValue);

	/// <summary>
	/// Make CPU Wait fence sign
	/// </summary>
	/// <param name="fenceValue"></param>
	void WaitForFenceValue(uint64_t fenceValue);

	/// <summary>
	/// Make CPU wait CommandQueue All commands complete
	/// </summary>
	void Flush();

	/// <summary>
	/// get the d3d12_command_queue obj
	/// </summary>
	/// <returns></returns>
	ComPtr<ID3D12CommandQueue> GetD3D12CommandQueue() const;

protected:
	ComPtr<ID3D12CommandAllocator> CreateCommandAllocator();
	ComPtr<ID3D12GraphicsCommandList4> CreateCommandList(ComPtr<ID3D12CommandAllocator> allocator);

private:
	// Keep track of command allocators that are "in-flight"
	struct CommandAllocatorEntry
	{
		uint64_t fenceValue;
		ComPtr<ID3D12CommandAllocator> commandAllocator;
	};

	using CommandAllocatorQueue = std::queue<CommandAllocatorEntry>;
	using CommandListQueue = std::queue<ComPtr<ID3D12GraphicsCommandList4>>;

	D3D12_COMMAND_LIST_TYPE m_CommandListType;

	ComPtr<ID3D12CommandQueue> m_d3d12CommandQueue;
	ComPtr<ID3D12Fence> m_d3d12Fence;
	HANDLE m_FenceEvent;
	uint64_t m_FenceValue;

	CommandAllocatorQueue m_CommandAllocatorQueue;
	CommandListQueue m_CommandListQueue;
};
