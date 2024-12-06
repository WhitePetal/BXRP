using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    public class GeometryUtil
    {
        public static Geometry CreateShaderAsset(string text)
        {
            return ScriptableObject.CreateInstance<Geometry>();
        }
    }
}
