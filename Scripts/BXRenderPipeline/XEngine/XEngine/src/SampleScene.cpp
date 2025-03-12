#include <SampleScene.h>

#include <Application.h>
#include <CommandQueue.h>
#include <Helpers.h>
#include <Window.h>

#include <wrl.h>
using namespace Microsoft::WRL;

#include <d3dx12.h>
#include <d3dcompiler.h>

// DXR
#include "external/DXRHelpers/DXRHelper.h"
#include "external/DXRHelpers/nv_helpers_dx12/BottomLevelASGenerator.h"
#include "external/DXRHelpers//nv_helpers_dx12/RaytracingPipelineGenerator.h"
#include "external/DXRHelpers/nv_helpers_dx12/RootSignatureGenerator.h"

#include <algorithm>
#if defined(min)
	#undef min
#endif
#if defined(max)
	#undef max
#endif

using namespace DirectX;


struct VertexPosColor
{
	XMFLOAT3 Position;
	XMFLOAT4 COlor;
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

static VertexPosColor g_Vertices[8] = {
    { XMFLOAT3(std::sqrtf(8.f / 9.f), 0.f, -1.f / 3.f), XMFLOAT4(0.0f, 0.0f, 0.0f, 1.0f) }, // 0
    { XMFLOAT3(-std::sqrtf(2.f / 9.f), std::sqrtf(2.f / 3.f), -1.f / 3.f), XMFLOAT4(0.0f, 1.0f, 0.0f, 1.0f) }, // 1
    { XMFLOAT3(-std::sqrtf(2.f / 9.f), -std::sqrtf(2.f / 3.f), -1.f / 3.f), XMFLOAT4(1.0f, 1.0f, 0.0f, 1.0f) }, // 2
    { XMFLOAT3(0.f, 0.f, 1.f), XMFLOAT4(1.0f, 0.0f, 0.0f, 1.0f) }, // 3
};

static WORD g_Indicies[36] =
{
    0, 1, 2, 0, 2, 3,
    4, 6, 5, 4, 7, 6,
    4, 5, 1, 4, 1, 0,
    3, 2, 6, 3, 6, 7,
    1, 5, 6, 1, 6, 2,
    4, 0, 3, 4, 3, 7
};

SampleScene::SampleScene(const std::wstring& name, int width, int height, bool vSync, bool raster)
    : super(name, width, height, vSync, raster)
    , m_ScissorRect(CD3DX12_RECT(0, 0, LONG_MAX, LONG_MAX))
    , m_Viewport(CD3DX12_VIEWPORT(0.0f, 0.0f, static_cast<float>(width), static_cast<float>(height)))
    , m_FoV(45.0)
{
}

void SampleScene::UpdateBufferResource(
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

bool SampleScene::LoadContent()
{
    Application& app = Application::Get();
    auto device = app.GetDevice();
    auto commandQueue = app.GetCommandQueue(D3D12_COMMAND_LIST_TYPE_COPY);
    auto commandList = commandQueue->GetCommandList();

    // Upload vertex buffer data.
    ComPtr<ID3D12Resource> intermediateVertexBuffer;
    UpdateBufferResource(
        commandList,
        &m_VertexBuffer,
        &intermediateVertexBuffer,
        _countof(g_Vertices), sizeof(VertexPosColor), g_Vertices);

    // Create the vertex buffer view.
    m_VertexBufferView.BufferLocation = m_VertexBuffer->GetGPUVirtualAddress();
    m_VertexBufferView.SizeInBytes = sizeof(g_Vertices);
    m_VertexBufferView.StrideInBytes = sizeof(VertexPosColor);

    // Upload index buffer data.
    ComPtr<ID3D12Resource> intermediateIndexBuffer;
    UpdateBufferResource(
        commandList,
        &m_IndexBuffer,
        &intermediateIndexBuffer,
        _countof(g_Indicies), sizeof(WORD), g_Indicies);

    // Create index buffer view.
    m_IndexBufferView.BufferLocation = m_IndexBuffer->GetGPUVirtualAddress();
    m_IndexBufferView.Format = DXGI_FORMAT_R16_UINT;
    m_IndexBufferView.SizeInBytes = sizeof(g_Indicies);

    // Create the descriptor heap for the depth-stencil view (DSV)
    D3D12_DESCRIPTOR_HEAP_DESC dsvHeapDesc = {};
    dsvHeapDesc.NumDescriptors = 1;
    dsvHeapDesc.Type = D3D12_DESCRIPTOR_HEAP_TYPE_DSV;
    dsvHeapDesc.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_NONE;
    ThrowIfFaild(device->CreateDescriptorHeap(&dsvHeapDesc, IID_PPV_ARGS(&m_DSVHeap)));

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
    // A single 32-bit constant root parameter that is used by the vertex shader
    CD3DX12_ROOT_PARAMETER1 rootParameters[1];
    rootParameters[0].InitAsConstants(sizeof
    (XMMATRIX) / 4, 0, 0, D3D12_SHADER_VISIBILITY_VERTEX);
    CD3DX12_VERSIONED_ROOT_SIGNATURE_DESC rootSignatureDescription;
    rootSignatureDescription.Init_1_1(_countof(rootParameters), rootParameters, 0, nullptr, rootSignatureFlags);
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

    auto fenceValue = commandQueue->ExecuteCommandList(commandList);
    commandQueue->WaitForFenceValue(fenceValue);

    // #DXR
    // Setup the acceleration structures (AS) for raytracing. When setting up
    // geometry, each bottom-level AS has its own transform matrix.
    CreateAccelerationStructures();
    // Create the raytracing pipeline, associating the shader code to symbol names
    // and to their root signatures, and defining the amount of memory carried by
    // rays (ray payload)
    CreateRaytracingPipline();
    CreateRaytracingResourceHeap();
    
    m_ContentLoaded = true;
    // Resize/Create the depth buffer
    ResizeDepthBuffer(m_Width, m_Height);

    return true;
}

void SampleScene::ResizeDepthBuffer(int width, int height)
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

        // #DXR
        CreateRaytracingOutputBuffer();
        CreateRaytracingResourceView();
        CreateRaytracingShaderBindingTable();
    }
}

AccelerationStructureBuffers SampleScene::CreateBottomLevelAS(ComPtr<ID3D12Device5> device, ComPtr<ID3D12GraphicsCommandList4> commandList, std::vector <std::pair<ComPtr<ID3D12Resource>, uint32_t>> vVertexBuffers)
{
    nv_helpers_dx12::BottomLevelASGenerator bottomLevelAS;

    // Adding all vertex buffers and not transforming their positon
    for (const auto& buffer : vVertexBuffers)
    {
        //bottomLevelAS.AddVertexBuffer(buffer.first.Get(), 0, buffer.second, sizeof(VertexPosColor), m_IndexBuffer.Get(), 0, _countof(g_Indicies), nullptr, 0, true);
        bottomLevelAS.AddVertexBuffer(buffer.first.Get(), 0, buffer.second, sizeof(VertexPosColor), 0, 0);
    }

    // The AS build requires some scratch space to store temporary information.
    // The amount of scratch memory is dependent on the scene complexity.
    UINT64 scratchSizeInBytes = 0;
    // The final AS also needs to be stored in addition to the existing vertex buffers.
    // It size is alos dependent on the scene complexity.
    UINT64 resultSizeInBytes = 0;

    bottomLevelAS.ComputeASBufferSizes(device.Get(), false, &scratchSizeInBytes, &resultSizeInBytes);

    // Once the size are obtained, the application is responsible for allocating the necessary buffers.
    // Since the entire generation will be done on the GPU, 
    // we can directly allocate those on the default heap
    AccelerationStructureBuffers buffers;
    buffers.pScratch = nv_helpers_dx12::CreateBuffer(
        device.Get(), scratchSizeInBytes,
        D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS,
        D3D12_RESOURCE_STATE_COMMON,
        nv_helpers_dx12::kDefaultHeapProps
    );
    buffers.pResult = nv_helpers_dx12::CreateBuffer(
        device.Get(), resultSizeInBytes,
        D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS,
        D3D12_RESOURCE_STATE_RAYTRACING_ACCELERATION_STRUCTURE,
        nv_helpers_dx12::kDefaultHeapProps
    );

    // Build the aceeleration structure. Note that this call integrates a barrier on the generated AS,
    // so that it can be used to compute a top-level AS right after this method.
    bottomLevelAS.Generate(commandList.Get(), buffers.pScratch.Get(), buffers.pResult.Get(), false, nullptr);

    return buffers;
}

void SampleScene::CreateTopLevelAS(ComPtr<ID3D12Device5> device, ComPtr<ID3D12GraphicsCommandList4> commandList, const std::vector<std::pair<ComPtr<ID3D12Resource>, XMMATRIX>>& instances)
{
    // Gather all the instances into the builder helper
    for (size_t i = 0; i < instances.size(); ++i)
    {
        m_TopLevelASGenerator.AddInstance(
            instances[i].first.Get(),
            instances[i].second,
            static_cast<UINT>(i), static_cast<UINT>(0));
    }

    // As for the bottom-levels AS, the building the AS requires some scratch space
    // to store stemporary data in addition to the actual AS.
    // In the case of the top-level AS, the instance descriptors also need to be stored in GPU memory.
    // This call ouputs the memory requirements for each (scratch, results, instance descriptors)
    // so that the application can allocate the corresponding memory
    UINT64 scratchSize, resultSize, instanceDescsSize;

    m_TopLevelASGenerator.ComputeASBufferSizes(device.Get(), true, &scratchSize, &resultSize, &instanceDescsSize);

    // Create the scratch and result buffers. Since the build is all done on GPU,
    // those can be allocated on the default heap
    m_TopLevelASBuffers.pScratch = nv_helpers_dx12::CreateBuffer(
        device.Get(), scratchSize,
        D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS,
        D3D12_RESOURCE_STATE_UNORDERED_ACCESS,
        nv_helpers_dx12::kDefaultHeapProps
    );
    m_TopLevelASBuffers.pResult = nv_helpers_dx12::CreateBuffer(
        device.Get(), resultSize,
        D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS,
        D3D12_RESOURCE_STATE_RAYTRACING_ACCELERATION_STRUCTURE,
        nv_helpers_dx12::kDefaultHeapProps
    );

    // The buffer describing the instances: ID, shader binding information, matrices ...
    // Those will be copied into the buffer by the helper through mapping,
    // so the buffer has to be allocated on the upload heap.
    m_TopLevelASBuffers.pInstanceDesc = nv_helpers_dx12::CreateBuffer(
        device.Get(), instanceDescsSize,
        D3D12_RESOURCE_FLAG_NONE,
        D3D12_RESOURCE_STATE_GENERIC_READ,
        nv_helpers_dx12::kUploadHeapProps
    );

    // After all the buffers are allocated, or if only an update is required,
    // we can build the acceleration structure. Note that in the case of the update
    // we also pass the existing AS as the 'previous' AS, so that it can be refitted in place.
    m_TopLevelASGenerator.Generate(
        commandList.Get(),
        m_TopLevelASBuffers.pScratch.Get(),
        m_TopLevelASBuffers.pResult.Get(),
        m_TopLevelASBuffers.pInstanceDesc.Get()
    );
}

void SampleScene::CreateAccelerationStructures()
{
    auto device = Application::Get().GetDevice();
    auto commandQueue = Application::Get().GetCommandQueue(D3D12_COMMAND_LIST_TYPE_DIRECT);
    auto commandList = commandQueue->GetCommandList();

    // Build the bottom AS
    m_BottomLevelASBuffers = CreateBottomLevelAS(device, commandList, { {m_VertexBuffer.Get(), _countof(g_Vertices)} });

    // Just one instance for now
    m_Instances = { {m_BottomLevelASBuffers.pResult, XMMatrixIdentity()} };
    CreateTopLevelAS(device, commandList, m_Instances);

    // Flush the command list and wait for it to finish
    auto fenceValue = commandQueue->ExecuteCommandList(commandList);
    commandQueue->WaitForFenceValue(fenceValue);
}

ComPtr<ID3D12RootSignature> SampleScene::CreateRayGenSignature()
{
    auto device = Application::Get().GetDevice();
    nv_helpers_dx12::RootSignatureGenerator rsc;
    rsc.AddHeapRangesParameter(
        {
            {0 /*u0*/, 1 /*1 descriptor*/, 0 /*use the implicit register space 0*/,
            D3D12_DESCRIPTOR_RANGE_TYPE_UAV, 0 /*heap slot where the UAV is defined*/},
            {0 /*t0*/, 1, 0,
            D3D12_DESCRIPTOR_RANGE_TYPE_SRV /*Top-level acceleration structure*/, 1}
        }
    );
    return rsc.Generate(device.Get(), true);
}

ComPtr<ID3D12RootSignature> SampleScene::CreateHitSignature()
{
    auto device = Application::Get().GetDevice();
    nv_helpers_dx12::RootSignatureGenerator rsc;
    rsc.AddRootParameter(D3D12_ROOT_PARAMETER_TYPE_SRV);
    return rsc.Generate(device.Get(), true);
}

ComPtr<ID3D12RootSignature> SampleScene::CreateMissSignature()
{
    auto device = Application::Get().GetDevice();
    nv_helpers_dx12::RootSignatureGenerator rsc;
    return rsc.Generate(device.Get(), true);
}

void SampleScene::CreateRaytracingPipline()
{
    auto device = Application::Get().GetDevice();
    nv_helpers_dx12::RayTracingPipelineGenerator pipeline(device.Get());

    m_RayGenLibrary = nv_helpers_dx12::CompileShaderLibrary(L"RayTracingShaders/RayGen.hlsl");
    m_MissLibrary = nv_helpers_dx12::CompileShaderLibrary(L"RayTracingShaders/Miss.hlsl");
    m_HitLibrary = nv_helpers_dx12::CompileShaderLibrary(L"RayTracingShaders/Hit.hlsl");

    pipeline.AddLibrary(m_RayGenLibrary.Get(), { L"RayGen" });
    pipeline.AddLibrary(m_MissLibrary.Get(), { L"Miss" });
    pipeline.AddLibrary(m_HitLibrary.Get(), { L"ClosestHit" });

    m_RayGenSignature = CreateRayGenSignature();
    m_MissSignature = CreateMissSignature();
    m_HitSignature = CreateHitSignature();

    pipeline.AddHitGroup(L"HitGroup", L"ClosestHit");

    pipeline.AddRootSignatureAssociation(m_RayGenSignature.Get(), { L"RayGen" });
    pipeline.AddRootSignatureAssociation(m_MissSignature.Get(), { L"Miss" });
    pipeline.AddRootSignatureAssociation(m_HitSignature.Get(), { L"HitGroup" });

    pipeline.SetMaxPayloadSize(4 * sizeof(float));

    pipeline.SetMaxAttributeSize(2 * sizeof(float));

    pipeline.SetMaxRecursionDepth(1);

    m_RTStateObject = pipeline.Generate();

    ThrowIfFaild(m_RTStateObject->QueryInterface(IID_PPV_ARGS(&m_RTStateObjectProps)));
}

void SampleScene::CreateRaytracingOutputBuffer()
{
    auto device = Application::Get().GetDevice();
    D3D12_RESOURCE_DESC resDesc = {};
    resDesc.DepthOrArraySize = 1;
    resDesc.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D;
    resDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;

    resDesc.Flags = D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS;
    resDesc.Width = m_Width;
    resDesc.Height = m_Height;
    resDesc.Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN;
    resDesc.MipLevels = 1;
    resDesc.SampleDesc.Count = 1;

    ThrowIfFaild(device->CreateCommittedResource(
        &nv_helpers_dx12::kDefaultHeapProps, D3D12_HEAP_FLAG_NONE, &resDesc,
        D3D12_RESOURCE_STATE_COPY_SOURCE, nullptr,
        IID_PPV_ARGS(&m_OutputResource)
    ));
}

void SampleScene::CreateRaytracingResourceHeap()
{
    auto device = Application::Get().GetDevice();
    m_SRV_UAV_Heap = nv_helpers_dx12::CreateDescriptorHeap(device.Get(), 2, D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV, true);
}

void SampleScene::CreateRaytracingResourceView()
{
    auto device = Application::Get().GetDevice();

    D3D12_CPU_DESCRIPTOR_HANDLE srvHandle = m_SRV_UAV_Heap->GetCPUDescriptorHandleForHeapStart();

    D3D12_UNORDERED_ACCESS_VIEW_DESC uavDesc = {};
    uavDesc.ViewDimension = D3D12_UAV_DIMENSION_TEXTURE2D;
    device->CreateUnorderedAccessView(m_OutputResource.Get(), nullptr, &uavDesc, srvHandle);

    // Add the Top Level AS SRV right after the raytracing output buffer
    srvHandle.ptr += device->GetDescriptorHandleIncrementSize(
        D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV
    );

    D3D12_SHADER_RESOURCE_VIEW_DESC srvDesc;
    srvDesc.Format = DXGI_FORMAT_UNKNOWN;
    srvDesc.ViewDimension = D3D12_SRV_DIMENSION_RAYTRACING_ACCELERATION_STRUCTURE;
    srvDesc.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING;
    srvDesc.RaytracingAccelerationStructure.Location = m_TopLevelASBuffers.pResult->GetGPUVirtualAddress();
    device->CreateShaderResourceView(nullptr, &srvDesc, srvHandle);
}

void SampleScene::CreateRaytracingShaderBindingTable()
{
    auto device = Application::Get().GetDevice();

    m_sbtHelper.Reset();

    D3D12_GPU_DESCRIPTOR_HANDLE srv_uav_HeapHandle = m_SRV_UAV_Heap->GetGPUDescriptorHandleForHeapStart();

    auto heapPointer = reinterpret_cast<UINT64 *>(srv_uav_HeapHandle.ptr);

    m_sbtHelper.AddRayGenerationProgram(L"RayGen", { heapPointer });

    m_sbtHelper.AddMissProgram(L"Miss", {});

    m_sbtHelper.AddHitGroup(L"HitGroup", {(void *)(m_VertexBuffer->GetGPUVirtualAddress())});

    uint32_t sbtSize = m_sbtHelper.ComputeSBTSize();

    m_sbtStorage = nv_helpers_dx12::CreateBuffer(
        device.Get(), sbtSize, D3D12_RESOURCE_FLAG_NONE,
        D3D12_RESOURCE_STATE_GENERIC_READ, nv_helpers_dx12::kUploadHeapProps
    );
    if (!m_sbtStorage)
    {
        throw std::logic_error("Could not allocate the shader binding table");
    }
    m_sbtHelper.Generate(m_sbtStorage.Get(), m_RTStateObjectProps.Get());
}

void SampleScene::OnResize(ResizeEventArgs& e)
{
    if (e.Width != m_Viewport.Width || e.Height != m_Viewport.Height)
    {
        super::OnResize(e);

        m_Viewport = CD3DX12_VIEWPORT(0.0f, 0.0f, static_cast<float>(e.Width), static_cast<float>(e.Height));
        ResizeDepthBuffer(e.Width, e.Height);
    }
}

void SampleScene::OnUpdate(UpdateEventArgs& e)
{
    static uint64_t frameCount = 0;
    static double totalTime = 0.0;
    
    super::OnUpdate(e);

    totalTime += e.deltaTime;
    frameCount++;

    if (totalTime > 1.0)
    {
        double fps = frameCount / totalTime;

        char buffer[512];
        sprintf_s(buffer, "FPS: %f\n", fps);
        OutputDebugStringA(buffer);

        frameCount = 0;
        totalTime = 0.0;
    }

    // Update the model matrix
    float angle = static_cast<float>(e.TotalTime * 90.0);
    const XMVECTOR rotationAxis = XMVectorSet(0, 1, 0, 0);
    m_ModelMatrix = XMMatrixRotationAxis(rotationAxis, XMConvertToRadians(angle));
    // Update the view matrix
    const XMVECTOR eyePosition = XMVectorSet(0, 0, -10, 1);
    const XMVECTOR focusPoint = XMVectorSet(0, 0, 0, 1);
    const XMVECTOR upDirection = XMVectorSet(0, 1, 0, 0);
    m_ViewMatrix = XMMatrixLookAtLH(eyePosition, focusPoint, upDirection);
    // Update the projection matrix.
    float aspectRatio = m_Viewport.Width / static_cast<float>(m_Viewport.Height);
    m_ProjectionMatrix = XMMatrixPerspectiveFovLH(XMConvertToRadians(m_FoV), aspectRatio, 0.1f, 100.0f);
}

void SampleScene::TransitionResource(ComPtr<ID3D12GraphicsCommandList2> commandList, ComPtr<ID3D12Resource> resource, D3D12_RESOURCE_STATES beforeState, D3D12_RESOURCE_STATES afterState)
{
    CD3DX12_RESOURCE_BARRIER barrier = CD3DX12_RESOURCE_BARRIER::Transition(resource.Get(), beforeState, afterState);
    commandList->ResourceBarrier(1, &barrier);
}

void SampleScene::ClearRTV(ComPtr<ID3D12GraphicsCommandList2> commandList, D3D12_CPU_DESCRIPTOR_HANDLE rtv, FLOAT* clearColor)
{
    commandList->ClearRenderTargetView(rtv, clearColor, 0, nullptr);
}

void SampleScene::ClearDepth(ComPtr<ID3D12GraphicsCommandList2> commandList, D3D12_CPU_DESCRIPTOR_HANDLE dsv, FLOAT depth)
{
    commandList->ClearDepthStencilView(dsv, D3D12_CLEAR_FLAG_DEPTH, depth, 0, 0, nullptr);
}

void SampleScene::OnRender(RenderEventArgs& e)
{
    super::OnRender(e);

    auto commandQueue = Application::Get().GetCommandQueue(D3D12_COMMAND_LIST_TYPE_DIRECT);
    auto commandList = commandQueue->GetCommandList();

    UINT currentBackBufferIndex = m_pWindow->GetCurrentBackBufferIndex();
    auto backBuffer = m_pWindow->GetCurrentBackBuffer();
    auto rtv = m_pWindow->GetCurrentRenderTargetView();
    auto dsv = m_DSVHeap->GetCPUDescriptorHandleForHeapStart();


    commandList->RSSetViewports(1, &m_Viewport);
    commandList->RSSetScissorRects(1, &m_ScissorRect);

    commandList->OMSetRenderTargets(1, &rtv, FALSE, &dsv);

    TransitionResource(commandList, backBuffer, D3D12_RESOURCE_STATE_PRESENT, D3D12_RESOURCE_STATE_RENDER_TARGET);

    commandList->SetPipelineState(m_PipelineState.Get());
    commandList->SetGraphicsRootSignature(m_RootSignature.Get());

    if (m_Raster)
    {
        // Clear the render targets
        FLOAT clearColor[] = { 0.4f, 0.6f, 0.9f, 1.0f };
        ClearRTV(commandList, rtv, clearColor);
        ClearDepth(commandList, dsv);

        commandList->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
        commandList->IASetVertexBuffers(0, 1, &m_VertexBufferView);
        commandList->IASetIndexBuffer(&m_IndexBufferView);

        // Update the MVP matrix
        XMMATRIX mvpMatrix = XMMatrixMultiply(m_ModelMatrix, m_ViewMatrix);
        mvpMatrix = XMMatrixMultiply(mvpMatrix, m_ProjectionMatrix);
        commandList->SetGraphicsRoot32BitConstants(0, sizeof(XMMATRIX) / 4, &mvpMatrix, 0);

        commandList->DrawIndexedInstanced(_countof(g_Indicies), 1, 0, 0, 0);
    }
    else
    {
        FLOAT clearColor[] = { 0.6f, 0.8f, 0.4f, 1.0f };
        ClearRTV(commandList, rtv, clearColor);

        std::vector<ID3D12DescriptorHeap *> heaps = { m_SRV_UAV_Heap.Get() };
        commandList->SetDescriptorHeaps(static_cast<UINT>(heaps.size()), heaps.data());

        CD3DX12_RESOURCE_BARRIER transition = CD3DX12_RESOURCE_BARRIER::Transition(
            m_OutputResource.Get(), D3D12_RESOURCE_STATE_COPY_SOURCE,
            D3D12_RESOURCE_STATE_UNORDERED_ACCESS
        );
        commandList->ResourceBarrier(1, &transition);

        D3D12_DISPATCH_RAYS_DESC desc = {};

        uint32_t rayGenerationSectionSizeInBytes = m_sbtHelper.GetRayGenSectionSize();
        desc.RayGenerationShaderRecord.StartAddress = m_sbtStorage->GetGPUVirtualAddress();
        desc.RayGenerationShaderRecord.SizeInBytes = rayGenerationSectionSizeInBytes;

        uint32_t missSectionSizeInBytes = m_sbtHelper.GetMissSectionSize();
        desc.MissShaderTable.StartAddress = m_sbtStorage->GetGPUVirtualAddress() + rayGenerationSectionSizeInBytes;
        desc.MissShaderTable.SizeInBytes = missSectionSizeInBytes;
        desc.MissShaderTable.StrideInBytes = m_sbtHelper.GetMissEntrySize();

        uint32_t stbStartAddress = m_sbtStorage->GetGPUVirtualAddress();
        uint32_t hitGroupSectionSize = m_sbtHelper.GetHitGroupSectionSize();
        desc.HitGroupTable.StartAddress = m_sbtStorage->GetGPUVirtualAddress() + rayGenerationSectionSizeInBytes + missSectionSizeInBytes;
        desc.HitGroupTable.SizeInBytes = hitGroupSectionSize;
        desc.HitGroupTable.StrideInBytes = m_sbtHelper.GetHitGroupEntrySize();

        desc.Width = m_Width;
        desc.Height = m_Height;
        desc.Depth = 1;

        commandList->SetPipelineState1(m_RTStateObject.Get());
        commandList->DispatchRays(&desc);

        transition = CD3DX12_RESOURCE_BARRIER::Transition(
            m_OutputResource.Get(), D3D12_RESOURCE_STATE_UNORDERED_ACCESS,
            D3D12_RESOURCE_STATE_COPY_SOURCE
        );
        commandList->ResourceBarrier(1, &transition);
        transition = CD3DX12_RESOURCE_BARRIER::Transition(
            backBuffer.Get(), D3D12_RESOURCE_STATE_RENDER_TARGET,
            D3D12_RESOURCE_STATE_COPY_DEST
        );
        commandList->ResourceBarrier(1, &transition);

        commandList->CopyResource(backBuffer.Get(), m_OutputResource.Get());

        transition = CD3DX12_RESOURCE_BARRIER::Transition(
            backBuffer.Get(), D3D12_RESOURCE_STATE_COPY_DEST,
            D3D12_RESOURCE_STATE_RENDER_TARGET
        );
        commandList->ResourceBarrier(1, &transition);
    }

    // Present
    {
        TransitionResource(commandList, backBuffer, D3D12_RESOURCE_STATE_RENDER_TARGET, D3D12_RESOURCE_STATE_PRESENT);

        m_FenceValues[currentBackBufferIndex] = commandQueue->ExecuteCommandList(commandList);

        currentBackBufferIndex = m_pWindow->Present();

        commandQueue->WaitForFenceValue(m_FenceValues[currentBackBufferIndex]);
    }
}

void SampleScene::UnloadContent()
{
    if (m_ContentLoaded)
    {
        m_VertexBuffer->Release();
        m_IndexBuffer->Release();
        //m_VertexBuffer = nullptr;
        //m_IndexBuffer = nullptr;
        //m_DepthBuffer->Release();

        //m_BottomLevelASBuffers.pInstanceDesc->Release();
        m_BottomLevelASBuffers.pResult->Release();
        m_BottomLevelASBuffers.pScratch->Release();
        m_BottomLevelASBuffers.pResult = nullptr;
        m_BottomLevelASBuffers.pScratch = nullptr;
        m_TopLevelASBuffers.pInstanceDesc->Release();
        m_TopLevelASBuffers.pResult->Release();
        m_TopLevelASBuffers.pScratch->Release();
        m_TopLevelASBuffers.pInstanceDesc = nullptr;
        m_TopLevelASBuffers.pResult = nullptr;
        m_TopLevelASBuffers.pScratch = nullptr;

        m_RayGenLibrary->Release();
        m_MissLibrary->Release();
        m_HitLibrary->Release();
        m_RayGenLibrary = nullptr;
        m_MissLibrary = nullptr;
        m_HitLibrary = nullptr;

        m_RayGenSignature->Release();
        m_MissSignature->Release();
        m_HitSignature->Release();
        m_RayGenSignature = nullptr;
        m_MissSignature = nullptr;
        m_HitSignature = nullptr;

        m_RTStateObject->Release();
        m_RTStateObjectProps->Release();
        m_RTStateObject = nullptr;
        m_RTStateObjectProps = nullptr;

        m_SRV_UAV_Heap->Release();
        m_SRV_UAV_Heap = nullptr;

        //if (m_OutputResource != nullptr)
        //{
        //    m_OutputResource->Release();
        //    m_OutputResource = nullptr;
        //}

        m_sbtStorage->Release();
        m_sbtStorage = nullptr;

        m_ContentLoaded = false;
    }
}
void SampleScene::OnKeyboardDown(KeyEventArgs& e)
{
    super::OnKeyboardDown(e);

    switch (e.Key)
    {
    case KeyCode::Escape:
        Application::Get().Quit(0);
        break;
    case KeyCode::Enter:
        if (e.Alt)
        {
    case KeyCode::F11:
        m_pWindow->ToggleFullscreen();
        break;
        }
    case KeyCode::V:
        m_pWindow->ToggleVSync();
        break;
    case KeyCode::R:
        m_Raster = !m_Raster;
        break;
    }
}

void SampleScene::OnMouseWheel(MouseWheelEventArgs& e)
{
    m_FoV -= e.WheelDelta;
    m_FoV = clamp(m_FoV, 12.0f, 90.0f);

    char buffer[256];
    sprintf_s(buffer, "FoV: %f\n", m_FoV);
    OutputDebugStringA(buffer);
}