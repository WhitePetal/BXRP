#pragma once

#include <d3d12.h>

#include <Engine.h>

#include <Window.h>

#include <DirectXMath.h>

#include <RasterPipeline.h>
#include <RaytracingPipeline.h>

class SampleScene : public Engine
{
public:
	using super = Engine;

	SampleScene(const std::wstring& name, int width, int height, bool vSync = false, bool raster = true);

	virtual bool LoadContent() override;

	virtual void UnloadContent() override;

protected:
	virtual void OnUpdate(UpdateEventArgs& e) override;
	virtual void OnRender(RenderEventArgs& e) override;
	virtual void OnKeyboardDown(KeyEventArgs& e) override;
	virtual void OnMouseWheel(MouseWheelEventArgs& e) override;
	virtual void OnResize(ResizeEventArgs& e) override;

private:
	float m_FoV;
	D3D12_RECT m_ScissorRect;
	D3D12_VIEWPORT m_Viewport;

	std::shared_ptr<RasterPipeline> m_RasterPipeline;
	std::shared_ptr<RaytracingPipeline> m_RaytracingPipeline;
};