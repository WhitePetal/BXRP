using BXGraphing;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BXGeometryGraph
{
    [System.Serializable]
    public class GeometryGraph : AbstructGeometryGraph, IGeometryGraph
    {
		public IMasterNode masterNode
		{
			get { return GetNodes<INode>().OfType<IMasterNode>().FirstOrDefault(); }
		}

        public string GetGeometry(string geometryName, GenerationMode mode, out List<PropertyCollector.TextureInfo> configuredTextures)
        {
			return masterNode.GetGeometry(mode, geometryName, out configuredTextures);
        }

		public void LoadedFromDisk()
		{
			OnEnable();
			ValidateGraph();
		}
	}
}
