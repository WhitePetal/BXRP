using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BXGeometryGraph
{
    class MessageManager
    {
        public interface IErrorLog
        {
            void LogError(string message, UnityEngine.Object context);
            void LogWarning(string message, UnityEngine.Object context);
        }

        protected Dictionary<object, Dictionary<string, List<GeometryMessage>>> m_Messages = new Dictionary<object, Dictionary<string, List<GeometryMessage>>>();

        private Dictionary<string, List<GeometryMessage>> m_Combined = new Dictionary<string, List<GeometryMessage>>();

        public bool nodeMessagesChanged { get; private set; }

        private Dictionary<string, List<GeometryMessage>> m_FoundMessages;

        public void AddOrAppendError(object errorProvider, string nodeId, GeometryMessage error)
        {
            if(!m_Messages.TryGetValue(errorProvider, out var messages))
            {
                messages = new Dictionary<string, List<GeometryMessage>>();
                m_Messages[errorProvider] = messages;
            }

            List<GeometryMessage> messageList;
            if(messages.TryGetValue(nodeId, out messageList))
            {
                messageList.Add(error);
            }
            else
            {
                messages[nodeId] = new List<GeometryMessage>() { error };
            }

            nodeMessagesChanged = true;
        }

        // Sort messages so errors come before warnings in the list
        private static int CompareMessages(GeometryMessage m1, GeometryMessage m2)
        {
            return m1.severity > m2.severity ? 1 : m2.severity > m1.severity ? -1 : 0;
        }

        public IEnumerable<KeyValuePair<string, List<GeometryMessage>>> GetNodeMessages()
        {
            var fixedNodes = new List<string>();
            m_Combined.Clear();
            foreach(var messageMap in m_Messages)
            {
                foreach(var messageList in messageMap.Value)
                {
                    if(!m_Combined.TryGetValue(messageList.Key, out var foundList))
                    {
                        foundList = new List<GeometryMessage>();
                        m_Combined.Add(messageList.Key, foundList);
                    }
                    foundList.AddRange(messageList.Value);

                    if(messageList.Value.Count == 0)
                    {
                        fixedNodes.Add(messageList.Key);
                    }
                }

                // If all the messages from a provider for a node are gone,
                // we can now remove it from the list since that will be reported in m_Combined
                fixedNodes.ForEach(nodeId => messageMap.Value.Remove(nodeId));
            }

            foreach(var nodeList in m_Combined)
            {
                nodeList.Value.Sort(CompareMessages);
            }

            nodeMessagesChanged = false;
            return m_Combined;
        }

        public void RemoveNode(string nodeId)
        {
            foreach(var messageMap in m_Messages)
            {
                nodeMessagesChanged |= messageMap.Value.Remove(nodeId);
            }
        }

        public void ClearAllFromProvider(object messageProvider)
        {
            if(m_Messages.TryGetValue(messageProvider, out m_FoundMessages))
            {
                foreach(var messageList in m_FoundMessages)
                {
                    nodeMessagesChanged |= messageList.Value.Count > 0;
                    messageList.Value.Clear();
                }

                m_FoundMessages = null;
            }
        }

        public void ClearNodesFromProvider(object messageProvider, IEnumerable<AbstractGeometryNode> nodes)
        {
            if(m_Messages.TryGetValue(messageProvider, out m_FoundMessages))
            {
                foreach(var node in nodes)
                {
                    if(m_FoundMessages.TryGetValue(node.objectId, out var messages))
                    {
                        nodeMessagesChanged |= messages.Count > 0;
                        messages.Clear();
                    }
                }
            }
        }

        public void ClearAll()
        {
            m_Messages.Clear();
            m_Combined.Clear();
            nodeMessagesChanged = false;
        }

        private void DebugPrint()
        {
            StringBuilder output = new StringBuilder("MessageMap:\n");
            foreach(var messageMap in m_Messages)
            {
                output.AppendFormat("\tFrom Provider {0}:\n", messageMap.Key.GetType());
                foreach (var messageList in messageMap.Value)
                {
                    output.AppendFormat("\t\tNode {0} has {1} messages:\n", messageList.Key, messageList.Value.Count);
                    foreach (var message in messageList.Value)
                    {
                        output.AppendFormat("\t\t\t{0}\n", message.message);
                    }
                }
            }
            Debug.Log(output.ToString());
        }

        public static void Log(string path, GeometryMessage message, UnityEngine.Object context, IErrorLog log)
        {
            var errString = $"{message.severity} in Graph at {path} on line {message.line}: {message.message}";
            if (message.severity == GeometryCompilerMessageSeverity.Error)
            {
                log.LogError(errString, context);
            }
            else
            {
                log.LogWarning(errString, context);
            }
        }

        public bool AnyError(Func<string, bool> nodeFilter = null)
        {
            if (m_Messages == null)
                return false;

            foreach(var kvp in m_Messages)
            {
                var errorProvider = kvp.Key;
                var messageMap = kvp.Value;
                foreach(var kvp2 in messageMap)
                {
                    var nodeId = kvp2.Key;
                    List<GeometryMessage> messageList = kvp2.Value;
                    if((nodeFilter == null) || nodeFilter(nodeId))
                    {
                        foreach(var message in messageList)
                        {
                            if (message.severity == GeometryCompilerMessageSeverity.Error)
                                return true;
                        }
                    }
                }
            }

            return false;
        }

        public IEnumerable<string> ErrorStrings(Func<string, bool> nodeFilter = null, GeometryCompilerMessageSeverity severity = GeometryCompilerMessageSeverity.Error)
        {
            if (m_Messages == null)
                yield break;

            foreach(var kvp in m_Messages)
            {
                var errorProvider = kvp.Key;
                var messageMap = kvp.Value;
                foreach(var kvp2 in messageMap)
                {
                    var nodeId = kvp2.Key;
                    if((nodeFilter == null) || nodeFilter(nodeId))
                    {
                        List<GeometryMessage> messageList = kvp2.Value;
                        foreach(var message in messageList)
                        {
                            if (message.severity == severity)
                                yield return message.message;
                        }
                    }
                }
            }
        }
    }
}
