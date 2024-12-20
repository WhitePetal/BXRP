using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace BXGeometryGraph
{
    [ExcludeFromPreset]
    [ScriptedImporter(132, Extension, -902)]
    public class GeometryGraphImporter : ScriptedImporter
    {
        public const string Extension = "geometrygraph";

        const string k_ErrorGeometry = @"
Geometry ""Hidden/GraphErrorGeometry""
{
}";

        internal static bool subtargetNotFoundError = false;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            Debug.Log("Import");
            var importLog = new AssetImportErrorLog(ctx);
            string path = ctx.assetPath;

            AssetCollection assetCollection = new AssetCollection();
            MinimalGraphData.GatherMinimalDependenciesFromFile(assetPath, assetCollection);

            var textGraph = File.ReadAllText(path, Encoding.UTF8);
            var graph = new GraphData
            {
                messageManager = new MessageManager(),
                assetGuid = AssetDatabase.AssetPathToGUID(path)
            };
            MultiJson.Deserialize(graph, textGraph);
            if (subtargetNotFoundError)
            {
                Debug.LogError($"{ctx.assetPath}: Import Error: Expected active subtarget not found, defaulting to first available.");
                subtargetNotFoundError = false;
            }
            graph.OnEnable();
            graph.ValidateGraph();

            UnityEngine.Object mainObject = new Geometry();

            Texture2D texture = Resources.Load<Texture2D>("Icons/gg_graph_icon");
            ctx.AddObjectToAsset("MainAsset", mainObject, texture);
            ctx.SetMainObject(mainObject);

            var graphDataReadOnly = new GraphDataReadOnly(graph);
            foreach(var target in graph.activeTargets)
            {
                if(target is IHasMetadata iHasMetadata)
                {
                    var metadata = iHasMetadata.GetMetadataObject(graphDataReadOnly);
                    if (metadata == null)
                        continue;

                    metadata.hideFlags = HideFlags.HideInHierarchy;
                    ctx.AddObjectToAsset($"{iHasMetadata.identifier}:Metadata", metadata);
                }
            }

            // In case a target couldn't be imported properly, we register a dependency to reimport this GeometryGraph when the current render pipeline type changes
            if (graph.allPotentialTargets.Any(t => t is MultiJsonInternal.UnknownTargetType))
                ctx.DependsOnCustomDependency(RenderPipelineChangedCallback.k_CustomDependencyKey);

            var ggMetadata = ScriptableObject.CreateInstance<GeometryGraphMetadata>();
            ggMetadata.hideFlags = HideFlags.HideInHierarchy;
            ggMetadata.assetDependencies = new List<UnityEngine.Object>();

            foreach(var asset in assetCollection.assets)
            {
                if (asset.Value.HasFlag(AssetCollection.Flags.IncludeInExportPackage))
                {
                    // this sucks that we have to fully load these assets just to set the reference,
                    // which then gets serialized as the GUID that we already have here.  :P

                    var dependencyPath = AssetDatabase.GUIDToAssetPath(asset.Key);
                    if (!string.IsNullOrEmpty(dependencyPath))
                    {
                        ggMetadata.assetDependencies.Add(
                            AssetDatabase.LoadAssetAtPath(dependencyPath, typeof(UnityEngine.Object)));
                    }
                }
            }

            List<GraphInputData> inputInspectorDataList = new List<GraphInputData>();
            foreach (AbstractGeometryProperty property in graph.properties)
            {
                // Don't write out data for non-exposed blackboard items
                if (!property.isExposed)
                    continue;

                // VTs are treated differently
                //if (property is VirtualTextureShaderProperty virtualTextureShaderProperty)
                    //inputInspectorDataList.Add(MinimalCategoryData.ProcessVirtualTextureProperty(virtualTextureShaderProperty));
                //else
                    inputInspectorDataList.Add(new GraphInputData() { referenceName = property.referenceName, propertyType = property.propertyType, isKeyword = false });
            }
            //foreach (ShaderKeyword keyword in graph.keywords)
            //{
            //    // Don't write out data for non-exposed blackboard items
            //    if (!keyword.isExposed)
            //        continue;

            //    var sanitizedReferenceName = keyword.referenceName;
            //    if (keyword.keywordType == KeywordType.Boolean && keyword.referenceName.Contains("_ON"))
            //        sanitizedReferenceName = sanitizedReferenceName.Replace("_ON", String.Empty);

            //    inputInspectorDataList.Add(new GraphInputData() { referenceName = sanitizedReferenceName, keywordType = keyword.keywordType, isKeyword = true });
            //}

            ggMetadata.categoryDatas = new List<MinimalCategoryData>();
            foreach(CategoryData categoryData in graph.categories)
            {
                // Don't write out empty categories
                if (categoryData.childCount == 0)
                    continue;

                MinimalCategoryData mcd = new MinimalCategoryData()
                {
                    categoryName = categoryData.name,
                    propertyDatas = new List<GraphInputData>()
                };
                foreach(var input in categoryData.Children)
                {
                    GraphInputData propData;
                    // Only write out data for exposed blackboard items
                    if (input.isExposed == false)
                        continue;

                    // VTs are treated differently
                    //if (input is VirtualTextureShaderProperty virtualTextureShaderProperty)
                    //{
                    //    propData = MinimalCategoryData.ProcessVirtualTextureProperty(virtualTextureShaderProperty);
                    //    inputInspectorDataList.RemoveAll(inputData => inputData.referenceName == propData.referenceName);
                    //    mcd.propertyDatas.Add(propData);
                    //    continue;
                    //}
                    //else if (input is ShaderKeyword keyword)
                    //{
                    //    var sanitizedReferenceName = keyword.referenceName;
                    //    if (keyword.keywordType == KeywordType.Boolean && keyword.referenceName.Contains("_ON"))
                    //        sanitizedReferenceName = sanitizedReferenceName.Replace("_ON", String.Empty);

                    //    propData = new GraphInputData() { referenceName = sanitizedReferenceName, keywordType = keyword.keywordType, isKeyword = true };
                    //}
                    //else
                    //{
                        var prop = input as AbstractGeometryProperty;
                        propData = new GraphInputData() { referenceName = input.referenceName, propertyType = prop.propertyType, isKeyword = false };
                    //}

                    mcd.propertyDatas.Add(propData);
                    inputInspectorDataList.Remove(propData);
                }
                ggMetadata.categoryDatas.Add(mcd);
            }

            // Any uncategorized elements get tossed into an un-named category at the top as a fallback
            if (inputInspectorDataList.Count > 0)
            {
                ggMetadata.categoryDatas.Insert(0, new MinimalCategoryData() { categoryName = "", propertyDatas = inputInspectorDataList });
            }

            ctx.AddObjectToAsset("GGInternal:Metadata", ggMetadata);

            // declare dependencies
            foreach (var asset in assetCollection.assets)
            {
                if (asset.Value.HasFlag(AssetCollection.Flags.SourceDependency))
                {
                    ctx.DependsOnSourceAsset(asset.Key);

                    // I'm not sure if this warning below is actually used or not, keeping it to be safe
                    var assetPath = AssetDatabase.GUIDToAssetPath(asset.Key);

                    // Ensure that dependency path is relative to project
                    if (!string.IsNullOrEmpty(assetPath) && !assetPath.StartsWith("Packages/") && !assetPath.StartsWith("Assets/"))
                    {
                        importLog.LogWarning($"Invalid dependency path: {assetPath}", mainObject);
                    }
                }

                // NOTE: dependencies declared by GatherDependenciesFromSourceFile are automatically registered as artifact dependencies
                // HOWEVER: that path ONLY grabs dependencies via MinimalGraphData, and will fail to register dependencies
                // on GUIDs that don't exist in the project.  For both of those reasons, we re-declare the dependencies here.
                if (asset.Value.HasFlag(AssetCollection.Flags.ArtifactDependency))
                {
                    ctx.DependsOnArtifact(asset.Key);
                }
            }
        }

        internal class AssetImportErrorLog : MessageManager.IErrorLog
        {
            AssetImportContext ctx;
            public AssetImportErrorLog(AssetImportContext ctx)
            {
                this.ctx = ctx;
            }

            public void LogError(string message, UnityEngine.Object context = null)
            {
                // Note: if you get sent here by clicking on a ShaderGraph error message,
                // this is a bug in the scripted importer system, not being able to link import error messages to the imported asset
                ctx.LogImportError(message, context);
            }

            public void LogWarning(string message, UnityEngine.Object context = null)
            {
                ctx.LogImportWarning(message, context);
            }
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
