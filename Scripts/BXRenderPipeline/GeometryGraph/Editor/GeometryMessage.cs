using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor.Rendering;
using UnityEngine;

namespace BXGeometryGraph
{
    [StructLayout(LayoutKind.Sequential)]
    public struct GeometryMessage : IEquatable<GeometryMessage>
    {
        public string message { get; }
        public string messageDetails { get; }
        public string file { get; }
        public int line { get; }
        public ShaderCompilerPlatform platform { get; }
        public GeometryCompilerMessageSeverity severity { get; }

        public GeometryMessage(string msg, GeometryCompilerMessageSeverity sev = GeometryCompilerMessageSeverity.Error)
        {
            message = msg;
            messageDetails = string.Empty;
            file = string.Empty;
            line = 0;
            platform = ShaderCompilerPlatform.None;
            severity = sev;
        }

        public bool Equals(GeometryMessage other)
        {
            return string.Equals(message, other.message)
                && string.Equals(messageDetails, other.messageDetails)
                && string.Equals(file, other.file)
                && line == other.line
                && platform == other.platform
                && severity == other.severity;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is GeometryMessage && Equals((GeometryMessage)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (message != null ? message.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (messageDetails != null ? messageDetails.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (file != null ? file.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ line;
                hashCode = (hashCode * 397) ^ (int)platform;
                hashCode = (hashCode * 397) ^ (int)severity;
                return hashCode;
            }
        }

        public static bool operator ==(GeometryMessage left, GeometryMessage right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GeometryMessage left, GeometryMessage right)
        {
            return !left.Equals(right);
        }
    }
}
