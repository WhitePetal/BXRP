#pragma once

#include <string>
#include <Window.h>
#include <mutex>
#include <LogQueue.h>


class Debug
{
public:
	enum class Level
	{
		_INFO,
		_WARNING,
		_ERROR,
		_EXCEPTION
	};

	static void Initialize(const std::string& logDIr);
	static void Log(const std::string& message);
	static void LogError(const std::string& message);
	static void LogWarning(const std::string& message);
	static void LogException(const std::exception& e);
	static void Shutdown();

private:
	static void Log(Level level, const std::string& message);
	static std::string GetLogFileName();
	static void WriteToFile(const std::string& message);

	static void LogThreadProc();

	static std::string g_LogDirectory;
	static HANDLE g_LogFileHandle;
	static LogQueue g_LogQueue;
	static std::thread g_LogThread;
};