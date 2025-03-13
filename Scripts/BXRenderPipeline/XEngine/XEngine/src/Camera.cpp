#include <Application.h>
#include <Camera.h>

#include <DirectXMath.h>

#include "external/DXRHelpers/DXRHelper.h"

using namespace DirectX;

Camera::Camera(float fov, float aspect)
	: m_FoV(fov)
	, m_AspectRatio(aspect)
{
	m_ContentLoaded = false;
}

Camera::~Camera()
{
	Destroy();
}

void Camera::Destroy()
{
	if (m_ContentLoaded)
	{
		m_CameraBuffer->Release();
		m_CameraBuffer = nullptr;

		m_ConstHeap->Release();
		m_ConstHeap = nullptr;

		m_ContentLoaded = false;
	}
}

void Camera::CreateCameraBuffer()
{
	auto device = Application::Get().GetDevice();
	// V, P, VInv, PInv
	uint32_t numMatrix = 4;
	m_CameraBufferSize = numMatrix * sizeof(XMMATRIX);

	m_CameraBuffer = nv_helpers_dx12::CreateBuffer(
		device.Get(), m_CameraBufferSize, D3D12_RESOURCE_FLAG_NONE,
		D3D12_RESOURCE_STATE_GENERIC_READ, nv_helpers_dx12::kUploadHeapProps
	);

	m_ConstHeap = nv_helpers_dx12::CreateDescriptorHeap(
		device.Get(), 1, D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV, true
	);

	D3D12_CONSTANT_BUFFER_VIEW_DESC cbvDesc = {};
	cbvDesc.BufferLocation = m_CameraBuffer->GetGPUVirtualAddress();
	cbvDesc.SizeInBytes = m_CameraBufferSize;

	D3D12_CPU_DESCRIPTOR_HANDLE srvHandle = m_ConstHeap->GetCPUDescriptorHandleForHeapStart();
	device->CreateConstantBufferView(&cbvDesc, srvHandle);

	m_ContentLoaded = true;
}

void Camera::Update(float fov, float aspect, double totalTime)
{
	m_FoV = fov;
	m_AspectRatio = aspect;

	m_Angle = static_cast<float>(totalTime * 90.0);
}

void Camera::UpdateCameraBuffer()
{
	std::vector<XMMATRIX> matrices(4);

	XMVECTOR Eye = XMVectorSet(1.5f, 1.5f, 1.5f, 0.f);
	XMVECTOR At = XMVectorSet(0.f, 0.f, 0.f, 0.f);
	XMVECTOR Up = XMVectorSet(0.f, 1.f, 0.f, 0.f);

	// Model Matrix
	const XMVECTOR rotationAxis = XMVectorSet(0, 1, 0, 0);
	XMMATRIX modleMat = XMMatrixRotationAxis(rotationAxis, XMConvertToRadians(m_Angle));

	// View Matrix
	matrices[0] = XMMatrixMultiply(modleMat, XMMatrixLookAtRH(Eye, At, Up));

	float fovAngleY = m_FoV * XM_PI / 180.0f;
	// Projection Matrix
	matrices[1] = XMMatrixPerspectiveFovRH(fovAngleY, m_AspectRatio, 0.1f, 1000.0f);

	XMVECTOR det;
	matrices[2] = XMMatrixInverse(&det, matrices[0]);
	matrices[3] = XMMatrixInverse(&det, matrices[1]);

	uint8_t* pData;
	ThrowIfFaild(m_CameraBuffer->Map(0, nullptr, (void**)&pData));
	memcpy(pData, matrices.data(), m_CameraBufferSize);
	m_CameraBuffer->Unmap(0, nullptr);
}

ComPtr<ID3D12Resource> Camera::GetCameraBuffer()
{
	return m_CameraBuffer;
}

uint32_t Camera::GetCameraBufferSize()
{
	return m_CameraBufferSize;
}

ComPtr<ID3D12DescriptorHeap> Camera::GetConstHeap()
{
	return m_ConstHeap;
}