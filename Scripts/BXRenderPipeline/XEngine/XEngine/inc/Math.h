#pragma once

namespace Math
{
	constexpr float PI = 3.1415926535897932384626433832795f;
	constexpr float _2PI = 2.0f * PI;

	constexpr float Degress(const float radians)
	{
		return radians * (180.0f / PI);
	}

	constexpr float Radians(const float degress)
	{
		return degress * (PI / 180.0f);
	}

	template <typename T>
	inline T AlignUpWithMask(T value, size_t mask)
	{
		return (T)(((size_t)value + mask) & ~mask);
	}

	template <typename T>
	inline T AlignDownWithMask(T value, size_t mask)
	{
		return (T)((size_t)value & ~mask);
	}

	template <typename T>
	inline T AlignUp(T value, size_t alignment)
	{
		return AlignUpWithMask(value, alignment - 1);
	}

	template <typename T>
	inline T AlignDown(T value, size_t alignment)
	{
		return AlignDownWithMask(value, alignment - 1);
	}

	template <typename T>
	inline bool IsAligned(T value, size_t alignment)
	{
		return 0 == ((size_t)value & (alignment - 1));
	}

	template <typename T>
	inline T DivideByMultiple(T value, size_t alignment)
	{
		return (T)((value + alignment - 1) / alignment);
	}
}