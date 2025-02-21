#pragma once

#define WIN32_LEN_AND_MEAN
#include <Windows.h> // For HRESULT
#include <exception>

// From DXSampleHelper.h
// Source: https://github.com/Microsoft/DirectX-Graphics-Samples
inline void ThrowIfFaild(HRESULT hr)
{
	if (FAILED(hr))
	{
		throw std::exception();
	}
}