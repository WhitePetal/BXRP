#include <LogQueue.h>

void LogQueue::Push(const std::string& message)
{
	std::lock_guard<std::mutex> lock(m_Mutex);
	m_Queue.push(message);
	m_Condition.notify_one(); // 通知后台线程
}

std::string LogQueue::Pop()
{
	std::unique_lock<std::mutex> lock(m_Mutex);
	m_Condition.wait(lock, [this]
		{
			return !m_Queue.empty() || m_Stop;
		});
	if (m_Stop && m_Queue.empty()) return ""; // 退出信号
	std::string message = m_Queue.front();
	m_Queue.pop();
	return message;
}

void LogQueue::Stop()
{
	std::lock_guard<std::mutex> lock(m_Mutex);
	m_Stop = true;
	m_Condition.notify_all(); // 通知所有等待的线程
}