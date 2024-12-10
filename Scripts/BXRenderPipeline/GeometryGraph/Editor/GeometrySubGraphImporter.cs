using System.Collections;
using System.Collections.Generic;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace BXGeometryGraph
{
    [ExcludeFromPreset]
    [ScriptedImporter(30, Extension, -905)]
    public class GeometrySubGraphImporter : ScriptedImporter
    {
        public const string Extension = "geometrysubgraph";

        public override void OnImportAsset(AssetImportContext ctx)
        {
            throw new System.NotImplementedException();
        }
    }
}
