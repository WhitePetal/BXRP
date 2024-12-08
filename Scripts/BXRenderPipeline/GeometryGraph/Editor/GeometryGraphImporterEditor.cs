using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace BXGeometryGraph
{
	[CustomEditor(typeof(GeometryGraphImporter))]
	public class GeometryGraphImporterEditor : ScriptedImporterEditor
	{
		public override void OnInspectorGUI()
		{
			if (GUILayout.Button("Open Goemetry Editor"))
			{
				AssetImporter importer = target as AssetImporter;
				Debug.Assert(importer != null, "importer != null");
				ShowGraphEditWindow(importer.assetPath);
			}
		}

		internal static bool ShowGraphEditWindow(string path)
		{
			var guid = AssetDatabase.AssetPathToGUID(path);
			var extension = Path.GetExtension(path);
			Debug.Log("extension: " + extension);
			if (extension != ".geometrygraph" && extension != ".layeredgeometrygraph" && extension != ".geometrysubgraph" && extension != ".geometryremapgraph")
				return false;

			var foundWindow = false;
			foreach (var w in Resources.FindObjectsOfTypeAll<GeometryGraphEditWindow>())
			{
				//if (w.selectedGuid == guid)
				//{
				//	foundWindow = true;
				//	w.Focus();
				//}
			}

			if (!foundWindow)
			{
				var window = CreateInstance<GeometryGraphEditWindow>();
				window.Show();
				window.Initialize(guid);
			}
			return true;
		}
	}
}
