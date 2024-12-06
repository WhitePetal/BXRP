using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace BXGeometryGraph
{
    [ScriptedImporter(13, GeometryGraphImporter.GeometryGraphExtension)]
    public class GeometryGraphImporter : ScriptedImporter
    {
        public const string GeometryGraphExtension = "geometrygraph";

        const string k_ErrorGeometry = @"
Geometry ""Hidden/GraphErrorGeometry""
{
}";

        public override void OnImportAsset(AssetImportContext ctx)
        {
            Debug.Log("Import");
            var oldGeometry = AssetDatabase.LoadAssetAtPath<Geometry>(ctx.assetPath);
            if(oldGeometry != null)
            {

            }

            List<PropertyCollector.TextureInfo> configuredTextures;
            var text = GetGeometryText(ctx.assetPath, out configuredTextures);
            var geometry = GeometryUtil.CreateShaderAsset(text);

            ctx.AddObjectToAsset("MainAsset", geometry);
            ctx.SetMainObject(geometry);
        }

        internal static string GetGeometryText(string path, out List<PropertyCollector.TextureInfo> configuredTextures)
        {
            string geometryString = null;
            var geometryName = Path.GetFileNameWithoutExtension(path);
            try
            {
                var textGraph = File.ReadAllText(path, Encoding.UTF8);
                var graph = JsonUtility.FromJson<GeometryGraph>(textGraph);
                graph.LoadedFromDisk();

                if (!string.IsNullOrEmpty(graph.path))
                    geometryName = graph.path + "/" + geometryName;
                geometryString = graph.GetGeometry(geometryName, GenerationMode.ForReals, out configuredTextures);
            }
            catch (Exception)
            {
                // ignored
            }
            configuredTextures = new List<PropertyCollector.TextureInfo>();
            return geometryString ?? k_ErrorGeometry.Replace("Hidden/GraphErrorGeometry", geometryName);
        }
    }

    //class GeometryGraphAssetPostProcess : AssetPostprocessor
    //{
        //static void RegisterGeometrys(string[] paths)
        //{
        //    foreach (var path in paths)
        //    {
        //        if (!path.EndsWith(GeometryGraphImporter.GeometryGraphExtension, StringComparison.InvariantCultureIgnoreCase))
        //            continue;

        //        var mainObj = AssetDatabase.LoadMainAssetAtPath(path);
        //        if (mainObj is Shader)
        //            ShaderUtil.RegisterShader((Shader)mainObj);

        //        var objs = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
        //        foreach (var obj in objs)
        //        {
        //            if (obj is Shader)
        //                ShaderUtil.RegisterShader((Shader)obj);
        //        }
        //    }
        //}

        //static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        //{
        //    MaterialGraphEditWindow[] windows = Resources.FindObjectsOfTypeAll<MaterialGraphEditWindow>();
        //    foreach (var matGraphEditWindow in windows)
        //    {
        //        matGraphEditWindow.forceRedrawPreviews = true;
        //    }
        //    RegisterGeometrys(importedAssets);
        //}
    //}
}
