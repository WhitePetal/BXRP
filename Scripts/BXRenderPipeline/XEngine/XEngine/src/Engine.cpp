#define WIN32_LEAN_AND_MEAN
#include <Windows.h>
#include <Shlwapi.h>
#include <psapi.h>


// The min/max macros conflict with like-named member functions.
// Only use std::main and std::max defined in <algorithm>.
#if defined(min)
#undef min
#endif // defined(min)

#if defined(max)
#undef max
#endif // defined(max)

// In order to define a function called CreateWindow, thew Windows macro needs to 
// be undefined.
#if defined(CreateWindow)
#undef CreateWindow
#endif // defined(CreateWindow)

// Windows Runtime Library. Needed for Microsoft:WRL:ComPtr<> template class.
#include <wrl.h>
using namespace Microsoft::WRL;

// DirectX 12 specific headers.
#include <d3d12.h>
#include <dxgi1_6.h>
#include <dxgidebug.h>
#include <d3dcompiler.h>
#include <DirectXMath.h>

// D3D12 extension library.
#include <d3dx12.h>

// STL Headers
#include <algorithm>
#include <cassert>
#include <chrono>

#include <Helpers.h>

#include <Engine.h>
#include <Application.h>
#include <Window.h>
#include <SampleScene.h>
#include <Debug.h>

HINSTANCE g_hInstance;

void ReportLiveObjects()
{
	IDXGIDebug1* dxgiDebug;
	DXGIGetDebugInterface1(0, IID_PPV_ARGS(&dxgiDebug));

	dxgiDebug->ReportLiveObjects(DXGI_DEBUG_ALL, DXGI_DEBUG_RLO_IGNORE_INTERNAL);
	dxgiDebug->Release();
}

/// <summary>
/// DLL入口
/// </summary>
/// <param name="hinstDLL"></param>
/// <param name="fdwReason"></param>
/// <param name="lpvReserved"></param>
/// <returns></returns>
BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved)
{
	if (fdwReason == DLL_PROCESS_ATTACH)
	{
		g_hInstance = hinstDLL;
	}
	return TRUE;
}

int CALLBACK wWinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, PWSTR lpCmdLine, int nCmdShow)
{
	int retCode = 0;

	// Set the working directory to the path of the executable.
	WCHAR path[MAX_PATH];
	HMODULE hModule = GetModuleHandleW(NULL);
	if (GetModuleFileNameW(hModule, path, MAX_PATH) > 0)
	{
		::PathRemoveFileSpecW(path);
		::SetCurrentDirectoryW(path);
	}
#if defined(_DEBUG)
	Debug::Initialize("Logs");
	std::wstring pathWStr = std::wstring(path);
	std::string pathStr = std::string(pathWStr.begin(), pathWStr.end());
	Debug::Log("工作目录：" + pathStr);
	try
	{
#endif
		Application::Create(hInstance);
		{
			std::shared_ptr<SampleScene> demo = std::make_shared<SampleScene>(L"Learning DirectX 12 - Lesson 2", 1280, 720);
			//demo->Initialize();
			retCode = Application::Get().Run(demo);
			//Debug::Log("retCode: " + std::to_string(retCode));
			//WindowPtr window = Application::Get().GetWindowByName(L"Learning DirectX 12 - Lesson 2");
			//Debug::Log("WindowFind: " + std::to_string(window == nullptr));
			//std::wstring wndName = window->GetWindowName();
			//Debug::Log("WindowName: " + std::string(wndName.begin(), wndName.end()));
			//Application::RemoveWindow(window->GetWindowHandle());
			//demo->Destroy();
		}
		Application::Destroy();
#if defined(_DEBUG)
	}
	catch (const std::exception& e)
	{
		Debug::LogException(e);
	}
	Debug::Shutdown();
#endif

	atexit(&ReportLiveObjects);

	::MessageBox(nullptr, "XEngine be Quited", "Info", MB_OK);

	return retCode;
}

XENGINE_API void StartXEngine(HWND parentWnd, LPWSTR workDir)
{
	int retCode = 0;

	// Set the working directory to the path of the executable.
	WCHAR path[MAX_PATH];
	if (GetModuleFileNameW(g_hInstance, path, MAX_PATH) > 0)
	{
		::PathRemoveFileSpecW(path);
		::SetCurrentDirectoryW(path);
	}
#if defined(_DEBUG)
	Debug::Initialize("Logs");
	__try
	{
		try
		{
#endif
			Application::Create(g_hInstance, parentWnd);
			std::shared_ptr<SampleScene> sampleScene = std::make_shared<SampleScene>(L"XEngine DX12", 1280, 720);
			retCode = Application::Get().Run(sampleScene);

			Application::Destroy();
#if defined(_DEBUG)
		}
		catch (const std::exception& e)
		{
			Debug::LogException(e);
		}
		catch (...)
		{
			Debug::LogException("Unknown Exception");
		}
	}
	__except (EXCEPTION_EXECUTE_HANDLER)
	{
		Debug::LogException("Structured exception occurred!");
	}
	Debug::Shutdown();
#endif

	atexit(&ReportLiveObjects);

	if (workDir != nullptr)
	{
		::SetCurrentDirectoryW(workDir);
	}
	::MessageBox(nullptr, "XEngine be Quited", "Info", MB_OK);
}

Engine::Engine(const std::wstring& name, int width, int height, bool vSync, bool raster)
	: m_Name(name)
	, m_Width(width)
	, m_Height(height)
	, m_vSync(vSync)
	, m_Raster(raster)
{
}

Engine::~Engine()
{
	assert(!m_pWindow && "Use Engine::Destroy() before destruction.");
}

bool Engine::Initialize()
{
	// Check for DirectX Math library support
	if (!DirectX::XMVerifyCPUSupport())
	{
		::MessageBoxA(NULL, "Failed to verify DirectX Math library support.", "Error", MB_OK | MB_ICONERROR);
		return false;
	}
	m_pWindow = Application::Get().CreateRenderWindow(m_Name, m_Width, m_Height, m_vSync);
	m_pWindow->RegisterCallbacks(shared_from_this());
	m_pWindow->Show();

	return true;
}

void Engine::Destroy()
{
	Application::Get().DestroyWindow(m_pWindow);
	m_pWindow.reset();
}

void Engine::OnUpdate(UpdateEventArgs& e)
{
}

void Engine::OnRender(RenderEventArgs& e)
{
}

void Engine::OnKeyboardDown(KeyEventArgs& e)
{
}

void Engine::OnKeyboardUp(KeyEventArgs& e)
{
}

void Engine::OnMouseMoved(MouseMotionEventArgs& e)
{
}

void Engine::OnMouseButtonDown(MouseButtonEventArgs& e)
{
}

void Engine::OnMouseButtonUp(MouseButtonEventArgs& e)
{
}

void Engine::OnMouseWheel(MouseWheelEventArgs& e)
{
}

void Engine::OnResize(ResizeEventArgs& e)
{
	m_Width = e.Width;
	m_Height = e.Height;
}

void Engine::OnWindowDestroy()
{
	// If the Window which we are registered to is 
	// destroyed, then any resources which are associated 
	// to the window must be released.
	UnloadContent();
}