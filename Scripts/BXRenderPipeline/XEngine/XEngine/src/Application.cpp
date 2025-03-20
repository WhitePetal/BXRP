#include <cassert>

#include <d3d12.h>
#include <dxgi1_6.h>
#include <dxgidebug.h>

#include <Helpers.h>

#include <Window.h>
#include <CommandQueue.h>
#include <Application.h>
#include <Engine.h>

constexpr wchar_t WINDOW_CLASS_NAME[] = L"XEngineWindowClass";

uint64_t Application::s_FrameCount = 0;

static LRESULT CALLBACK WndProc(HWND hwnd, UINT message, WPARAM wParam, LPARAM lParam);

struct MakeWindow : public Window
{
	MakeWindow(HWND hWnd, const std::wstring& windowName, int clientWidth, int clientHeight, bool vSync)
		: Window(hWnd, windowName, clientWidth, clientHeight, vSync)
	{ }
};

Application* Application::gs_pSingelton = nullptr;

WindowMap Application::gs_Windows;
WindowNameMap Application::gs_WindowByName;


Application::Application(HINSTANCE hInst, HWND parentWnd)
	: m_hInstance(hInst)
	, m_TeraingSupported(false)
	, m_ParentWnd(parentWnd)
{
}

void Application::Initialize()
{
	// Windows 10 Creators update adds Per Monitor V2 DPI awareness context.
	// Using this awareness context allows the client area of the window
	// to achieve 100% scaling while still allowing non-client window content to
	// be rendered in a DPI sensitive fashion.
	::SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

#if defined(_DEBUG)
	// Always enable the debug layer before doing anything DX12 related
	// so all possible errors generated while creating DX12 objects
	// are caught by the debug layer.
	ComPtr<ID3D12Debug> debugInterface;
	ThrowIfFaild(D3D12GetDebugInterface(IID_PPV_ARGS(&debugInterface)));
	debugInterface->EnableDebugLayer();
#endif

	// Register a window class for creating our render window with.
	WNDCLASSEXW windowClass = { 0 };

	windowClass.cbSize = sizeof(WNDCLASSEXW);
	windowClass.style = CS_HREDRAW | CS_VREDRAW;
	windowClass.lpfnWndProc = &WndProc;
	windowClass.hInstance = m_hInstance;
	windowClass.hIcon = ::LoadIcon(m_hInstance, NULL);
	windowClass.hCursor = ::LoadCursor(NULL, IDC_ARROW);
	windowClass.hbrBackground = (HBRUSH)(COLOR_WINDOW + 1);
	windowClass.lpszClassName = WINDOW_CLASS_NAME;
	windowClass.cbClsExtra = 0;
	windowClass.cbWndExtra = 0;
	windowClass.lpszMenuName = NULL;
	windowClass.hIconSm = ::LoadIcon(m_hInstance, NULL);

	if (!::RegisterClassExW(&windowClass))
	{
		MessageBoxA(NULL, "Unable to register the window class.", "Error", MB_OK | MB_ICONERROR);
	}

	m_dxgiAdapter = GetAdapter(false);
	if (m_dxgiAdapter)
	{
		m_d3d12Device = CreateDevice(m_dxgiAdapter);
	}

	m_DirectCommandQueue = std::make_shared<CommandQueue>(D3D12_COMMAND_LIST_TYPE_DIRECT);
	m_ComputeCommandQueue = std::make_shared<CommandQueue>(D3D12_COMMAND_LIST_TYPE_COMPUTE);
	m_CopyCommandQueue = std::make_shared<CommandQueue>(D3D12_COMMAND_LIST_TYPE_COPY);

	m_TeraingSupported = CheckTearingSupport();

	m_RaytracingSupported = CheckRaytracingSupport();

	s_FrameCount = 0;
}

ComPtr<IDXGIAdapter4> Application::GetAdapter(bool useWarp)
{
	ComPtr<IDXGIFactory4> dxgiFactory;
	UINT createFactoryFlags = 0;
#if defined(_DEBUG)
	createFactoryFlags = DXGI_CREATE_FACTORY_DEBUG;
#endif

	ThrowIfFaild(CreateDXGIFactory2(createFactoryFlags, IID_PPV_ARGS(&dxgiFactory)));

	ComPtr<IDXGIAdapter1> dxgiAdapter1;
	ComPtr<IDXGIAdapter4> dxgiAdapter4;

	if (useWarp)
	{
		ThrowIfFaild(dxgiFactory->EnumWarpAdapter(IID_PPV_ARGS(&dxgiAdapter1)));
		ThrowIfFaild(dxgiAdapter1.As(&dxgiAdapter4));
	}
	else
	{
		SIZE_T maxDedicatedVideoMemory = 0;
		for (UINT i = 0; dxgiFactory->EnumAdapters1(i, &dxgiAdapter1) != DXGI_ERROR_NOT_FOUND; ++i)
		{
			DXGI_ADAPTER_DESC1 dxgiAdapterDesc1;
			dxgiAdapter1->GetDesc1(&dxgiAdapterDesc1);

			// Check to see if the adapter can create a D3D12 device without actually
			// creating it. The adapter with the largest dedicated video memory
			// is favored.
			if ((dxgiAdapterDesc1.Flags & DXGI_ADAPTER_FLAG_SOFTWARE) == 0 &&
				SUCCEEDED(D3D12CreateDevice(dxgiAdapter1.Get(), D3D_FEATURE_LEVEL_12_1, __uuidof(ID3D12Device), nullptr)) &&
				dxgiAdapterDesc1.DedicatedVideoMemory > maxDedicatedVideoMemory)
			{
				maxDedicatedVideoMemory = dxgiAdapterDesc1.DedicatedVideoMemory;
				ThrowIfFaild(dxgiAdapter1.As(&dxgiAdapter4));
			}
		}
	}

	return dxgiAdapter4;
}

ComPtr<ID3D12Device5> Application::CreateDevice(ComPtr<IDXGIAdapter4> adapter)
{
	ComPtr<ID3D12Device5> d3d12Device5;
	ThrowIfFaild(D3D12CreateDevice(adapter.Get(), D3D_FEATURE_LEVEL_11_0, IID_PPV_ARGS(&d3d12Device5)));

	UINT dxgiFactoryFlags = 0;
	// Enable debug message in debug mode.
#if defined(_DEBUG)
	ComPtr<ID3D12InfoQueue> pInfoQueue;
	if (SUCCEEDED(d3d12Device5.As(&pInfoQueue)))
	{
		pInfoQueue->SetBreakOnSeverity(D3D12_MESSAGE_SEVERITY_CORRUPTION, TRUE);
		pInfoQueue->SetBreakOnSeverity(D3D12_MESSAGE_SEVERITY_ERROR, TRUE);
		pInfoQueue->SetBreakOnSeverity(D3D12_MESSAGE_SEVERITY_WARNING, TRUE);

		// Suppress whole categories of messages
		// D3D12_MESSAGE_CATEGORY Categories[] = {};

		// Suppress message based on their severity level
		D3D12_MESSAGE_SEVERITY Severities[] =
		{
			D3D12_MESSAGE_SEVERITY_INFO
		};

		// Suppress individual messages by their ID
		D3D12_MESSAGE_ID DenyIds[] =
		{
			D3D12_MESSAGE_ID_CLEARRENDERTARGETVIEW_MISMATCHINGCLEARVALUE, // I'm really not sure how to avoid this message.
			D3D12_MESSAGE_ID_MAP_INVALID_NULLRANGE, // This waning occurs uing capture frame while graphics debugging.
			D3D12_MESSAGE_ID_UNMAP_INVALID_NULLRANGE, // This waning occurs uing capture frame while graphics debugging.
		};

		D3D12_INFO_QUEUE_FILTER NewFilter = {};
		//NewFilter.DenyList.NumCategories = _countof(Categories);
		//NewFilter.DenyList.pCategoryList = Categories
		NewFilter.DenyList.NumSeverities = _countof(Severities);
		NewFilter.DenyList.pSeverityList = Severities;
		NewFilter.DenyList.NumIDs = _countof(DenyIds);
		NewFilter.DenyList.pIDList = DenyIds;

		ThrowIfFaild(pInfoQueue->PushStorageFilter(&NewFilter));
	}

	ComPtr<IDXGIInfoQueue> dxgiInfoQueue;
	if (SUCCEEDED(DXGIGetDebugInterface1(0, IID_PPV_ARGS(&dxgiInfoQueue))))
	{
		dxgiFactoryFlags = DXGI_CREATE_FACTORY_DEBUG;
		dxgiInfoQueue->SetBreakOnSeverity(DXGI_DEBUG_ALL, DXGI_INFO_QUEUE_MESSAGE_SEVERITY_ERROR, true);

		dxgiInfoQueue->SetBreakOnSeverity(DXGI_DEBUG_ALL, DXGI_INFO_QUEUE_MESSAGE_SEVERITY_CORRUPTION, true);
	}
#endif
	ThrowIfFaild(CreateDXGIFactory2(dxgiFactoryFlags, IID_PPV_ARGS(&m_dxgiFactory)));
	return d3d12Device5;
}

bool Application::CheckTearingSupport()
{
	BOOL allowTearing = FALSE;

	// Rather than create the DXGI 1.5 factory interface directly, we create the
	// DXGI 1.4 interface and query for the 1.5 interface. This is to enable the
	// graphics debugging tools which will not support the 1.5 factory interface
	// until a future update
	if (SUCCEEDED(CreateDXGIFactory1(IID_PPV_ARGS(&m_dxgiFactory))))
	{
		ComPtr<IDXGIFactory5> factory5;
		if (SUCCEEDED(m_dxgiFactory.As(&factory5)))
		{
			if (FAILED(factory5->CheckFeatureSupport(
				DXGI_FEATURE_PRESENT_ALLOW_TEARING,
				&allowTearing, sizeof(allowTearing)
			)))
			{
				allowTearing = FALSE;
			}
		}
	}

	return allowTearing == TRUE;
}

bool Application::CheckRaytracingSupport()
{
	D3D12_FEATURE_DATA_D3D12_OPTIONS5 options5 = {};
	ThrowIfFaild(m_d3d12Device->CheckFeatureSupport(
		D3D12_FEATURE_D3D12_OPTIONS5,
		&options5,
		sizeof(options5)
	));
	if (options5.RaytracingTier < D3D12_RAYTRACING_TIER_1_0)
	{
		return false;
	}

	return true;
}

void Application::Create(HINSTANCE hInst, HWND parentWnd)
{
	if (!gs_pSingelton)
	{
		gs_pSingelton = new Application(hInst, parentWnd);
		gs_pSingelton->Initialize();
	}
}

void Application::Destroy()
{
	if (gs_pSingelton)
	{
		assert(gs_Windows.empty() && gs_WindowByName.empty() &&
			"All windows should be destroyed before destroying the application instance.");

		delete gs_pSingelton;
		gs_pSingelton = nullptr;
	}
}

Application& Application::Get()
{
	assert(gs_pSingelton);
	return *gs_pSingelton;
}

bool Application::IsTeraingSupported() const
{
	return m_TeraingSupported;
}

std::shared_ptr<Window> Application::CreateRenderWindow(const std::wstring& windowName, int clientWidth, int clientHeight, bool vSync)
{
	// First check if a window with the given name already exists
	WindowNameMap::iterator windowIter = gs_WindowByName.find(windowName);
	if (windowIter != gs_WindowByName.end())
	{
		return windowIter->second;
	}

	RECT windowRect = { 0, 0, clientWidth, clientHeight };
	::AdjustWindowRect(&windowRect, WS_OVERLAPPEDWINDOW, FALSE);

	int screenWidth = ::GetSystemMetrics(SM_CXSCREEN);
	int screenHeight = ::GetSystemMetrics(SM_CYSCREEN);
	int windowWidth = windowRect.right - windowRect.left;
	int windowHeight = windowRect.bottom - windowRect.top;

	// Center the window within the screen. Clamp to 0, 0 for the top-left corner
	int windowX = std::max<int>(0, (screenWidth - windowWidth) / 2);
	int windowY = std::max<int>(0, (screenHeight - windowHeight) / 2);

	HWND hWnd = ::CreateWindowExW(
		NULL,
		WINDOW_CLASS_NAME,
		windowName.c_str(),
		WS_OVERLAPPEDWINDOW,
		windowX,
		windowY,
		windowWidth,
		windowHeight,
		m_ParentWnd,
		NULL,
		m_hInstance,
		nullptr
	);

	if (!hWnd)
	{
		::MessageBoxA(NULL, "Could not create the render window.", "Error", MB_OK | MB_ICONERROR);
		return nullptr;
	}

	WindowPtr pWindow = std::make_shared<MakeWindow>(hWnd, windowName, clientWidth, clientHeight, vSync);

	gs_Windows.insert(WindowMap::value_type(hWnd, pWindow));
	gs_WindowByName.insert(WindowNameMap::value_type(windowName, pWindow));

	return pWindow;
}

void Application::DestroyWindow(const std::wstring& windowName)
{
	WindowPtr pWindow = GetWindowByName(windowName);
	if (pWindow)
	{
		DestroyWindow(pWindow);
	}
}

void Application::DestroyWindow(std::shared_ptr<Window> window)
{
	window->Destroy();
}

std::shared_ptr<Window> Application::GetWindowByName(const std::wstring& windowName)
{
	std::shared_ptr<Window> window;
	WindowNameMap::iterator iter = gs_WindowByName.find(windowName);
	if (iter != gs_WindowByName.end())
	{
		window = iter->second;
	}
	return window;
}

int Application::Run(std::shared_ptr<Engine> pEngine)
{
	if (!pEngine->Initialize()) return 1;
	if (!pEngine->LoadContent()) return 2;
	MSG msg = { 0 };
	while (msg.message != WM_QUIT)
	{
		if (::PeekMessage(&msg, 0, 0, 0, PM_REMOVE))
		{
			::TranslateMessage(&msg);
			::DispatchMessage(&msg);
		}
	}

	// Flush any commands in the commands queues before quiting
	Flush();

	pEngine->UnloadContent();
	pEngine->Destroy();

	return 0;
}

void Application::Quit(int exitCode)
{
	PostQuitMessage(exitCode);
}

ComPtr<ID3D12Device5> Application::GetDevice() const
{
	return m_d3d12Device;
}

std::shared_ptr<CommandQueue> Application::GetCommandQueue(D3D12_COMMAND_LIST_TYPE type) const
{
	std::shared_ptr<CommandQueue> commandQueue;
	switch (type)
	{
	case D3D12_COMMAND_LIST_TYPE_DIRECT:
		commandQueue = m_DirectCommandQueue;
		break;
	case D3D12_COMMAND_LIST_TYPE_COMPUTE:
		commandQueue = m_ComputeCommandQueue;
		break;
	case D3D12_COMMAND_LIST_TYPE_COPY:
		commandQueue = m_CopyCommandQueue;
		break;
	default:
		assert(false && "Invalid command queue type.");
	}
	return commandQueue;
}

void Application::Flush()
{
	m_DirectCommandQueue->Flush();
	m_ComputeCommandQueue->Flush();
	m_CopyCommandQueue->Flush();
}

ComPtr<ID3D12DescriptorHeap> Application::CreateDescriptorHeap(UINT numDescriptors, D3D12_DESCRIPTOR_HEAP_TYPE type)
{
	ComPtr<ID3D12DescriptorHeap> descriptorHeap;

	D3D12_DESCRIPTOR_HEAP_DESC desc = {};
	desc.NumDescriptors = numDescriptors;
	desc.Type = type;
	desc.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_NONE;
	desc.NodeMask = 0;

	ThrowIfFaild(m_d3d12Device->CreateDescriptorHeap(&desc, IID_PPV_ARGS(&descriptorHeap)));

	return descriptorHeap;
}

UINT Application::GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE type)
{
	return m_d3d12Device->GetDescriptorHandleIncrementSize(type);
}

bool Application::IsParentWnd(HWND wnd)
{
	if (m_ParentWnd == nullptr) return false;
	return m_ParentWnd == wnd;
}

Application::~Application()
{
	Flush();
}

void Application::RemoveWindow(HWND hWnd)
{
	WindowMap::iterator windowIter = gs_Windows.find(hWnd);
	if (windowIter != gs_Windows.end())
	{
		WindowPtr pWindow = windowIter->second;
		gs_WindowByName.erase(pWindow->GetWindowName());
		gs_Windows.erase(windowIter);
	}
}

WindowPtr Application::Find(HWND hwnd)
{
	WindowPtr pWindow;
	WindowMap::iterator iter = gs_Windows.find(hwnd);
	if (iter != gs_Windows.end())
	{
		pWindow = iter->second;
	}
	return pWindow;
}

// Convert the message ID into a MouseButton ID
MouseButtonEventArgs::MouseButton DecodeMouseButton(UINT messageID)
{
	MouseButtonEventArgs::MouseButton mouseButton = MouseButtonEventArgs::None;
	switch (messageID)
	{
	case WM_LBUTTONDOWN:
	case WM_LBUTTONUP:
	case WM_LBUTTONDBLCLK:
	{
		mouseButton = MouseButtonEventArgs::Left;
	}
	break;
	case WM_RBUTTONDOWN:
	case WM_RBUTTONUP:
	case WM_RBUTTONDBLCLK:
	{
		mouseButton = MouseButtonEventArgs::Right;
	}
	break;
	case WM_MBUTTONDOWN:
	case WM_MBUTTONUP:
	case WM_MBUTTONDBLCLK:
	{
		mouseButton = MouseButtonEventArgs::Middel;
	}
	break;
	}

	return mouseButton;
}

uint64_t Application::GetFrameCount()
{
	return s_FrameCount;
}

static LRESULT CALLBACK WndProc(HWND hwnd, UINT message, WPARAM wParam, LPARAM lParam)
{
	//::OutputDebugString(L"Processing message: X\n");
	//if (Application::Get().IsParentWnd(hwnd))
	//{
	//	return DefWindowProcW(hwnd, message, wParam, lParam);
	//}
	WindowPtr pWindow = Application::Find(hwnd);

	if (!pWindow)
	{
		return DefWindowProcW(hwnd, message, wParam, lParam);
	}

	switch (message)
	{
	case WM_PAINT:
	{
		++Application::s_FrameCount;
		// delta time will be filled in by the window
		UpdateEventArgs updateEventArgs(0.0f, 0.0f);
		pWindow->OnUpdate(updateEventArgs);
		// delta time will be filled in by the window
		RenderEventArgs renderEventArgs(0.0f, 0.0f);
		pWindow->OnRender(renderEventArgs);
	}
	break;
	case WM_SYSKEYDOWN:
	case WM_KEYDOWN:
	{
		MSG charMsg;
		// Get the Unicode character (UTF-16)
		unsigned int c = 0;
		// For printable characters, the next message will be WM_CHAR.
		// This message contains the character code we need to send the KeyPressed event.
		// Inspired by the SDL 1.2 implementation.
		if (PeekMessage(&charMsg, hwnd, 0, 0, PM_NOREMOVE) && charMsg.message == WM_CHAR)
		{
			GetMessage(&charMsg, hwnd, 0, 0);
			c = static_cast<unsigned int>(charMsg.wParam);
		}
		bool shift = (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0;
		bool control = (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0;
		bool alt = (GetAsyncKeyState(VK_MENU) & 0x8000) != 0;
		KeyCode::Key key = (KeyCode::Key)wParam;
		unsigned int scanCode = (lParam & 0x00FF0000) >> 16;
		KeyEventArgs keyEventArgs(key, c, KeyEventArgs::Down, shift, control, alt);
		pWindow->OnKeyboardDown(keyEventArgs);
	}
	break;
	case WM_SYSKEYUP:
	case WM_KEYUP:
	{
		bool shift = (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0;
		bool control = (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0;
		bool alt = (GetAsyncKeyState(VK_MENU) & 0x8000) != 0;
		KeyCode::Key key = (KeyCode::Key)wParam;
		unsigned int c = 0;
		unsigned int scanCode = (lParam & 0x00FF0000) >> 16;

		// Determine which key was released by converting the key code and the scan code
		// to a printable character (if possible).
		// Inspired by the SDL 1.2 implementation.
		unsigned char keyboardState[256];
		GetKeyboardState(keyboardState);
		wchar_t translatedCharacters[4];
		if (int result = ToUnicodeEx(static_cast<UINT>(wParam), scanCode, keyboardState, translatedCharacters, 4, 0, NULL) > 0)
		{
			c = translatedCharacters[0];
		}

		KeyEventArgs keyEventArgs(key, c, KeyEventArgs::Up, shift, control, alt);
		pWindow->OnKeyboardUp(keyEventArgs);
	}
	break;
	// The default window procedure will play a system notification sound
	// when pressing the Alt+Enter keyboard combination if this message is
	// not handled
	case WM_SYSCHAR:
		break;
	case WM_MOUSEMOVE:
	{
		bool lButton = (wParam & MK_LBUTTON) != 0;
		bool rButton = (wParam & MK_RBUTTON) != 0;
		bool mButton = (wParam & MK_MBUTTON) != 0;
		bool shift = (wParam & MK_SHIFT) != 0;
		bool control = (wParam & MK_CONTROL) != 0;

		int x = ((int)(short)LOWORD(lParam));
		int y = ((int)(short)HIWORD(lParam));

		MouseMotionEventArgs mouseMotionEventArgs(lButton, mButton, rButton, control, shift, x, y);
		pWindow->OnMouseMoved(mouseMotionEventArgs);
	}
	break;
	case WM_LBUTTONDOWN:
	case WM_RBUTTONDOWN:
	case WM_MBUTTONDOWN:
	{
		bool lButton = (wParam & MK_LBUTTON) != 0;
		bool rButton = (wParam & MK_RBUTTON) != 0;
		bool mButton = (wParam & MK_MBUTTON) != 0;
		bool shift = (wParam & MK_SHIFT) != 0;
		bool control = (wParam & MK_CONTROL) != 0;

		int x = ((int)(short)LOWORD(lParam));
		int y = ((int)(short)HIWORD(lParam));

		MouseButtonEventArgs mouseButtonEventArgs(DecodeMouseButton(message), MouseButtonEventArgs::Down, lButton, mButton, rButton, control, shift, x, y);
		pWindow->OnMouseButtonDown(mouseButtonEventArgs);
	}
	break;
	case WM_LBUTTONUP:
	case WM_RBUTTONUP:
	case WM_MBUTTONUP: 
	{
		bool lButton = (wParam & MK_LBUTTON) != 0;
		bool rButton = (wParam & MK_RBUTTON) != 0;
		bool mButton = (wParam & MK_MBUTTON) != 0;
		bool shift = (wParam & MK_SHIFT) != 0;
		bool control = (wParam & MK_CONTROL) != 0;

		int x = ((int)(short)LOWORD(lParam));
		int y = ((int)(short)HIWORD(lParam));

		MouseButtonEventArgs mouseButtonEventArgs(DecodeMouseButton(message), MouseButtonEventArgs::Up, lButton, mButton, rButton, control, shift, x, y);
		pWindow->OnMouseButtonUp(mouseButtonEventArgs);
	}
	break;
	case WM_MOUSEWHEEL:
	{
		// The distance the mouse wheel is rotated.
		// A positive value indicates the wheel was rotated to the right.
		// A negative value indicates the wheel was rotated to the left.
		float zDelta = ((int)(short)HIWORD(wParam)) / (float)WHEEL_DELTA;
		short keyStates = (short)LOWORD(wParam);

		bool lButton = (keyStates & MK_LBUTTON) != 0;
		bool rButton = (keyStates & MK_RBUTTON) != 0;
		bool mButton = (keyStates & MK_MBUTTON) != 0;
		bool shift = (keyStates & MK_SHIFT) != 0;
		bool control = (keyStates & MK_CONTROL) != 0;

		int x = ((int)(short)LOWORD(lParam));
		int y = ((int)(short)HIWORD(lParam));

		// Convert the screen coordinates to client coordinates.
		POINT clientToScreenPoint;
		clientToScreenPoint.x = x;
		clientToScreenPoint.y = y;
		ScreenToClient(hwnd, &clientToScreenPoint);

		MouseWheelEventArgs mouseWheelEventArgs(zDelta, lButton, mButton, rButton, control, shift, (int)clientToScreenPoint.x, (int)clientToScreenPoint.y);
		pWindow->OnMouseWheel(mouseWheelEventArgs);
	}
	break;
	case WM_SIZE:
	{
		int width = ((int)(short)LOWORD(lParam));
		int height = ((int)(short)HIWORD(lParam));
		
		ResizeEventArgs resizeEventArgs(width, height);
		pWindow->OnResize(resizeEventArgs);
	}
	break;
	case WM_DESTROY:
		Application::RemoveWindow(hwnd);
		if (Application::gs_Windows.empty())
		{
			::PostQuitMessage(0);
		}
		break;
	default:
		return ::DefWindowProcW(hwnd, message, wParam, lParam);
	}

	return 0;
}