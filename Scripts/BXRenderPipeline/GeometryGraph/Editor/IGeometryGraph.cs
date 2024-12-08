using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
	public interface IGeometryGraph
	{
		string GetGeometry(string name, GenerationMode mode, out List<PropertyCollector.TextureInfo> configuredTextures);
		void LoadedFromDisk();
	}
}
