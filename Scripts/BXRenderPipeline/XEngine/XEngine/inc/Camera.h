#pragma once
#include <d3d12.h>

#include <Window.h>

#include <memory>


class Camera : public std::enable_shared_from_this<Camera>
{
public:
	Camera(float fov, float aspect);
	~Camera();

	void CreateCameraBuffer();
	void Update(float fov, float aspect, double totalTime);
	void UpdateCameraBuffer();
	void Destroy();

	ComPtr<ID3D12Resource> GetCameraBuffer();
	uint32_t GetCameraBufferSize();
	ComPtr<ID3D12DescriptorHeap> GetConstHeap();

private:
	bool m_ContentLoaded;
	float m_FoV;
	float m_AspectRatio;
	float m_Angle;
	ComPtr<ID3D12Resource> m_CameraBuffer;
	ComPtr<ID3D12DescriptorHeap> m_ConstHeap;
	uint32_t m_CameraBufferSize = 0;
};