#pragma once

#include <queue>
#include<string>
#include <mutex>

class LogQueue
{
public:
	void Push(const std::string& message);

	std::string Pop();

	void Stop();

private:
	std::queue<std::string> m_Queue;
	std::mutex m_Mutex;
	std::condition_variable m_Condition;
	bool m_Stop = false;
};