using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    [System.Serializable]
    public class GeometryGraph
    {
        public string path;

        public void LoadedFromDisk()
        {

        }

        public string GetGeometry(string geometryName, GenerationMode mode, out List<PropertyCollector.TextureInfo> configuredTextures)
        {
            configuredTextures = new List<PropertyCollector.TextureInfo>();
            return "";
        }
    }
}
