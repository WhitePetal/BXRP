#pragma once

#define WIN32_LEN_AND_MEAN
#include <Windows.h> // For HRESULT
#include <exception>
#include <Debug.h>

// From DXSampleHelper.h
// Source: https://github.com/Microsoft/DirectX-Graphics-Samples
inline void ThrowIfFaild(HRESULT hr)
{
	if (FAILED(hr))
	{
		throw std::exception();
	}
}

/// <summary>
/// Clamp a value between a min and max range
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="val"></param>
/// <param name="min"></param>
/// <param name="max"></param>
/// <returns></returns>
template<typename T>
constexpr const T& clamp(const T& val, const T& min, const T& max)
{
	return val < min ? min : val > max ? max : val;
}