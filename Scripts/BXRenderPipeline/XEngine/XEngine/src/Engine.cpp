#define WIN32_LEAN_AND_MEAN
#include <Windows.h>
#include <Shlwapi.h>


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
//BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved)
//{
//	if (fdwReason == DLL_PROCESS_ATTACH)
//	{
//		g_hInstance = hinstDLL;
//	}
//	return TRUE;
//}

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

	Debug::Initialize("Logs");
	try
	{
		Application::Create(hInstance);
		{
			std::shared_ptr<SampleScene> demo = std::make_shared<SampleScene>(L"Learning DirectX 12 - Lesson 2", 1280, 720);
			retCode = Application::Get().Run(demo);
		}
		Application::Destroy();
	}
	catch (const std::exception& e)
	{
		Debug::LogException(e);
	}

	Debug::Shutdown();
	atexit(&ReportLiveObjects);

	::MessageBox(nullptr, "XEngine be Quited", "Info", MB_OK);

	return retCode;
}

XENGINE_API void StartXEngine(HWND parentWnd = nullptr)
{
	//int retCode = 0;

	//// Set the working directory to the path of the executable.
	//WCHAR path[MAX_PATH];
	//HMODULE hModule = GetModuleHandleW(NULL);
	//if (GetModuleFileNameW(hModule, path, MAX_PATH) > 0)
	//{
	//	::PathRemoveFileSpecW(path);
	//	::SetCurrentDirectoryW(path);
	//}

	//Application::Create(g_hInstance);
	//std::shared_ptr<SampleScene> sampleScene = std::make_shared<SampleScene>(L"XEngine DX12", 1280, 720);
	//::MessageBox(nullptr, "Create Application and SampleScene", "Info", MB_OK);
	//retCode = Application::Get().Run(sampleScene);

	//Application::Destroy();

	//atexit(&ReportLiveObjects);

	//::MessageBox(nullptr, "XEngine be Quited", "Info", MB_OK);
}



Engine::Engine(const std::wstring& name, int width, int height, bool vSync)
	: m_Name(name)
	, m_Width(width)
	, m_Height(height)
	, m_vSync(vSync)
{
}

Engine::~Engine()
{
	assert(!m_pWindow && "Use Game::Destroy() before destruction.");
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