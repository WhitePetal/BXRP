using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
	public class GenerationResults
	{
		public string geometry { get; set; }
		public List<PropertyCollector.TextureInfo> configuredTextures;
		public PreviewMode previewMode { get; set; }
		public Vector1GeometryProperty outputIdProperty { get; set; }
		//public GeometrySourceMap sourceMap { get; set; }

		public GenerationResults()
		{
			configuredTextures = new List<PropertyCollector.TextureInfo>();
		}
	}
}
