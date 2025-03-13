#include <RasterPipeline.h>
#include <Helpers.h>

#include <wrl.h>
using namespace Microsoft::WRL;

#include <CommandQueue.h>

#include <d3dx12.h>
#include <d3dcompiler.h>

using namespace DirectX;


RasterPipeline::RasterPipeline(Engine* engine) : m_Engine(engine)
{
    m_ContentLoaded = false;
}

RasterPipeline::~RasterPipeline()
{
	Destroy();
}

inline bool RasterPipeline::Initialize()
{
	return true;
}

void RasterPipeline::LoadContent()
{
    Application& app = Application::Get();
    auto device = app.GetDevice();

    // Load the vertex shader
    ComPtr<ID3DBlob> vertexShaderBlob;
    ThrowIfFaild(D3DReadFileToBlob(L"VertexShader.cso", &vertexShaderBlob));

    // Load the pixel shader
    ComPtr<ID3DBlob> pixelShaderBlob;
    ThrowIfFaild(D3DReadFileToBlob(L"PixelShader.cso", &pixelShaderBlob));

    // Create the vertex input layout
    D3D12_INPUT_ELEMENT_DESC inputLayout[] = {
        {"POSITION", 0, DXGI_FORMAT_R32G32B32_FLOAT, 0, D3D12_APPEND_ALIGNED_ELEMENT, D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, 0},
        {"COLOR", 0, DXGI_FORMAT_R32G32B32_FLOAT, 0, D3D12_APPEND_ALIGNED_ELEMENT, D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, 0}
    };

    // Create a root signature.
    D3D12_FEATURE_DATA_ROOT_SIGNATURE featureData = {};
    featureData.HighestVersion = D3D_ROOT_SIGNATURE_VERSION_1_1;
    if (FAILED(device->CheckFeatureSupport(D3D12_FEATURE_ROOT_SIGNATURE, &featureData, sizeof(featureData))))
    {
        featureData.HighestVersion = D3D_ROOT_SIGNATURE_VERSION_1_0;
    }

    // Allow input layout and deny unnecessary access to certain pipeline stages
    D3D12_ROOT_SIGNATURE_FLAGS rootSignatureFlags =
        D3D12_ROOT_SIGNATURE_FLAG_ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT |
        D3D12_ROOT_SIGNATURE_FLAG_DENY_HULL_SHADER_ROOT_ACCESS |
        D3D12_ROOT_SIGNATURE_FLAG_DENY_DOMAIN_SHADER_ROOT_ACCESS |
        D3D12_ROOT_SIGNATURE_FLAG_DENY_GEOMETRY_SHADER_ROOT_ACCESS |
        D3D12_ROOT_SIGNATURE_FLAG_DENY_PIXEL_SHADER_ROOT_ACCESS;

    // Camera Matrixs
    CD3DX12_ROOT_PARAMETER1 rootParameter;
    CD3DX12_DESCRIPTOR_RANGE1 range;
    range.Init(D3D12_DESCRIPTOR_RANGE_TYPE_CBV, 1, 0);
    rootParameter.InitAsDescriptorTable(1, &range, D3D12_SHADER_VISIBILITY_VERTEX);

    CD3DX12_VERSIONED_ROOT_SIGNATURE_DESC rootSignatureDescription;
    rootSignatureDescription.Init_1_1(1, &rootParameter, 0, nullptr, D3D12_ROOT_SIGNATURE_FLAG_ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT);

    // Serialize the root signature
    ComPtr<ID3DBlob> rootSignatureBlob;
    ComPtr<ID3DBlob> errorBlob;
    ThrowIfFaild(D3DX12SerializeVersionedRootSignature(&rootSignatureDescription, featureData.HighestVersion, &rootSignatureBlob, &errorBlob));
    // Create the root signature.
    ThrowIfFaild(device->CreateRootSignature(0, rootSignatureBlob->GetBufferPointer(), rootSignatureBlob->GetBufferSize(), IID_PPV_ARGS(&m_RootSignature)));

    struct PipelineStateStream
    {
        CD3DX12_PIPELINE_STATE_STREAM_ROOT_SIGNATURE pRootSignature;
        CD3DX12_PIPELINE_STATE_STREAM_INPUT_LAYOUT InputLayout;
        CD3DX12_PIPELINE_STATE_STREAM_PRIMITIVE_TOPOLOGY PrimitiveTopologyType;
        CD3DX12_PIPELINE_STATE_STREAM_VS VS;
        CD3DX12_PIPELINE_STATE_STREAM_PS PS;
        CD3DX12_PIPELINE_STATE_STREAM_DEPTH_STENCIL_FORMAT DSVFormat;
        CD3DX12_PIPELINE_STATE_STREAM_RENDER_TARGET_FORMATS RTVFormats;
    } pipelineStateStream;

    D3D12_RT_FORMAT_ARRAY rtvFormats = {};
    rtvFormats.NumRenderTargets = 1;
    rtvFormats.RTFormats[0] = DXGI_FORMAT_R8G8B8A8_UNORM;

    pipelineStateStream.pRootSignature = m_RootSignature.Get();
    pipelineStateStream.InputLayout = { inputLayout, _countof(inputLayout) };
    pipelineStateStream.PrimitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE;
    pipelineStateStream.VS = CD3DX12_SHADER_BYTECODE(vertexShaderBlob.Get());
    pipelineStateStream.PS = CD3DX12_SHADER_BYTECODE(pixelShaderBlob.Get());
    pipelineStateStream.DSVFormat = DXGI_FORMAT_D32_FLOAT;
    pipelineStateStream.RTVFormats = rtvFormats;

    D3D12_PIPELINE_STATE_STREAM_DESC pipelineStateStreamDesc = {
        sizeof(PipelineStateStream), &pipelineStateStream
    };
    ThrowIfFaild(device->CreatePipelineState(&pipelineStateStreamDesc, IID_PPV_ARGS(&m_PipelineState)));

    m_ContentLoaded = true;
}

void RasterPipeline::OnResize()
{
}

void RasterPipeline::OnRender(ComPtr<ID3D12GraphicsCommandList4> commandList, D3D12_CPU_DESCRIPTOR_HANDLE rtv, D3D12_CPU_DESCRIPTOR_HANDLE dsv)
{
    commandList->SetPipelineState(m_PipelineState.Get());
    commandList->SetGraphicsRootSignature(m_RootSignature.Get());

    // Clear the render targets
    FLOAT clearColor[] = { 0.4f, 0.6f, 0.9f, 1.0f };
    m_Engine->ClearRTV(commandList, rtv, clearColor);
    ClearDepth(commandList, dsv);

    commandList->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
    commandList->IASetVertexBuffers(0, 1, m_Engine->GetVertexBufferView());
    commandList->IASetIndexBuffer(m_Engine->GetIndexBufferView());

    // Update the Camera Matrixs
    ComPtr<ID3D12DescriptorHeap> camerConstHeap = m_Engine->GetCamera()->GetConstHeap();
    std::vector<ID3D12DescriptorHeap*> heaps = { camerConstHeap.Get() };
    commandList->SetDescriptorHeaps(static_cast<UINT>(heaps.size()), heaps.data());
    commandList->SetGraphicsRootDescriptorTable(
        0, camerConstHeap->GetGPUDescriptorHandleForHeapStart()
    );

    commandList->DrawIndexedInstanced(_countof(g_Indicies), 1, 0, 0, 0);
}

void RasterPipeline::UnloadContent()
{

}

void RasterPipeline::Destroy()
{
	UnloadContent();
}

void RasterPipeline::ClearDepth(ComPtr<ID3D12GraphicsCommandList2> commandList, D3D12_CPU_DESCRIPTOR_HANDLE dsv, FLOAT depth)
{
    commandList->ClearDepthStencilView(dsv, D3D12_CLEAR_FLAG_DEPTH, depth, 0, 0, nullptr);
}