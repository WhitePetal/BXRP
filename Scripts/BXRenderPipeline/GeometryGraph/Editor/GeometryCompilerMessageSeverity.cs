using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    public enum GeometryCompilerMessageSeverity
    {
		/// <summary>
		///   <para>Indicates that a message returned by the Unity Shader Compiler is an error and the compilation failed.</para>
		/// </summary>
		Error,
		/// <summary>
		///   <para>Indicates that a message returned by the Unity Shader Compiler is a warning.</para>
		/// </summary>
		Warning
	}
}
