#pragma once

#define XENGINE_API __declspec(dllexport)

#include <Events.h>
#include <Camera.h>

#include <memory>
#include <string>

#include <Window.h>

#include <dxcapi.h>
#include <DirectXMath.h>
#include <vector>
#include <cmath>

extern "C" {
	XENGINE_API void StartXEngine(HWND parentHWnd = nullptr, LPWSTR workDir = nullptr);
}

struct VertexPosColor
{
	DirectX::XMFLOAT3 Position;
	DirectX::XMFLOAT4 COlor;
};

//static VertexPosColor g_Vertices[8] = {
//    { XMFLOAT3(-1.0f, -1.0f, -1.0f), XMFLOAT4(0.0f, 0.0f, 0.0f, 1.0f) }, // 0
//    { XMFLOAT3(-1.0f,  1.0f, -1.0f), XMFLOAT4(0.0f, 1.0f, 0.0f, 1.0f) }, // 1
//    { XMFLOAT3(1.0f,  1.0f, -1.0f), XMFLOAT4(1.0f, 1.0f, 0.0f, 1.0f) }, // 2
//    { XMFLOAT3(1.0f, -1.0f, -1.0f), XMFLOAT4(1.0f, 0.0f, 0.0f, 1.0f) }, // 3
//    { XMFLOAT3(-1.0f, -1.0f,  1.0f), XMFLOAT4(0.0f, 0.0f, 1.0f, 1.0f) }, // 4
//    { XMFLOAT3(-1.0f,  1.0f,  1.0f), XMFLOAT4(0.0f, 1.0f, 1.0f, 1.0f) }, // 5
//    { XMFLOAT3(1.0f,  1.0f,  1.0f), XMFLOAT4(1.0f, 1.0f, 1.0f, 1.0f) }, // 6
//    { XMFLOAT3(1.0f, -1.0f,  1.0f), XMFLOAT4(1.0f, 0.0f, 1.0f, 1.0f) }  // 7
//};

static VertexPosColor g_Vertices[4] = {
	{ DirectX::XMFLOAT3(std::sqrtf(8.f / 9.f), 0.f, -1.f / 3.f), DirectX::XMFLOAT4(0.0f, 0.0f, 0.0f, 1.0f) }, // 0
	{ DirectX::XMFLOAT3(-std::sqrtf(2.f / 9.f), std::sqrtf(2.f / 3.f), -1.f / 3.f), DirectX::XMFLOAT4(0.0f, 1.0f, 0.0f, 1.0f) }, // 1
	{ DirectX::XMFLOAT3(-std::sqrtf(2.f / 9.f), -std::sqrtf(2.f / 3.f), -1.f / 3.f), DirectX::XMFLOAT4(1.0f, 1.0f, 0.0f, 1.0f) }, // 2
	{ DirectX::XMFLOAT3(0.f, 0.f, 1.f), DirectX::XMFLOAT4(1.0f, 0.0f, 0.0f, 1.0f) }, // 3
};

static uint32_t g_Indicies[12] =
{
	0, 1, 2, 0, 3, 1, 0, 2, 3, 1, 3, 2
};

class Engine : public std::enable_shared_from_this<Engine>
{
public:
	Engine(const std::wstring& name, int width, int height, bool vSync, bool raster);
	virtual ~Engine();

	virtual bool Initialize();

	virtual bool LoadContent() = 0;

	virtual void UnloadContent() = 0;

	virtual void Destroy();

	int GetWidth();
	int GetHeight();

	ComPtr<ID3D12Resource> GetVertexBuffer();
	ComPtr<ID3D12Resource> GetIndexBuffer();

	D3D12_VERTEX_BUFFER_VIEW* GetVertexBufferView();
	D3D12_INDEX_BUFFER_VIEW* GetIndexBufferView();

	std::shared_ptr<Camera> GetCamera();

	/// <summary>
	/// Transition a resource
	/// </summary>
	/// <param name="commandList"></param>
	/// <param name="resource"></param>
	/// <param name="beforeState"></param>
	/// <param name="afterState"></param>
	void TransitionResource(ComPtr<ID3D12GraphicsCommandList2> commandList, ComPtr<ID3D12Resource> resource, D3D12_RESOURCE_STATES beforeState, D3D12_RESOURCE_STATES afterState);

	/// <summary>
	/// Clear a render target view
	/// </summary>
	/// <param name="commandList"></param>
	/// <param name="rtv"></param>
	/// <param name="clearColor"></param>
	void ClearRTV(ComPtr<ID3D12GraphicsCommandList2> commandList, D3D12_CPU_DESCRIPTOR_HANDLE rtv, FLOAT* clearColor);

	/// <summary>
	/// Create a GPU buffer
	/// </summary>
	/// <param name="commandList"></param>
	/// <param name="pDestinationResource"></param>
	/// <param name="pIntermediateResource"></param>
	/// <param name="numElements"></param>
	/// <param name="elementSize"></param>
	/// <param name="bufferData"></param>
	/// <param name="flags"></param>
	void UpdateBufferResource(ComPtr<ID3D12GraphicsCommandList2> commandList, ID3D12Resource** pDestinationResource, ID3D12Resource** pIntermediateResource,
		size_t numElements, size_t elementSize, const void* bufferData, D3D12_RESOURCE_FLAGS flags = D3D12_RESOURCE_FLAG_NONE);

protected:
	friend class Window;

	void ResizeDepthBuffer(int width, int height);

	/// <summary>
	/// update game logic frame
	/// </summary>
	/// <param name="e"></param>
	virtual void OnUpdate(UpdateEventArgs& e);
	/// <summary>
	/// update render frame
	/// </summary>
	/// <param name="e"></param>
	virtual void OnRender(RenderEventArgs& e);
	/// <summary>
	/// Invoked by the registered window when a key is pressed
	/// while the window has focus
	/// </summary>
	/// <param name="e"></param>
	virtual void OnKeyboardDown(KeyEventArgs& e);
	/// <summary>
	/// Invoked when a key on the keyboard is released.
	/// </summary>
	/// <param name="e"></param>
	virtual void OnKeyboardUp(KeyEventArgs& e);
	/// <summary>
	/// Invoked when the mouse is moved over the registered window
	/// </summary>
	/// <param name="e"></param>
	virtual void OnMouseMoved(MouseMotionEventArgs& e);
	/// <summary>
	/// Invoked when the mouse button is pressed over the registered window
	/// </summary>
	/// <param name="e"></param>
	virtual void OnMouseButtonDown(MouseButtonEventArgs& e);
	/// <summary>
	/// Invoked when the mouse button is released over the registered window
	/// </summary>
	/// <param name="e"></param>
	virtual void OnMouseButtonUp(MouseButtonEventArgs& e);
	/// <summary>
	/// Invoked when the mouse wheel is scrolled while the registered window
	/// </summary>
	/// <param name="e"></param>
	virtual void OnMouseWheel(MouseWheelEventArgs& e);
	/// <summary>
	/// Invoked when the attached window is resized
	/// </summary>
	/// <param name="e"></param>
	virtual void OnResize(ResizeEventArgs& e);
	/// <summary>
	/// Invoked when the registered window instance is destroyed
	/// </summary>
	virtual void OnWindowDestroy();

	std::shared_ptr<Window> m_pWindow;

	// Depth buffer
	ComPtr<ID3D12Resource> m_DepthBuffer;
	ComPtr<ID3D12DescriptorHeap> m_DSVHeap;

	uint64_t m_FenceValues[Window::BackBufferCount] = {};

	int m_Width;
	int m_Height;

	std::shared_ptr<Camera> m_Camera;

	// Vertex buffer for the cube
	ComPtr<ID3D12Resource> m_VertexBuffer;
	D3D12_VERTEX_BUFFER_VIEW m_VertexBufferView;
	// Index buffer for the cube
	ComPtr<ID3D12Resource> m_IndexBuffer;
	D3D12_INDEX_BUFFER_VIEW m_IndexBufferView;

	bool m_ContentLoaded;

	bool m_Raster = true;

private:
	std::wstring m_Name;
	bool m_vSync;
};