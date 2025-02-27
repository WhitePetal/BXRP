#pragma once

// Windows Runtime Library. Needed for Microsoft:WRL:ComPtr<> template class.
#include <wrl.h>

// DirectX 12 specific headers.
#include <d3d12.h>
#include <dxgi1_6.h>

#include <memory>
#include <string>

using namespace Microsoft::WRL;

class Window;
class Engine;
class CommandQueue;

class Application
{
public:
	void Initialize();
	/// <summary>
	/// Create the application singleton with the application instance handle
	/// </summary>
	/// <param name="hInst"></param>
	static void Create(HINSTANCE hInst, HWND parentWnd = nullptr);

	/// <summary>
	/// Destroy the application instance and all windows created by application instance
	/// </summary>
	static void Destroy();

	/// <summary>
	/// Get the application singleton
	/// </summary>
	/// <returns></returns>
	static Application& Get();

	/// <summary>
	/// Check to see if VSync-off is supported
	/// </summary>
	/// <returns></returns>
	bool IsTeraingSupported() const;

	/// <summary>
	/// Create a new DX12 render window instance
	/// </summary>
	/// <param name="windowName"></param>
	/// <param name="clientWidth"></param>
	/// <param name="clientHeight"></param>
	/// <param name="vSync"></param>
	/// <returns></returns>
	std::shared_ptr<Window> CreateRenderWindow(const std::wstring& windowName, int clientWidth, int clientHeight, bool vSync = true);

	/// <summary>
	/// Destroy a window given the window name
	/// </summary>
	/// <param name="windowName"></param>
	void DestroyWindow(const std::wstring& windowName);

	/// <summary>
	/// Destroy a windo given the reference
	/// </summary>
	/// <param name="window"></param>
	void DestroyWindow(std::shared_ptr<Window> window);

	/// <summary>
	/// Find a window by name
	/// </summary>
	/// <param name="windowName"></param>
	/// <returns></returns>
	std::shared_ptr<Window> GetWindowByName(const std::wstring& windowName);

	/// <summary>
	/// Run the application loop and message pump
	/// </summary>
	/// <param name="pEngine"></param>
	/// <returns></returns>
	int Run(std::shared_ptr<Engine> pEngine);

	/// <summary>
	/// Request to quit the application and close all window
	/// </summary>
	/// <param name="exitCode"></param>
	void Quit(int exitCode = 0);

	/// <summary>
	/// Get the DX12 device
	/// </summary>
	/// <returns></returns>
	ComPtr<ID3D12Device2> GetDevice() const;

	/// <summary>
	/// Get a command queue
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	std::shared_ptr<CommandQueue> GetCommandQueue(D3D12_COMMAND_LIST_TYPE type = D3D12_COMMAND_LIST_TYPE_DIRECT) const;

	/// <summary>
	/// Flush all command queues
	/// </summary>
	void Flush();

	/// <summary>
	/// Get the descriptor heap
	/// </summary>
	/// <param name="numDescriptors"></param>
	/// <param name="type"></param>
	/// <returns></returns>
	ComPtr<ID3D12DescriptorHeap> CreateDescriptorHeap(UINT numDescriptors, D3D12_DESCRIPTOR_HEAP_TYPE type);

	/// <summary>
	/// Get the descriptor increment size
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	UINT GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE type);

	bool HaveAndIsParentWnd(HWND wnd);

protected:
	/// <summary>
	/// Create an application instance
	/// </summary>
	/// <param name="hInst"></param>
	Application(HINSTANCE hInst, HWND parentWnd = nullptr);

	virtual ~Application();

	/// <summary>
	/// Get the adapter
	/// </summary>
	/// <param name="useWarp">使用软件适配器</param>
	/// <returns></returns>
	ComPtr<IDXGIAdapter4> GetAdapter(bool useWarp);

	/// <summary>
	/// Create the device object
	/// </summary>
	/// <param name="adapter"></param>
	/// <returns></returns>
	ComPtr<ID3D12Device2> CreateDevice(ComPtr<IDXGIAdapter4> adapter);

	/// <summary>
	/// The VSync-off is supported
	/// </summary>
	/// <returns></returns>
	bool CheckTearingSupport();

private:
	Application(const Application& copy) = delete;
	Application& operator=(const Application& other) = delete;

	/// <summary>
	/// The application instance handle that this application was created with
	/// </summary>
	HINSTANCE m_hInstance;

	HWND m_ParentWnd;

	ComPtr<IDXGIAdapter4> m_dxgiAdapter;
	ComPtr<ID3D12Device2> m_d3d12Device;

	std::shared_ptr<CommandQueue> m_DirectCommandQueue;
	std::shared_ptr<CommandQueue> m_ComputeCommandQueue;
	std::shared_ptr<CommandQueue> m_CopyCommandQueue;

	bool m_TeraingSupported;
};