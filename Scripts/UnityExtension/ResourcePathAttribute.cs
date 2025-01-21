using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field)]
public class ResourcePathAttribute : Attribute
{
	public string path;

	public ResourcePathAttribute(string path)
	{
		this.path = path;
	}
}
