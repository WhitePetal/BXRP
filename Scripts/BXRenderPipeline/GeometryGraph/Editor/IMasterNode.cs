using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BXGeometryGraph
{
	public interface IMasterNode
	{
		string GetGeometry(GenerationMode mode, string name, out List<PropertyCollector.TextureInfo> configuredTextures);
		bool IsPipelineCompatible(RenderPipeline renderPipeline);
	}
}