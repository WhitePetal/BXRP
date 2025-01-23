using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> Where to search the resource. </summary>
public enum SearchType
{
    /// <summary> Used for resources inside the project (e.g.: in packages) </summary>
    ProjectPath,

    /// <summary> Used for builtin resources </summary>
    BuiltinPath,

    /// <summary> Used for builtin extra resources </summary>
    BuiltinExtraPath,

    /// <summary> Used for shader that should be found by their name </summary>
    ShaderName,
}

/// <summary>
/// Abstract attribute specifying information about the path where this resources are located.
/// This is only used in the editor and doesn't have any effect at runtime.
/// To use it, Create a child class implementing it or use <see cref="ResourcePathAttribute"/>, <see cref="ResourcePathsAttribute"/> or <see cref="ResourceFormattedPathsAttribute"/>.
/// See <see cref="IRenderPipelineResources"/> for usage.
/// </summary>
[AttributeUsage(AttributeTargets.Field, Inherited = true)]
public abstract class ResourcePathsBaseAttribute : Attribute
{
    /// <summary> The lookup method. As we don't store it at runtime, you cannot rely on this property for runtime operation. </summary>
    public SearchType location { get; private set; }
    /// <summary> Search paths. As we don't store it at runtime, you cannot rely on this property for runtime operation. </summary>
    public string[] paths { get; private set; }
    /// <summary> Disambiguish array of 1 element and fields. As we don't store it at runtime, you cannot rely on this property for runtime operation. </summary>
    public bool isField { get; private set; }

    protected ResourcePathsBaseAttribute(string[] paths, bool isField, SearchType location)
    {
        this.paths = paths;
        this.isField = isField;
        this.location = location;
    }
}

public class ResourcePathAttribute : ResourcePathsBaseAttribute
{
	public string path;

	public ResourcePathAttribute(string path, SearchType location = SearchType.ProjectPath) : base(new[] {path}, true, location)
	{
		this.path = path;
	}
}
