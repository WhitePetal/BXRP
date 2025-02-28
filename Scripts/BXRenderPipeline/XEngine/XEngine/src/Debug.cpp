#include <Debug.h>
#include <sstream>
#include <iomanip>
#include <chrono>

std::string Debug::g_LogDirectory;
HANDLE Debug::g_LogFileHandle;
LogQueue Debug::g_LogQueue;
std::thread Debug::g_LogThread;

void Debug::Initialize(const std::string& logDir)
{
	g_LogDirectory = logDir;

	if (!::CreateDirectory(g_LogDirectory.c_str(), nullptr) && GetLastError() != ERROR_ALREADY_EXISTS)
	{
		::MessageBox(nullptr, "Failed to create log directory!", "Error", MB_OK | MB_ICONERROR);
		return;
	}

	std::string logFilePath = GetLogFileName();
	g_LogFileHandle = ::CreateFile(
		logFilePath.c_str(),
		GENERIC_WRITE,
		FILE_SHARE_READ,
		nullptr,
		OPEN_ALWAYS,
		FILE_ATTRIBUTE_NORMAL,
		nullptr
	);

	if (g_LogFileHandle == INVALID_HANDLE_VALUE)
	{
		::MessageBox(nullptr, "Failed to open log file!", "Error", MB_OK | MB_ICONERROR);
	}
	else
	{
		SetFilePointer(g_LogFileHandle, 0, nullptr, FILE_END);
	}

	g_LogThread = std::thread(LogThreadProc);
}

void Debug::LogThreadProc()
{
	while (true)
	{
		std::string message = g_LogQueue.Pop();
		if (message.empty()) break;

		WriteToFile(message);
	}
}

void Debug::Log(Level level, const std::string& message)
{
	if (g_LogFileHandle == INVALID_HANDLE_VALUE)
		return;

	// 确保日志线程已启动
	if (!g_LogThread.joinable()) return;

	// 获取当前时间
	SYSTEMTIME localTime;
	::GetLocalTime(&localTime);

	// 格式化时间戳
	char timestamp[64];
	sprintf_s(timestamp,
		"[%04d-%02d-%02d %02d:%02d:%02d.%03d]",
		localTime.wYear, localTime.wMonth, localTime.wDay,
		localTime.wHour, localTime.wMinute, localTime.wSecond, localTime.wMilliseconds);

	// 格式化日志级别
	const char* levelStr = "";
	switch (level)
	{
	case Level::_INFO:
		levelStr = "INFO";
		break;
	case Level::_WARNING:
		levelStr = "WARNING";
		break;
	case Level::_ERROR:
		levelStr = "ERROR";
		break;
	case Level::_EXCEPTION:
		levelStr = "EXCEPTION";
		break;
	default:
		break;
	}

	// 构建完整日志消息
	std::string logMessage = std::string(timestamp) + " [" + levelStr + "] " + message + "\n";

	// 加入日志队列
	g_LogQueue.Push(logMessage);
}

void Debug::Log(const std::string& message)
{
	Log(Level::_INFO, message);
}
void Debug::LogWarning(const std::string& message)
{
	Log(Level::_WARNING, message);
}
void Debug::LogError(const std::string& message)
{
	Log(Level::_ERROR, message);
}
void Debug::LogException(const std::exception& e)
{
	Log(Level::_EXCEPTION, std::string(e.what()));
}

void Debug::WriteToFile(const std::string& message)
{
	DWORD bytesWritten;
	::WriteFile(
		g_LogFileHandle,
		message.c_str(),
		static_cast<DWORD>(message.size()),
		&bytesWritten,
		nullptr
	);

	// 检查文件大小，如果超过限制则分割文件
	DWORD fileSize = ::GetFileSize(g_LogFileHandle, nullptr);
	if (fileSize > 10 * 1024 * 1024) // 10mb
	{
		::CloseHandle(g_LogFileHandle);
		std::string newLogFilePath = GetLogFileName();
		g_LogFileHandle = ::CreateFile(
			newLogFilePath.c_str(),
			GENERIC_WRITE,
			FILE_SHARE_READ,
			nullptr,
			CREATE_ALWAYS,
			FILE_ATTRIBUTE_NORMAL,
			nullptr
		);
	}
}

void Debug::Shutdown()
{
	g_LogQueue.Stop();
	if (g_LogThread.joinable())
	{
		// 等待后台线程结束
		g_LogThread.join();
	}
	if (g_LogFileHandle != INVALID_HANDLE_VALUE)
	{
		::CloseHandle(g_LogFileHandle);
		g_LogFileHandle = INVALID_HANDLE_VALUE;
	}
}

std::string Debug::GetLogFileName()
{
	SYSTEMTIME localTime;
	::GetLocalTime(&localTime);

	char fileName[256];
	sprintf_s(fileName, "%s\\XEngine_Log_%04d%02d%02d_%02d%02d%02d.log",
		g_LogDirectory.c_str(),
		localTime.wYear, localTime.wMonth, localTime.wDay,
		localTime.wHour, localTime.wMinute, localTime.wSecond);
	return fileName;
}