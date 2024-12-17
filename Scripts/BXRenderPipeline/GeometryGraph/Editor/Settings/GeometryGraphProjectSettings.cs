using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BXGeometryGraph
{
    [FilePath("ProjectSettings/GeometryGraphSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class GeometryGraphProjectSettings : ScriptableSingleton<GeometryGraphProjectSettings>
    {
        [SerializeField]
        internal int shaderVariantLimit = 2048;
        [SerializeField]
        internal int customInterpolatorErrorThreshold = 32;
        [SerializeField]
        internal int customInterpolatorWarningThreshold = 16;
    }
}
