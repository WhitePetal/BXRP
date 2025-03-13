#include <RaytracingPipeline.h>

#include <wrl.h>
using namespace Microsoft::WRL;

#include <Helpers.h>
#include <d3dx12.h>
#include <d3dcompiler.h>

#include <CommandQueue.h>

#include "external/DXRHelpers/DXRHelper.h"
#include "external/DXRHelpers/nv_helpers_dx12/BottomLevelASGenerator.h"
#include "external/DXRHelpers//nv_helpers_dx12/RaytracingPipelineGenerator.h"
#include "external/DXRHelpers/nv_helpers_dx12/RootSignatureGenerator.h"

using namespace DirectX;

RaytracingPipeline::RaytracingPipeline(Engine* engine) : m_Engine(engine)
{
    m_ContentLoaded = false;
}

RaytracingPipeline::~RaytracingPipeline()
{
    Destroy();
}

bool RaytracingPipeline::Initialize()
{
	return true;
}

void RaytracingPipeline::LoadContent()
{
    // Setup the acceleration structures (AS) for raytracing. When setting up
    // geometry, each bottom-level AS has its own transform matrix.
    CreateAccelerationStructures();
    // Create the raytracing pipeline, associating the shader code to symbol names
    // and to their root signatures, and defining the amount of memory carried by
    // rays (ray payload)
    CreateRaytracingPipline();
    CreateRaytracingResourceHeap();
    m_ContentLoaded = true;
    OnResize();
}

void RaytracingPipeline::OnResize()
{
    if (m_ContentLoaded)
    {
        CreateRaytracingOutputBuffer();
        CreateRaytracingResourceView();
        CreateRaytracingShaderBindingTable();
    }
}

void RaytracingPipeline::OnRender(ComPtr<ID3D12GraphicsCommandList4> commandList, D3D12_CPU_DESCRIPTOR_HANDLE rtv, ComPtr<ID3D12Resource> backBuffer)
{
    FLOAT clearColor[] = { 0.6f, 0.8f, 0.4f, 1.0f };
    m_Engine->ClearRTV(commandList, rtv, clearColor);

    std::vector<ID3D12DescriptorHeap*> heaps = { m_SRV_UAV_Heap.Get() };
    commandList->SetDescriptorHeaps(static_cast<UINT>(heaps.size()), heaps.data());

    CD3DX12_RESOURCE_BARRIER transition = CD3DX12_RESOURCE_BARRIER::Transition(
        m_OutputResource.Get(), D3D12_RESOURCE_STATE_COPY_SOURCE,
        D3D12_RESOURCE_STATE_UNORDERED_ACCESS
    );
    commandList->ResourceBarrier(1, &transition);

    D3D12_DISPATCH_RAYS_DESC desc = {};

    uint32_t rayGenerationSectionSizeInBytes = m_SBTHelper.GetRayGenSectionSize();
    desc.RayGenerationShaderRecord.StartAddress = m_SBTStorage->GetGPUVirtualAddress();
    desc.RayGenerationShaderRecord.SizeInBytes = rayGenerationSectionSizeInBytes;

    uint32_t missSectionSizeInBytes = m_SBTHelper.GetMissSectionSize();
    desc.MissShaderTable.StartAddress = m_SBTStorage->GetGPUVirtualAddress() + rayGenerationSectionSizeInBytes;
    desc.MissShaderTable.SizeInBytes = missSectionSizeInBytes;
    desc.MissShaderTable.StrideInBytes = m_SBTHelper.GetMissEntrySize();

    uint32_t stbStartAddress = m_SBTStorage->GetGPUVirtualAddress();
    uint32_t hitGroupSectionSize = m_SBTHelper.GetHitGroupSectionSize();
    desc.HitGroupTable.StartAddress = m_SBTStorage->GetGPUVirtualAddress() + rayGenerationSectionSizeInBytes + missSectionSizeInBytes;
    desc.HitGroupTable.SizeInBytes = hitGroupSectionSize;
    desc.HitGroupTable.StrideInBytes = m_SBTHelper.GetHitGroupEntrySize();

    desc.Width = m_Engine->GetWidth();
    desc.Height = m_Engine->GetHeight();
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

void RaytracingPipeline::UnloadContent()
{
    if (m_ContentLoaded)
    {
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

        m_SBTStorage->Release();
        m_SBTStorage = nullptr;

        m_ContentLoaded = false;
    }
}

void RaytracingPipeline::Destroy()
{
    UnloadContent();
}

AccelerationStructureBuffers RaytracingPipeline::CreateBottomLevelAS(ComPtr<ID3D12Device5> device, ComPtr<ID3D12GraphicsCommandList4> commandList, std::vector <std::pair<ComPtr<ID3D12Resource>, uint32_t>> vVertexBuffers)
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

void RaytracingPipeline::CreateTopLevelAS(ComPtr<ID3D12Device5> device, ComPtr<ID3D12GraphicsCommandList4> commandList, const std::vector<std::pair<ComPtr<ID3D12Resource>, XMMATRIX>>& instances)
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

void RaytracingPipeline::CreateAccelerationStructures()
{
    auto device = Application::Get().GetDevice();
    auto commandQueue = Application::Get().GetCommandQueue(D3D12_COMMAND_LIST_TYPE_DIRECT);
    auto commandList = commandQueue->GetCommandList();

    // Build the bottom AS
    m_BottomLevelASBuffers = CreateBottomLevelAS(device, commandList, { {m_Engine->GetVertexBuffer().Get(), _countof(g_Vertices)} });

    // Just one instance for now
    m_Instances = { {m_BottomLevelASBuffers.pResult, XMMatrixIdentity()} };
    CreateTopLevelAS(device, commandList, m_Instances);

    // Flush the command list and wait for it to finish
    auto fenceValue = commandQueue->ExecuteCommandList(commandList);
    commandQueue->WaitForFenceValue(fenceValue);
}

ComPtr<ID3D12RootSignature> RaytracingPipeline::CreateRayGenSignature()
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

ComPtr<ID3D12RootSignature> RaytracingPipeline::CreateHitSignature()
{
    auto device = Application::Get().GetDevice();
    nv_helpers_dx12::RootSignatureGenerator rsc;
    rsc.AddRootParameter(D3D12_ROOT_PARAMETER_TYPE_SRV);
    return rsc.Generate(device.Get(), true);
}

ComPtr<ID3D12RootSignature> RaytracingPipeline::CreateMissSignature()
{
    auto device = Application::Get().GetDevice();
    nv_helpers_dx12::RootSignatureGenerator rsc;
    return rsc.Generate(device.Get(), true);
}

void RaytracingPipeline::CreateRaytracingPipline()
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

void RaytracingPipeline::CreateRaytracingOutputBuffer()
{
    auto device = Application::Get().GetDevice();
    D3D12_RESOURCE_DESC resDesc = {};
    resDesc.DepthOrArraySize = 1;
    resDesc.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D;
    resDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;

    resDesc.Flags = D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS;
    resDesc.Width = m_Engine->GetWidth();
    resDesc.Height = m_Engine->GetHeight();
    resDesc.Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN;
    resDesc.MipLevels = 1;
    resDesc.SampleDesc.Count = 1;

    ThrowIfFaild(device->CreateCommittedResource(
        &nv_helpers_dx12::kDefaultHeapProps, D3D12_HEAP_FLAG_NONE, &resDesc,
        D3D12_RESOURCE_STATE_COPY_SOURCE, nullptr,
        IID_PPV_ARGS(&m_OutputResource)
    ));
}

void RaytracingPipeline::CreateRaytracingResourceHeap()
{
    auto device = Application::Get().GetDevice();
    m_SRV_UAV_Heap = nv_helpers_dx12::CreateDescriptorHeap(device.Get(), 2, D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV, true);
}

void RaytracingPipeline::CreateRaytracingResourceView()
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

void RaytracingPipeline::CreateRaytracingShaderBindingTable()
{
    auto device = Application::Get().GetDevice();

    m_SBTHelper.Reset();

    D3D12_GPU_DESCRIPTOR_HANDLE srv_uav_HeapHandle = m_SRV_UAV_Heap->GetGPUDescriptorHandleForHeapStart();

    auto heapPointer = reinterpret_cast<UINT64*>(srv_uav_HeapHandle.ptr);

    m_SBTHelper.AddRayGenerationProgram(L"RayGen", { heapPointer });

    m_SBTHelper.AddMissProgram(L"Miss", {});

    m_SBTHelper.AddHitGroup(L"HitGroup", { (void*)(m_Engine->GetVertexBuffer()->GetGPUVirtualAddress()) });

    uint32_t sbtSize = m_SBTHelper.ComputeSBTSize();

    m_SBTStorage = nv_helpers_dx12::CreateBuffer(
        device.Get(), sbtSize, D3D12_RESOURCE_FLAG_NONE,
        D3D12_RESOURCE_STATE_GENERIC_READ, nv_helpers_dx12::kUploadHeapProps
    );
    if (!m_SBTStorage)
    {
        throw std::logic_error("Could not allocate the shader binding table");
    }
    m_SBTHelper.Generate(m_SBTStorage.Get(), m_RTStateObjectProps.Get());
}