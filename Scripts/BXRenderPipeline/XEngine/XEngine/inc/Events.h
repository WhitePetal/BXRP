#pragma once
#include <KeyCodes.h>

/// <summary>
/// Base class for all event args
/// </summary>
class EventArgs
{
public:
	EventArgs()
	{ }

};

class KeyEventArgs : public EventArgs
{
public:
	enum KeyState
	{
		Up = 0, // released
		Down = 1 // pressed
	};

	typedef EventArgs base;
	KeyEventArgs(KeyCode::Key key, unsigned int c, KeyState state, bool control, bool shift, bool alt)
		: Key(key)
		, Char(c)
		, State(state)
		, Control(control)
		, Shift(shift)
		, Alt(alt)
	{ }
	
	KeyCode::Key Key; // The Key Code that was pressed or released.
	unsigned int Char; // The 32-bit character code that was pressed. This value will be 0 if it is a non-printable character.
	KeyState State; // Was the key pressed or released?
	bool Control; // Is the Control modifier pressed
	bool Shift; // Is the Shift modifier pressed
	bool Alt; // Is the Alt modifier pressed
};

class MouseMotionEventArgs : public EventArgs
{
public:
	typedef EventArgs base;
	MouseMotionEventArgs(bool leftButton, bool middleButton, bool rightButton, bool control, bool shift, int x, int y)
		: LeftButton(leftButton)
		, MiddleButton(middleButton)
		, RightButton(rightButton)
		, Control(control)
		, Shift(shift)
		, X(x), Y(y)
	{ }

	bool LeftButton; // Is the left mouse button down
	bool MiddleButton; // Is the middle mouse button down
	bool RightButton; // Is the right mouse button down
	bool Control; // Is the CTRL key down
	bool Shift; // Is the Shift key down

	int X, Y; // cursor pos relative to the upper-left corner of the client area
	int RelX, RelY; // how far the moused moved since the last event
};

class MouseButtonEventArgs : public EventArgs
{
public:
	enum MouseButton
	{
		None = 0,
		Left = 1,
		Right = 2,
		Middel = 3
	};
	enum ButtonState
	{
		Up = 0, // Released
		Down = 1, // Pressed
	};

	typedef EventArgs base;
	MouseButtonEventArgs(MouseButton buttonID, ButtonState state, bool leftButton, bool middleButton, bool rightButton, bool control, bool shift, int x, int y)
		: Button(buttonID)
		, State(state)
		, LeftButton(leftButton)
		, MiddleButton(middleButton)
		, RightButton(rightButton)
		, Control(control)
		, Shift(shift)
		, X(x), Y(y)
	{ }

	MouseButton Button; // The mouse button that was pressed or released.
	ButtonState State; // Was the button pressed or released
	bool LeftButton; // Is the left mouse button down
	bool MiddleButton; // Is the middle mouse button down
	bool RightButton; // Is the right mouse button down
	bool Control; // Is the CTRL key down
	bool Shift; // Is the Shift key down

	int X, Y; // The cursor pos relative to the upper-left corner of the client area
};

class MouseWheelEventArgs : public EventArgs
{
public:
	typedef EventArgs base;
	MouseWheelEventArgs(float wheelDelta, bool leftButton, bool middleButton, bool rightButton, bool control, bool shift, int x, int y)
		: WheelDelta(wheelDelta)
		, LeftButton(leftButton)
		, MiddleButton(middleButton)
		, RightButton(rightButton)
		, Control(control)
		, Shift(shift)
		, X(x), Y(y)
	{ }

	float WheelDelta; // How much the mouse wheel has moved. A positive value indicates that the wheel was moved to the right. A negative value indicates the wheel was moved to the left. 
	bool LeftButton; // Is the left mouse button down
	bool MiddleButton; // Is the middle mouse button down
	bool RightButton; // Is the right mouse button down
	bool Control; // Is the CTRL key down
	bool Shift; // Is the Shift key down

	int X, Y; // The cursor pos relative to the upper-left corner of the client area
};

class UpdateEventArgs : public EventArgs
{
public:
	typedef EventArgs base;
	UpdateEventArgs(double fDeltaTime, double fTotalTime)
		: deltaTime(fDeltaTime)
		, TotalTime(fTotalTime)
	{ }

	double deltaTime;
	double TotalTime;
};

class RenderEventArgs : public EventArgs
{
public:
	typedef EventArgs base;
	RenderEventArgs(double fDeltaTime, double fTotalTime)
		: deltaTime(fDeltaTime)
		, TotalTime(fTotalTime)
	{
	}

	double deltaTime;
	double TotalTime;
};

class ResizeEventArgs : public EventArgs
{
public:
	typedef EventArgs base;
	
	ResizeEventArgs(int width, int height)
		: Width(width)
		, Height(height)
	{ }

	int Width;
	int Height;

private:

};

class UserEventArgs : public EventArgs
{
public:
	typedef EventArgs base;
	UserEventArgs(int code, void* data1, void* data2)
		: Code(code)
		, Data1(data1)
		, Data2(data2)
	{ }

	int Code;
	void* Data1;
	void* Data2;
};