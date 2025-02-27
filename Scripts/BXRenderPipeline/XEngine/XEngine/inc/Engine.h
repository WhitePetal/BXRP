#pragma once

#define XENGINE_API __declspec(dllexport)

extern "C" {
	XENGINE_API void StartXEngine(HWND parentHWnd);
}

#include <Events.h>

#include <memory>
#include <string>

#include <Window.h>

class Engine : public std::enable_shared_from_this<Engine>
{
public:
	Engine(const std::wstring& name, int width, int height, bool vSync);
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

	std::shared_ptr<Window> m_pWindow;

	uint64_t m_FenceValues[Window::BackBufferCount] = {};

private:
	std::wstring m_Name;
	int m_Width;
	int m_Height;
	bool m_vSync;
};