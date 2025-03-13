#pragma once

#include <Engine.h>

#include <memory>

#include <d3d12.h>
#include <Window.h>
#include <DirectXMath.h>
#include "external/DXRHelpers/nv_helpers_dx12/TopLevelASGenerator.h"
#include "external/DXRHelpers/nv_helpers_dx12/ShaderBindingTableGenerator.h"

struct AccelerationStructureBuffers
{
	ComPtr<ID3D12Resource> pScratch; // Scratch memory for AS builder
	ComPtr<ID3D12Resource> pResult; // Where the AS is
	ComPtr<ID3D12Resource> pInstanceDesc; // Hold the matrices of the instances
};

class RaytracingPipeline : public std::enable_shared_from_this<RaytracingPipeline>
{
public:
	RaytracingPipeline(Engine* engine);
	~RaytracingPipeline();

	bool Initialize();

	void LoadContent();

	void OnResize();

	void OnRender(ComPtr<ID3D12GraphicsCommandList4> commandList, D3D12_CPU_DESCRIPTOR_HANDLE rtv, ComPtr<ID3D12Resource> backBuffer);

	void UnloadContent();

	void Destroy();

private:
	AccelerationStructureBuffers CreateBottomLevelAS(ComPtr<ID3D12Device5> device, ComPtr<ID3D12GraphicsCommandList4> commandList, std::vector <std::pair<ComPtr<ID3D12Resource>, uint32_t>> vVertexBuffers);
	void CreateTopLevelAS(ComPtr<ID3D12Device5> device, ComPtr<ID3D12GraphicsCommandList4> commandList, const std::vector<std::pair<ComPtr<ID3D12Resource>, DirectX::XMMATRIX>>& instances);
	void CreateAccelerationStructures();

	ComPtr<ID3D12RootSignature> CreateRayGenSignature();
	ComPtr<ID3D12RootSignature> CreateMissSignature();
	ComPtr<ID3D12RootSignature> CreateHitSignature();

	void CreateRaytracingPipline();

	void CreateRaytracingOutputBuffer();
	void CreateRaytracingResourceHeap();
	void CreateRaytracingResourceView();

	void CreateRaytracingShaderBindingTable();

	bool m_ContentLoaded;

	Engine* m_Engine;

	AccelerationStructureBuffers m_BottomLevelASBuffers;
	nv_helpers_dx12::TopLevelASGenerator m_TopLevelASGenerator;
	AccelerationStructureBuffers m_TopLevelASBuffers;
	std::vector<std::pair<ComPtr<ID3D12Resource>, DirectX::XMMATRIX>> m_Instances;

	ComPtr<IDxcBlob> m_RayGenLibrary;
	ComPtr<IDxcBlob> m_HitLibrary;
	ComPtr<IDxcBlob> m_MissLibrary;

	ComPtr<ID3D12RootSignature> m_RayGenSignature;
	ComPtr<ID3D12RootSignature> m_HitSignature;
	ComPtr<ID3D12RootSignature> m_MissSignature;

	// Ray tracing pipeline state
	ComPtr<ID3D12StateObject> m_RTStateObject;
	// Ray tracing pipeline state properties, retaining the shader identifiers
	// to use int the Shader Binding Table
	ComPtr<ID3D12StateObjectProperties> m_RTStateObjectProps;

	ComPtr<ID3D12Resource> m_OutputResource;
	ComPtr<ID3D12DescriptorHeap> m_SRV_UAV_Heap;

	nv_helpers_dx12::ShaderBindingTableGenerator m_SBTHelper;
	ComPtr<ID3D12Resource> m_SBTStorage;
};