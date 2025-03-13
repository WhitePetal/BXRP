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

using namespace DirectX;

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
	ComPtr<IDXGIDebug1> dxgiDebug;
	DXGIGetDebugInterface1(0, IID_PPV_ARGS(&dxgiDebug));

	dxgiDebug->ReportLiveObjects(DXGI_DEBUG_ALL, DXGI_DEBUG_RLO_FLAGS(DXGI_DEBUG_RLO_SUMMARY | DXGI_DEBUG_RLO_IGNORE_INTERNAL));
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
			retCode = Application::Get().Run(demo);
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
	//__try
	//{
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
	//}
	//__except (EXCEPTION_EXECUTE_HANDLER)
	//{
	//	Debug::LogException("Structured exception occurred!");
	//}
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

int Engine::GetWidth()
{
	return m_Width;
}

int Engine::GetHeight()
{
	return m_Height;
}

ComPtr<ID3D12Resource> Engine::GetVertexBuffer()
{
	return m_VertexBuffer;
}
ComPtr<ID3D12Resource> Engine::GetIndexBuffer()
{
	return m_IndexBuffer;
}

D3D12_VERTEX_BUFFER_VIEW* Engine::GetVertexBufferView()
{
	return &m_VertexBufferView;
}
D3D12_INDEX_BUFFER_VIEW* Engine::GetIndexBufferView()
{
	return &m_IndexBufferView;
}

DirectX::XMMATRIX Engine::GetMVPMatrix()
{
	XMMATRIX mvpMatrix = XMMatrixMultiply(m_ModelMatrix, m_ViewMatrix);
	mvpMatrix = XMMatrixMultiply(mvpMatrix, m_ProjectionMatrix);
	return mvpMatrix;
}

void Engine::UpdateBufferResource(
	ComPtr<ID3D12GraphicsCommandList2> commandList,
	ID3D12Resource** pDestinationResource,
	ID3D12Resource** pIntermediatedResource,
	size_t numElements, size_t elementSize, const void* bufferData,
	D3D12_RESOURCE_FLAGS flags
)
{
	auto device = Application::Get().GetDevice();
	size_t bufferSize = numElements * elementSize;

	// Create a committed resource for the CPU resource in a default heap
	// default heap 的内存是 GPU内存, CPU 无法直接写入
	CD3DX12_HEAP_PROPERTIES defaultHeapProperties(D3D12_HEAP_TYPE_DEFAULT);
	CD3DX12_RESOURCE_DESC bufferDesc = CD3DX12_RESOURCE_DESC::Buffer(bufferSize, flags);
	ThrowIfFaild(device->CreateCommittedResource(
		&defaultHeapProperties,
		D3D12_HEAP_FLAG_NONE,
		&bufferDesc,
		D3D12_RESOURCE_STATE_COMMON,
		nullptr,
		IID_PPV_ARGS(pDestinationResource)
	));

	// Create an committted resource for the upload.
	// upload heap 的内存是 系统内存，CPU可以写入
	// 之后通过 commandlist 发起指令让 GPU 从这里复制数据
	if (bufferData)
	{
		CD3DX12_HEAP_PROPERTIES uploadHeapProperties(D3D12_HEAP_TYPE_UPLOAD);
		ThrowIfFaild(device->CreateCommittedResource(
			&uploadHeapProperties,
			D3D12_HEAP_FLAG_NONE,
			&bufferDesc,
			D3D12_RESOURCE_STATE_GENERIC_READ,
			nullptr,
			IID_PPV_ARGS(pIntermediatedResource)
		));

		D3D12_SUBRESOURCE_DATA subresourceData = {};
		subresourceData.pData = bufferData;
		subresourceData.RowPitch = bufferSize;
		subresourceData.SlicePitch = subresourceData.RowPitch;

		UpdateSubresources(commandList.Get(), *pDestinationResource, *pIntermediatedResource, 0, 0, 1, &subresourceData);
	}
}

void Engine::TransitionResource(ComPtr<ID3D12GraphicsCommandList2> commandList, ComPtr<ID3D12Resource> resource, D3D12_RESOURCE_STATES beforeState, D3D12_RESOURCE_STATES afterState)
{
	CD3DX12_RESOURCE_BARRIER barrier = CD3DX12_RESOURCE_BARRIER::Transition(resource.Get(), beforeState, afterState);
	commandList->ResourceBarrier(1, &barrier);
}

void Engine::ClearRTV(ComPtr<ID3D12GraphicsCommandList2> commandList, D3D12_CPU_DESCRIPTOR_HANDLE rtv, FLOAT* clearColor)
{
	commandList->ClearRenderTargetView(rtv, clearColor, 0, nullptr);
}

void Engine::ResizeDepthBuffer(int width, int height)
{
	if (m_ContentLoaded)
	{
		// Flush any GPU commands that might be referencing the depth buffer.
		Application::Get().Flush();

		width = std::max(1, width);
		height = std::max(1, height);

		auto device = Application::Get().GetDevice();
		// Resize screen dependent reoures.
		// Create a depth buffer.
		D3D12_CLEAR_VALUE optimizedClearValue = {};
		optimizedClearValue.Format = DXGI_FORMAT_D32_FLOAT;
		optimizedClearValue.DepthStencil = { 1.0f, 0 };

		CD3DX12_HEAP_PROPERTIES heapProerties(D3D12_HEAP_TYPE_DEFAULT);
		CD3DX12_RESOURCE_DESC tex2DResDesc = CD3DX12_RESOURCE_DESC::Tex2D(DXGI_FORMAT_D32_FLOAT, width, height,
			1, 0, 1, 0, D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL);
		ThrowIfFaild(device->CreateCommittedResource(
			&heapProerties,
			D3D12_HEAP_FLAG_NONE,
			&tex2DResDesc,
			D3D12_RESOURCE_STATE_DEPTH_WRITE,
			&optimizedClearValue,
			IID_PPV_ARGS(&m_DepthBuffer)
		));

		// Update the depth-stencil view
		D3D12_DEPTH_STENCIL_VIEW_DESC dsv = {};
		dsv.Format = DXGI_FORMAT_D32_FLOAT;
		dsv.ViewDimension = D3D12_DSV_DIMENSION_TEXTURE2D;
		dsv.Texture2D.MipSlice = 0;
		dsv.Flags = D3D12_DSV_FLAG_NONE;

		device->CreateDepthStencilView(m_DepthBuffer.Get(), &dsv,
			m_DSVHeap->GetCPUDescriptorHandleForHeapStart());
	}
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