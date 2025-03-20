#pragma once
#include <Engine.h>

#include <Window.h>

#include <memory>

class RasterPipeline : public std::enable_shared_from_this<RasterPipeline>
{
public:
	RasterPipeline(Engine* engine);
	~RasterPipeline();

	bool Initialize();

	void LoadContent();

	void OnResize();

	void OnRender(ComPtr<ID3D12GraphicsCommandList4> commandList, D3D12_CPU_DESCRIPTOR_HANDLE rtv, D3D12_CPU_DESCRIPTOR_HANDLE dsv);

	void UnloadContent();

	void Destroy();

private:
	/// <summary>
	/// Clear the depth of a depth-stencil view
	/// </summary>
	/// <param name="commandList"></param>
	/// <param name="dsv"></param>
	/// <param name="depth"></param>
	void ClearDepth(ComPtr<ID3D12GraphicsCommandList2> commandList, D3D12_CPU_DESCRIPTOR_HANDLE dsv, FLOAT depth = 1.0f);

	Engine* m_Engine;

	// Root signature
	ComPtr<ID3D12RootSignature> m_RootSignature;

	// Pipeline state object
	ComPtr<ID3D12PipelineState> m_PipelineState;

	bool m_ContentLoaded;

};