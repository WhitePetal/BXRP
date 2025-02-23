#pragma once

#define XENGINE_API __declspec(dllexport)

extern "C" {
	XENGINE_API void StartXEngine(HWND parentHWnd);
}