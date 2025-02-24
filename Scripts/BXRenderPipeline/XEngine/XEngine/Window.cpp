#include <cassert>

#include <Application.h>
#include <Window.h>
#include <Helpers.h>


Window::Window(HWND hWnd, const std::wstring& windowName, int clientWidth, int clientHeight, bool vSync)
	: m_hWnd(hWnd),
	m_WindowName(windowName),
	m_ClientWidth(clientWidth),
	m_ClientHeight(clientHeight),
	m_VSync(vSync),
	m_Fullscreen(false),
	m_FrameCounter(0)
{
	Application& app = Application::Get();

	m_IsTearingSupported = app.IsTeraingSupported();

	m_dxgiSwapChain = CreateSwapChain(app);
	m_d3d12RTVDescriptorHeap = app.CreateDescriptorHeap(BackBufferCount, D3D12_DESCRIPTOR_HEAP_TYPE_RTV);
	m_RTVDescriptorSize = app.GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_RTV);

	UpdateRenderTargetViews();
}

Window::~Window()
{
	// Window should be destroyed with Application::DestroyWindow before
	// the window goes out of scope.
	assert(!m_hWnd && "Use Application::DestroyWindow before destruction.");
}

void Window::RegisterCallbacks(std::shared_ptr<Engine> pEngine)
{
	m_pEngine = pEngine;
}

void Window::OnUpdate()
{
	m_UpdateClock.Tick();

	if (auto pEngine = m_pEngine.lock())
	{
		m_FrameCounter++;


	}
}

void Window::OnRender()
{
	m_RenderClock.Tick();

	if (auto pEngine = m_pEngine.lock())
	{
		
	}
}

HWND Window::GetWindowHandle() const
{
	return m_hWnd;
}

const std::wstring& Window::GetWindowName() const
{
	return m_WindowName;
}

int Window::GetClientWidth() const
{
	return m_ClientWidth;
}

int Window::GetClientHeight() const
{
	return m_ClientHeight;
}

bool Window::IsVSync() const
{
	return m_VSync;
}

void Window::SetVSync(bool vSync)
{
	m_VSync = vSync;
}

void Window::ToggleVSync()
{
	SetVSync(!m_VSync);
}

bool Window::IsFullScreen() const
{
	return m_Fullscreen;
}

void Window::SetFullScreen(bool fullscreen)
{
	if (m_Fullscreen != fullscreen)
	{
		m_Fullscreen = fullscreen;

		if (m_Fullscreen) // Switching to fullscreen
		{
			// Store the current window dimensions so they can be restored
			// when switching out of fullscreen state.
			::GetWindowRect(m_hWnd, &m_WindowRect);

			// Set the window style to a borderless window so the client area fills
			// the enter screen.
			UINT windowStyle = WS_OVERLAPPEDWINDOW & ~(WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX);
			::SetWindowLongW(m_hWnd, GWL_STYLE, windowStyle);

			// Query the name of the nearest display device for the window.
			// This is required to set the fullscreen dimensions of the window
			// when using a multi-monitor steup.
			HMONITOR hMonitor = ::MonitorFromWindow(m_hWnd, MONITOR_DEFAULTTONEAREST);
			MONITORINFOEX monitorInfo = {};
			monitorInfo.cbSize = sizeof(MONITORINFOEX);
			::GetMonitorInfo(hMonitor, &monitorInfo);

			::SetWindowPos(m_hWnd, HWND_TOP,
				monitorInfo.rcMonitor.left,
				monitorInfo.rcMonitor.top,
				monitorInfo.rcMonitor.right - monitorInfo.rcMonitor.left,
				monitorInfo.rcMonitor.bottom - monitorInfo.rcMonitor.top,
				SWP_FRAMECHANGED | SWP_NOACTIVATE);

			::ShowWindow(m_hWnd, SW_MAXIMIZE);
		}
		else
		{
			// Restore all the window decorators.
			::SetWindowLong(m_hWnd, GWL_STYLE, WS_OVERLAPPEDWINDOW);

			::SetWindowPos(m_hWnd, HWND_NOTOPMOST,
				m_WindowRect.left,
				m_WindowRect.top,
				m_WindowRect.right - m_WindowRect.left,
				m_WindowRect.bottom - m_WindowRect.top,
				SWP_FRAMECHANGED | SWP_NOACTIVATE);

			::ShowWindow(m_hWnd, SW_NORMAL);
		}
	}
}

void Window::ToggleFullscreen()
{
	SetFullScreen(!m_Fullscreen);
}

void Window::Show()
{
	::ShowWindow(m_hWnd, SW_SHOW);
}

void Window::Hide()
{
	::ShowWindow(m_hWnd, SW_HIDE);
}

UINT Window::GetCurrentBackBufferIndex() const
{
	return m_CurrentBackBufferIndex;
}

UINT Window::Present()
{
	UINT syncInterval = m_VSync ? 1 : 0;
	UINT presentFlags = m_IsTearingSupported && !m_VSync ? DXGI_PRESENT_ALLOW_TEARING : 0;
	ThrowIfFaild(m_dxgiSwapChain->Present(syncInterval, presentFlags));
	m_CurrentBackBufferIndex = m_dxgiSwapChain->GetCurrentBackBufferIndex();

	return m_CurrentBackBufferIndex;
}

D3D12_CPU_DESCRIPTOR_HANDLE Window::GetCurrentRenderTargetView() const
{
	return CD3DX12_CPU_DESCRIPTOR_HANDLE(m_d3d12RTVDescriptorHeap->GetCPUDescriptorHandleForHeapStart(), m_CurrentBackBufferIndex, m_RTVDescriptorSize);
}

ComPtr<ID3D12Resource> Window::GetCurrentBackBuffer() const
{
	return m_d3d12BackBuffers[m_CurrentBackBufferIndex];
}

void Window::Destroy()
{
	if (auto pEngine = m_pEngine.lock())
	{
		// Notify the registered engine that the window is being destroyed.
	}
	if (m_hWnd)
	{
		::DestroyWindow(m_hWnd);
		m_hWnd = nullptr;
	}
}

ComPtr<IDXGISwapChain4> Window::CreateSwapChain(Application& app)
{
	ComPtr<IDXGISwapChain4> swapChain4;
	ComPtr<IDXGIFactory4> dxgiFactory4;
	UINT createFactoryFlags = 0;
#if defined(_DEBUG)
	createFactoryFlags = DXGI_CREATE_FACTORY_DEBUG;
#endif

	ThrowIfFaild(CreateDXGIFactory2(createFactoryFlags, IID_PPV_ARGS(&dxgiFactory4)));

	DXGI_SWAP_CHAIN_DESC1 swapChainDesc = {};
	swapChainDesc.Width = m_ClientWidth;
	swapChainDesc.Height = m_ClientHeight;
	swapChainDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
	swapChainDesc.Stereo = FALSE;
	swapChainDesc.SampleDesc = { 1, 0 };
	swapChainDesc.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
	swapChainDesc.BufferCount = BackBufferCount;
	swapChainDesc.Scaling = DXGI_SCALING_STRETCH;
	swapChainDesc.SwapEffect = DXGI_SWAP_EFFECT_FLIP_DISCARD;
	swapChainDesc.AlphaMode = DXGI_ALPHA_MODE_UNSPECIFIED;
	// It is recommended to always teraing if tearing support is available.
	swapChainDesc.Flags = m_IsTearingSupported ? DXGI_SWAP_CHAIN_FLAG_ALLOW_TEARING : 0;

	ID3D12CommandQueue* pCommandQueue = app.GetCommandQueue()->GetD3D12CommandQueue().Get();

	ComPtr<IDXGISwapChain1> swapChain1;
	ThrowIfFaild(dxgiFactory4->CreateSwapChainForHwnd(
		pCommandQueue,
		m_hWnd,
		&swapChainDesc,
		nullptr,
		nullptr,
		&swapChain1
	));

	// Disable the Alt+Enter fullscreen toggle feature. Switching to fullscreen
	// will be handled manually.
	ThrowIfFaild(dxgiFactory4->MakeWindowAssociation(m_hWnd, DXGI_MWA_NO_ALT_ENTER));

	ThrowIfFaild(swapChain1.As(&swapChain4));

	m_CurrentBackBufferIndex = swapChain4->GetCurrentBackBufferIndex();

	return swapChain4;
}

void Window::UpdateRenderTargetViews(Application& app)
{
	auto device = app.GetDevice();

	CD3DX12_CPU_DESCRIPTOR_HANDLE rtvHandle(m_d3d12RTVDescriptorHeap->GetCPUDescriptorHandleForHeapStart());

	for (int i = 0; i < BackBufferCount; ++i)
	{
		ComPtr<ID3D12Resource> backBuffer;
		ThrowIfFaild(m_dxgiSwapChain->GetBuffer(i, IID_PPV_ARGS(&backBuffer)));

		device->CreateRenderTargetView(backBuffer.Get(), nullptr, rtvHandle);

		m_d3d12BackBuffers[i] = backBuffer;

		rtvHandle.Offset(m_RTVDescriptorSize);
	}
}
