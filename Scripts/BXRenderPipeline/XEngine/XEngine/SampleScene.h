#pragma once

#include <d3d12.h>

#include <Engine.h>
#include <Window.h>

#include <DirectXMath.h>

class SampleScene : public Engine
{
public:
	using super = Engine;

	SampleScene(const std::wstring& name, int width, int height, bool vSync = false, HWND parentWnd = NULL);

	virtual bool LoadContent() override;

	virtual void UnloadContent() override;

protected:
	virtual void OnUpdate(UpdateEventArgs& e) override;
	virtual void OnRender(RenderEventArgs& e) override;
	virtual void OnKeyboardDown(KeyEventArgs& e) override;
	virtual void OnMouseWheel(MouseWheelEventArgs& e) override;
	virtual void OnResize(ResizeEventArgs& e) override;

private:
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
	/// Clear the depth of a depth-stencil view
	/// </summary>
	/// <param name="commandList"></param>
	/// <param name="dsv"></param>
	/// <param name="depth"></param>
	void ClearDepth(ComPtr<ID3D12GraphicsCommandList2> commandList, D3D12_CPU_DESCRIPTOR_HANDLE dsv, FLOAT depth = 1.0f);

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

	/// <summary>
	/// Resize the depth buffer to match the size of the client area
	/// </summary>
	/// <param name="width"></param>
	/// <param name="height"></param>
	void ResizeDepthBuffer(int width, int height);

	// Vertex buffer for the cube
	ComPtr<ID3D12Resource> m_VertexBuffer;
	D3D12_VERTEX_BUFFER_VIEW m_VertexBufferView;
	// Index buffer for the cube
	ComPtr<ID3D12Resource> m_IndexBuffer;
	D3D12_INDEX_BUFFER_VIEW m_IndexBufferView;
	// Depth buffer
	ComPtr<ID3D12Resource> m_DepthBuffer;
	ComPtr<ID3D12DescriptorHeap> m_DSVHeap;

	// Root signature
	ComPtr<ID3D12RootSignature> m_RootSignature;

	// Pipeline state object
	ComPtr<ID3D12PipelineState> m_PipelineState;

	D3D12_VIEWPORT m_Viewport;
	D3D12_RECT m_ScissorRect;

	float m_FoV;

	DirectX::XMMATRIX m_ModelMatrix;
	DirectX::XMMATRIX m_ViewMatrix;
	DirectX::XMMATRIX m_ProjectionMatrix;

	bool m_ContentLoaded;
};