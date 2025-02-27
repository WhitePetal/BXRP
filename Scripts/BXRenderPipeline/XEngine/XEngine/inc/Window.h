#pragma once

#include <string>
#include <memory>

#include <d3d12.h>
#include <dxgi1_6.h>

#include <HighResolutionClock.h>
#include <Events.h>
#include <Application.h>

#include <wrl.h>
using namespace Microsoft::WRL;


class Engine;

class Window
{
public:
	/// <summary>
	/// Number of swapchain back buffers
	/// </summary>
	static const UINT BackBufferCount = 3;

	/// <summary>
	/// Get a handle to this window's instance
	/// </summary>
	/// <returns></returns>
	HWND GetWindowHandle() const;

	/// <summary>
	/// Get the window name
	/// </summary>
	/// <returns></returns>
	const std::wstring& GetWindowName() const;

	/// <summary>
	/// Get the window width
	/// </summary>
	/// <returns></returns>
	int GetClientWidth() const;
	/// <summary>
	/// Get the window height
	/// </summary>
	/// <returns></returns>
	int GetClientHeight() const;

	/// <summary>
	/// Should this window be renderred with vsync
	/// </summary>
	/// <returns></returns>
	bool IsVSync() const;
	void SetVSync(bool vSync);
	void ToggleVSync();

	/// <summary>
	/// Is this a windowed window or full-screen?
	/// </summary>
	/// <returns></returns>
	bool IsFullScreen() const;

	/// <summary>
	/// Set the fullscreen of the window (无边框全屏窗口)
	/// </summary>
	/// <param name="fullscreen"></param>
	void SetFullScreen(bool fullscreen);
	void ToggleFullscreen();

	/// <summary>
	/// Show the window
	/// </summary>
	void Show();

	/// <summary>
	/// Hide the window
	/// </summary>
	void Hide();

	/// <summary>
	/// Return the current back buffer index
	/// </summary>
	/// <returns></returns>
	UINT GetCurrentBackBufferIndex() const;

	/// <summary>
	/// Present the swapchain's back buffer to the screen
	/// </summary>
	/// <returns>the current back buffer index after the present</returns>
	UINT Present();

	/// <summary>
	/// Get the render target view for the current back buffer
	/// </summary>
	/// <returns></returns>
	D3D12_CPU_DESCRIPTOR_HANDLE GetCurrentRenderTargetView()  const;

	/// <summary>
	/// Get the back buffer resource for the current back buffer
	/// </summary>
	/// <returns></returns>
	ComPtr<ID3D12Resource> GetCurrentBackBuffer() const;

	/// <summary>
	/// Destroy the window
	/// </summary>
	void Destroy();

protected:
	friend LRESULT CALLBACK WndProc(HWND hwnd, UINT message, WPARAM wParam, LPARAM lParam);

	friend class Application;

	friend class Engine;

	Window() = delete;
	Window(HWND hWnd, const std::wstring& windowName, int clientWidth, int clientHeight, bool vSync);
	virtual ~Window();

	void RegisterCallbacks(std::shared_ptr<Engine> pEngine);

	virtual void OnUpdate(UpdateEventArgs& e);
	virtual void OnRender(RenderEventArgs& e);
	virtual void OnResize(ResizeEventArgs& e);

	virtual void OnKeyboardDown(KeyEventArgs& e);
	virtual void OnKeyboardUp(KeyEventArgs& e);
	virtual void OnMouseMoved(MouseMotionEventArgs& e);
	virtual void OnMouseButtonDown(MouseButtonEventArgs& e);
	virtual void OnMouseButtonUp(MouseButtonEventArgs& e);
	virtual void OnMouseWheel(MouseWheelEventArgs& e);

private:
	Window(const Window& copy) = delete;
	Window& operator=(const Window& other) = delete;

	ComPtr<IDXGISwapChain4> CreateSwapChain(Application& app);

	void UpdateRenderTargetViews(Application& app);

	HWND m_hWnd;

	std::wstring m_WindowName;

	int m_ClientWidth;
	int m_ClientHeight;
	bool m_VSync;
	bool m_Fullscreen;

	HighResolutionClock m_UpdateClock;
	HighResolutionClock m_RenderClock;
	uint64_t m_FrameCounter;

	std::weak_ptr<Engine> m_pEngine;

	ComPtr<IDXGISwapChain4> m_dxgiSwapChain;
	ComPtr<ID3D12DescriptorHeap> m_d3d12RTVDescriptorHeap;
	ComPtr<ID3D12Resource> m_d3d12BackBuffers[BackBufferCount];

	UINT m_RTVDescriptorSize;
	UINT m_CurrentBackBufferIndex;

	RECT m_WindowRect;
	bool m_IsTearingSupported;
};