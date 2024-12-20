using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.Callbacks;
using UnityEngine;

namespace BXGeometryGraph
{
	[CustomEditor(typeof(GeometryGraphImporter))]
	public class GeometryGraphImporterEditor : ScriptedImporterEditor
	{
        protected override bool needsApplyRevert => false;

        public override void OnInspectorGUI()
		{
            GraphData GetGraphData(AssetImporter importer)
            {
                var textGraph = File.ReadAllText(importer.assetPath, Encoding.UTF8);
                var graphObject = CreateInstance<GraphObject>();
                graphObject.hideFlags = HideFlags.HideAndDontSave;
                bool isSubGraph;
                var extension = Path.GetExtension(importer.assetPath).Replace(".", "");
                switch (extension)
                {
                    case GeometryGraphImporter.Extension:
                        isSubGraph = false;
                        break;
                    case GeometrySubGraphImporter.Extension:
                        isSubGraph = true;
                        break;
                    default:
                        throw new Exception($"Invalid file extension {extension}");
                }
                var assetGuid = AssetDatabase.AssetPathToGUID(importer.assetPath);
                graphObject.graph = new GraphData
                {
                    assetGuid = assetGuid,
                    isSubGraph = isSubGraph,
                    messageManager = null
                };
                MultiJson.Deserialize(graphObject.graph, textGraph);
                graphObject.graph.OnEnable();
                graphObject.graph.ValidateGraph();
                return graphObject.graph;
            }

            if (GUILayout.Button("Open Geometry Editor"))
            {
                AssetImporter importer = target as AssetImporter;
                Debug.Assert(importer != null, "importer != null");
                ShowGraphEditWindow(importer.assetPath);
            }

            using (var horizontalScope = new GUILayout.HorizontalScope("box"))
            {
                AssetImporter importer = target as AssetImporter;
                string assetName = Path.GetFileNameWithoutExtension(importer.assetPath);
                string path = String.Format("Temp/GeneratedFromGraph-{0}.shader", assetName.Replace(" ", ""));
                bool alreadyExists = File.Exists(path);
                bool update = false;
                bool open = false;

                //if (GUILayout.Button("View Generated Shader"))
                //{
                //    update = true;
                //    open = true;
                //}

                //if (alreadyExists && GUILayout.Button("Regenerate"))
                //    update = true;

                //if (update)
                //{
                //    var graphData = GetGraphData(importer);
                //    var generator = new Generator(graphData, null, GenerationMode.ForReals, assetName, humanReadable: true);
                //    if (!GraphUtil.WriteToFile(path, generator.generatedShader))
                //        open = false;
                //}

                if (open)
                    GraphUtil.OpenFile(path);
            }
            if (Unsupported.IsDeveloperMode())
            {
                //if (GUILayout.Button("View Preview Shader"))
                //{
                //    AssetImporter importer = target as AssetImporter;
                //    string assetName = Path.GetFileNameWithoutExtension(importer.assetPath);
                //    string path = String.Format("Temp/GeneratedFromGraph-{0}-Preview.shader", assetName.Replace(" ", ""));

                //    var graphData = GetGraphData(importer);
                //    var generator = new Generator(graphData, null, GenerationMode.Preview, $"{assetName}-Preview", humanReadable: true);
                //    if (GraphUtil.WriteToFile(path, generator.generatedShader))
                //        GraphUtil.OpenFile(path);
                //}
            }
            //if (GUILayout.Button("Copy Shader"))
            //{
            //    AssetImporter importer = target as AssetImporter;
            //    string assetName = Path.GetFileNameWithoutExtension(importer.assetPath);

            //    var graphData = GetGraphData(importer);
            //    var generator = new Generator(graphData, null, GenerationMode.ForReals, assetName, humanReadable: true);
            //    GUIUtility.systemCopyBuffer = generator.generatedShader;
            //}

            ApplyRevertGUI();

            //if (geometryEditor)
            //{
            //    EditorGUILayout.Space();
            //    materialEditor.DrawHeader();
            //    using (new EditorGUI.DisabledGroupScope(true))
            //        materialEditor.OnInspectorGUI();
            //}
        }

        public override void OnEnable()
        {
            base.OnEnable();
            //AssetImporter importer = target as AssetImporter;
            //var material = AssetDatabase.LoadAssetAtPath<Material>(importer.assetPath);
            //if (material)
            //    materialEditor = (MaterialEditor)CreateEditor(material);
        }

        public override void OnDisable()
        {
            base.OnDisable();
            //if (materialEditor != null)
            //    DestroyImmediate(materialEditor);
        }

        internal static bool ShowGraphEditWindow(string path)
        {
            var guid = AssetDatabase.AssetPathToGUID(path);
            var extension = Path.GetExtension(path);
            if (string.IsNullOrEmpty(extension))
                return false;
            // Path.GetExtension returns the extension prefixed with ".", so we remove it. We force lower case such that
            // the comparison will be case-insensitive.
            extension = extension.Substring(1).ToLowerInvariant();
            if (extension != GeometryGraphImporter.Extension && extension != GeometrySubGraphImporter.Extension)
                return false;

            foreach (var w in Resources.FindObjectsOfTypeAll<GeometryGraphEditWindow>())
            {
                if (w.selectedGuid == guid)
                {
                    w.Focus();
                    return true;
                }
            }

            var window = EditorWindow.CreateWindow<GeometryGraphEditWindow>(typeof(GeometryGraphEditWindow), typeof(SceneView));
            window.Initialize(guid);
            window.Focus();
            return true;
        }

        [OnOpenAsset(0)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var path = AssetDatabase.GetAssetPath(instanceID);
            return ShowGraphEditWindow(path);
        }
    }
}
