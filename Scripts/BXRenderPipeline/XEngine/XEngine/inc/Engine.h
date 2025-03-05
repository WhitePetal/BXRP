#pragma once

#define XENGINE_API __declspec(dllexport)

extern "C" {
	XENGINE_API void StartXEngine(HWND parentHWnd = nullptr, LPWSTR workDir = nullptr);
}

#include <Events.h>

#include <memory>
#include <string>

#include <Window.h>

#include <dxcapi.h>
#include <DirectXMath.h>
#include <vector>
#include "external/DXRHelpers/nv_helpers_dx12/TopLevelASGenerator.h"

struct AccelerationStructureBuffers
{
	ComPtr<ID3D12Resource> pScratch; // Scratch memory for AS builder
	ComPtr<ID3D12Resource> pResult; // Where the AS is
	ComPtr<ID3D12Resource> pInstanceDesc; // Hold the matrices of the instances
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

protected:
	friend class Window;

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

	/// <summary>
	/// Create the acceleration structure of an instance
	/// </summary>
	/// <param name="vVertexBuffers">pair of buffer and vertex count</param>
	/// <returns>AccelerationStructureBuffers for TLAS</returns>
	virtual AccelerationStructureBuffers CreateBottomLevelAS(ComPtr<ID3D12Device5> device, ComPtr<ID3D12GraphicsCommandList4> commandList, std::vector <std::pair<ComPtr<ID3D12Resource>, uint32_t>> vVertexBuffers);
	
	/// <summary>
	/// Create the main accleration structure that holds
	/// all instances of the scene
	/// </summary>
	/// <param name="instances">instances: pair of BLAS and transform</param>
	virtual void CreateTopLevelAS(ComPtr<ID3D12Device5> device, ComPtr<ID3D12GraphicsCommandList4> commandList, const std::vector<std::pair<ComPtr<ID3D12Resource>, DirectX::XMMATRIX>>& instances);

	/// <summary>
	/// Create all acceleration structures, bottom and top
	/// </summary>
	virtual void CreateAccelerationStructures();

	std::shared_ptr<Window> m_pWindow;

	uint64_t m_FenceValues[Window::BackBufferCount] = {};

	int m_Width;
	int m_Height;
	bool m_Raster = true;

	ComPtr<ID3D12Resource> m_BottomLevelAS;
	nv_helpers_dx12::TopLevelASGenerator m_TopLevelASGenerator;
	AccelerationStructureBuffers m_TopLevelASBuffers;
	std::vector<std::pair<ComPtr<ID3D12Resource>, DirectX::XMMATRIX>> m_Instances;

private:
	std::wstring m_Name;
	bool m_vSync;
};