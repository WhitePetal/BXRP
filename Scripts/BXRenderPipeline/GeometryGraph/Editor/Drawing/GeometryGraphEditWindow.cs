using BXGraphing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
	public class GeometryGraphEditWindow : EditorWindow
	{
		[SerializeField]
		private string m_Selected;

		[SerializeField]
		private GraphObject m_GraphObject;

		[NonSerialized]
		private bool m_HasError;

		[NonSerialized]
		public bool forceRedrawPreviews = false;

		private ColorSpace m_ColorSpace;

		private GraphEditorView m_GraphEditorView;

		internal GraphEditorView graphEditorView
		{
			get { return m_GraphEditorView; }
			set
			{
				if(m_GraphEditorView != null)
				{
					m_GraphEditorView.RemoveFromHierarchy();
					m_GraphEditorView.Dispose();
				}

				m_GraphEditorView = value;
				if(m_GraphEditorView != null)
				{
					m_GraphEditorView.saveRequested += UpdateAsset;
					m_GraphEditorView.convertToSubgraphRequested += ToSubGraph;
					m_GraphEditorView.showInProjectRequested += PingAsset;
					rootVisualElement.contentContainer.Add(graphEditorView);
				}
			}
		}

		private GraphObject graphObject
		{
			get { return m_GraphObject; }
			set
			{
				if (m_GraphObject != null)
					DestroyImmediate(m_GraphObject);
				m_GraphObject = value;
			}
		}

		public string selectedGuid
		{
			get { return m_Selected; }
			private set { m_Selected = value; }
		}

		private void Update()
		{
			if (m_HasError)
				return;

			if(PlayerSettings.colorSpace != m_ColorSpace)
			{
				graphEditorView = null;
				m_ColorSpace = PlayerSettings.colorSpace;
			}

			try
			{
				if(graphObject == null && selectedGuid != null)
				{
					var guid = selectedGuid;
					selectedGuid = null;
					Initialize(guid);
				}

				if(graphObject == null)
				{
					Close();
					return;
				}

				var geometryGraph = graphObject.graph as AbstructGeometryGraph;
				if (geometryGraph == null)
					return;
				if(graphEditorView == null)
				{
					var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GUIDToAssetPath(selectedGuid));
					graphEditorView = new GraphEditorView(this, geometryGraph, asset.name) { persistenceKey = selectedGuid };
					m_ColorSpace = PlayerSettings.colorSpace;
				}

				if (forceRedrawPreviews)
				{
					// Redraw all previews
					foreach (INode node in m_GraphObject.graph.GetNodes<INode>())
						node.Dirty(ModificationScope.Node);
					forceRedrawPreviews = false;
				}

				graphEditorView.HandleGraphChanges();
				graphObject.graph.ClearChanges();
			}
			catch(Exception e)
			{
				m_HasError = true;
				m_GraphEditorView = null;
				graphObject = null;
				Debug.LogException(e);
				throw;
			}
		}

		private void OnDisable()
		{
			graphEditorView = null;
		}

		private void OnDestroy()
		{
			if(graphObject != null)
			{
				if (graphObject.isDirty && EditorUtility.DisplayDialog("Geometry Graph Has Been Modified", "Do you want to save the changes you made in the geometry graph?\n\nYour changes will be lost if you don't save them.", "Save", "Don't Save"))
					UpdateAsset();
				Undo.ClearUndo(graphObject);
				DestroyImmediate(graphObject);
			}

			graphEditorView = null;
		}

		private void UpdateDependantGraphs()
		{
			string[] lookFor = new string[] { "Assets" };
			var guids = AssetDatabase.FindAssets("t:geometry", lookFor);
			foreach(string guid in guids)
			{
				if (AssetDatabase.GUIDToAssetPath(guid).ToLower().EndsWith(GeometryGraphImporter.GeometryGraphExtension))
				{
					var path = AssetDatabase.GUIDToAssetPath(guid);

					var textGraph = File.ReadAllText(path, Encoding.UTF8);
					var graph = JsonUtility.FromJson<GeometryGraph>(textGraph);
					graph.LoadedFromDisk();
					
					foreach(SubGraphNode graphNode in graph.GetNodes<SubGraphNode>())
					{
						//var subpath = AssetDatabase.GetAssetPath(graphNode.subGraphAsset);
						//var subguid = AssetDatabase.AssetPathToGUID(subpath);
						//if(subguid == selectedGuid)
						//{
						//	UpdateGeometryGraphOnDisk(path, graph);
						//}
					}
				}
			}
		}

		public void PingAsset()
		{
			if(selectedGuid != null)
			{
				var path = AssetDatabase.GUIDToAssetPath(selectedGuid);
				var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
				EditorGUIUtility.PingObject(asset);
			}
		}

		public void UpdateAsset()
		{
			if(selectedGuid != null && graphObject != null)
			{
				var path = AssetDatabase.GUIDToAssetPath(selectedGuid);
				if (string.IsNullOrEmpty(path) || graphObject == null)
					return;

				if (m_GraphObject.graph.GetType() == typeof(GeometryGraph))
					UpdateGeometryGraphOnDisk(path);

				//if (m_GraphObject.graph.GetType() == typeof(SubGraph))
				//UpdateAbstractSubgraphOnDisk<SubGraph>(path);

				graphObject.isDirty = false;
				var windows = Resources.FindObjectsOfTypeAll<GeometryGraphEditWindow>();
				foreach(var geometryGraphEditWindow in windows)
				{
					geometryGraphEditWindow.Rebuild();
				}
			}
		}

		public void ToSubGraph()
		{
			var path = EditorUtility.SaveFilePanelInProject("Save subgraph", "New SubGraph", "GeometrySubGraph", "");
			path = path.Replace(Application.dataPath, "Assets");
			if (path.Length == 0)
				return;

			graphObject.RegisterCompleteObjectUndo("Convert Tp Subgraph");
			var graphView = graphEditorView.graphView;

			var nodes = graphView.selection.OfType<GeometryNodeView>().Where(x => !(x.node is PropertyNode)).Select(x => x.node as INode).ToArray();
			var bounds = Rect.MinMaxRect(float.PositiveInfinity, float.PositiveInfinity, float.NegativeInfinity, float.NegativeInfinity);
			foreach(var node in nodes)
			{
				var center = node.drawState.position.center;
				bounds = Rect.MinMaxRect(
					Mathf.Min(bounds.xMin, center.x),
					Mathf.Min(bounds.yMin, center.y),
					Mathf.Max(bounds.xMax, center.x),
					Mathf.Max(bounds.yMax, center.y));
			}
			var middle = bounds.center;
			bounds.center = Vector2.zero;

			// Collect the property nodes and get the corresponding properties
			var propertyNodeGuids = graphView.selection.OfType<GeometryNodeView>().Where(x => (x.node is PropertyNode)).Select(x => ((PropertyNode)x.node).propertyGuid);
			var metaProperties = graphView.graph.properties.Where(x => propertyNodeGuids.Contains(x.guid));

			var copyPasteGraph = new CopyPasteGraph(
				graphView.graph.guid,
				graphView.selection.OfType<GeometryNodeView>().Where(x => !(x.node is PropertyNode)).Select(x => x.node as INode),
				graphView.selection.OfType<UnityEditor.Experimental.GraphView.Edge>().Select(x => x.userData as IEdge),
				graphView.selection.OfType<BlackboardField>().Select(x => x.userData as IGeometryProperty),
				metaProperties);

			var deserialized = CopyPasteGraph.FromJson(JsonUtility.ToJson(copyPasteGraph, false));
			if (deserialized == null)
				return;

			//var subGraph = new SubGraph();
			//var subGraphOutputNode = new SubGraphOutputNode();
			//{
			//	var drawState = subGraphOutputNode.drawState;
			//	drawState.position = new Rect(new Vector2(bounds.xMax + 200f, 0f), drawState.position.size);
			//	subGraphOutputNode.drawState = drawState;
			//}
			//subGraph.AddNode(subGraphOutputNode);

			var nodeGuidMap = new Dictionary<Guid, Guid>();
			foreach(var node in deserialized.GetNodes<INode>())
			{
				var oldGuid = node.guid;
				var newGuid = node.RewriteGuid();
				nodeGuidMap[oldGuid] = newGuid;
				var drawState = node.drawState;
				drawState.position = new Rect(drawState.position.position - middle, drawState.position.size);
				node.drawState = drawState;
				//subGraph.AddNode(node);
			}

			// figure out what needs remapping
			var externalOutputSlots = new List<IEdge>();
			var externalInputSlots = new List<IEdge>();
			foreach(var edge in deserialized.edges)
			{
				var outputSlot = edge.outputSlot;
				var inputSlot = edge.inputSlot;

				Guid remappedOutputNodeGuid;
				Guid remappedInputNodeGuid;
				var outputSlotExistsInSubgraph = nodeGuidMap.TryGetValue(outputSlot.nodeGuid, out remappedOutputNodeGuid);
				var inputSlotExistsInSubgraph = nodeGuidMap.TryGetValue(inputSlot.nodeGuid, out remappedInputNodeGuid);

				// pasting nice internal links!
				if(outputSlotExistsInSubgraph && inputSlotExistsInSubgraph)
				{
					var outputSlotRef = new SlotReference(remappedOutputNodeGuid, outputSlot.slotId);
					var inputSlotRef = new SlotReference(remappedInputNodeGuid, inputSlot.slotId);
					//subGraph.Connect(outputSlotRef, inputSlot);
				}
				// one edge needs to go to outside world
				else if (outputSlotExistsInSubgraph)
				{
					externalInputSlots.Add(edge);
				}
				else if (inputSlotExistsInSubgraph)
				{
					externalOutputSlots.Add(edge);
				}
			}

			// Find the unique edges coming INTO the graph
			var uniqueIncomingEdges = externalOutputSlots.GroupBy(
				edge => edge.outputSlot,
				edge => edge,
				(key, edges) => new { slotRef = key, edges = edges.ToList() });

			var externalInputNeedingConnection = new List<KeyValuePair<IEdge, IGeometryProperty>>();
			foreach(var group in uniqueIncomingEdges)
			{
				var sr = group.slotRef;
				var fromNode = graphObject.graph.GetNodeFromGuid(sr.nodeGuid);
				var fromSlot = fromNode.FindOutputSlot<GeometrySlot>(sr.slotId);

				IGeometryProperty prop;
				switch (fromSlot.concreteValueType)
				{
					case ConcreteSlotValueType.Texture2D:
						prop = new TextureGeometryProperty();
						break;
					//case ConcreteSlotValueType.Cubemap:
					//	prop = new CubemapGeometryProperty();
					//	break;
					case ConcreteSlotValueType.Vector4:
						prop = new Vector4GeometryProperty();
						break;
					case ConcreteSlotValueType.Vector3:
						prop = new Vector3GeometryProperty();
						break;
					case ConcreteSlotValueType.Vector2:
						prop = new Vector2GeometryProperty();
						break;
					case ConcreteSlotValueType.Vector1:
						prop = new Vector1GeometryProperty();
						break;
					//case ConcreteSlotValueType.Boolean:
					//	prop = new BooleanGeometryProperty();
					//	break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				if(prop != null)
				{
					var geometryGraph = (AbstructGeometryGraph)graphObject.graph;
					var fromPropertyNode = fromNode as PropertyNode;
					var fromProperty = fromPropertyNode != null ? geometryGraph.properties.FirstOrDefault(p => p.guid == fromPropertyNode.propertyGuid) : null;
					prop.displayName = fromProperty != null ? fromProperty.displayName : fromNode.name;
					//subGraph.AddGeometryProperty(prop);
					var propNode = new PropertyNode();
					{
						var drawState = propNode.drawState;
						drawState.position = new Rect(new Vector2(bounds.xMin - 300f, 0f), drawState.position.size);
						propNode.drawState = drawState;
					}
					//subGraph.AddNode(propNode);
					propNode.propertyGuid = prop.guid;

					foreach(var edge in group.edges)
					{
						//subGraph.Connect(
						//	new SlotReference(propNode.guid, PropertyNode.OutputSlotId),
						//	new SlotReference(nodeGuidMap[edge.inputSlot.nodeGuid], edge.inputSlot.slotId));
						//externalInputNeedingConnection.Add(new KeyValuePair<IEdge, IGeometryProperty>(edge, prop));
					}
				}
			}

			var uniqueOutgoingEdges = externalInputSlots.GroupBy(
				edge => edge.inputSlot,
				edge => edge,
				(key, edges) => new { slot = key, edges = edges.ToList() });

			var externalOutputsNeedingConnection = new List<KeyValuePair<IEdge, IEdge>>();
			foreach(var group in uniqueOutgoingEdges)
			{
				//var outputNode = subGraph.outputNode;
				//var slotId = outputNode.AddSlot();

				//var inputSlotRef = new SlotReference(outputNode.guid, slotId);

				//foreach (var edge in group.edges)
				//{
				//	var newEdge = subGraph.Connect(new SlotReference(nodeGuidMap[edge.outputSlot.nodeGuid], edge.outputSlot.slotId), inputSlotRef);
				//	externalOutputsNeedingConnection.Add(new KeyValuePair<IEdge, IEdge>(edge, newEdge));
				//}
			}

			//File.WriteAllText(path, EditorJsonUtility.ToJson(subGraph));
			AssetDatabase.ImportAsset(path);

			//var loadedSubGraph = AssetDatabase.LoadAssetAtPath(path, typeof(GeometrySubGraphAsset)) as GeometrySubGraphAsset;
			//if (loadedSubGraph == null)
			//	return;

			//var subGraphNode = new SubGraphNode();
			//var ds = subGraphNode.drawState;
			//ds.position = new Rect(middle - new Vector2(100f, 150f), Vector2.zero);
			//subGraphNode.drawState = ds;
			//graphObject.graph.AddNode(subGraphNode);
			//subGraphNode.subGraphAsset = loadedSubGraph;

			//foreach (var edgeMap in externalInputNeedingConnection)
			//{
			//	graphObject.graph.Connect(edgeMap.Key.outputSlot, new SlotReference(subGraphNode.guid, edgeMap.Value.guid.GetHashCode()));
			//}

			//foreach (var edgeMap in externalOutputsNeedingConnection)
			//{
			//	graphObject.graph.Connect(new SlotReference(subGraphNode.guid, edgeMap.Value.inputSlot.slotId), edgeMap.Key.inputSlot);
			//}

			//graphObject.graph.RemoveElements(
			//	graphView.selection.OfType<GeometryNodeView>().Select(x => x.node as INode),
			//	Enumerable.Empty<IEdge>());
			//graphObject.graph.ValidateGraph();
		}

		//private void UpdateAbstractSubgraphOnDisk<T>(string path) where T : SubGraph
		//{
		//	var graph = graphObject.graph as T;
		//	if (graph == null)
		//		return;

		//	File.WriteAllText(path, EditorJsonUtility.ToJson(graph, true));
		//	AssetDatabase.ImportAsset(path);

		//	UpdateDependantGraphs();
		//}

		private void UpdateGeometryGraphOnDisk(string path)
		{
			var graph = graphObject.graph as IGeometryGraph;
			if (graph == null)
				return;

			UpdateGeometryGraphOnDisk(path, graph);
		}

		private static void UpdateGeometryGraphOnDisk(string path, IGeometryGraph graph)
		{
			var shaderImporter = AssetImporter.GetAtPath(path) as GeometryGraphImporter;
			if (shaderImporter == null)
				return;

			File.WriteAllText(path, EditorJsonUtility.ToJson(graph, true));
			shaderImporter.SaveAndReimport();
			AssetDatabase.ImportAsset(path);
		}

		private void Rebuild()
		{
			if (graphObject != null && graphObject.graph != null)
			{
				var subNodes = graphObject.graph.GetNodes<SubGraphNode>();
				//foreach (var node in subNodes)
					//node.UpdateSlots();
			}
		}

		public void Initialize(string assetGuid)
		{
			try
			{
				m_ColorSpace = PlayerSettings.colorSpace;

				var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GUIDToAssetPath(assetGuid));
				if (asset == null)
					return;

				if (!EditorUtility.IsPersistent(asset))
					return;

				if (selectedGuid == assetGuid)
					return;

				var path = AssetDatabase.GetAssetPath(asset);
				var extension = Path.GetExtension(path);
				Type graphType;
				switch (extension)
				{
					case ".geometrygraph":
						graphType = typeof(GeometryGraph);
						break;
					//case ".geometrysubgraph":
						//graphType = typeof(SubGraph);
						//break;
					default:
						return;
				}

				selectedGuid = assetGuid;

				var textGraph = File.ReadAllText(path, Encoding.UTF8);
				graphObject = CreateInstance<GraphObject>();
				graphObject.hideFlags = HideFlags.HideAndDontSave;
				graphObject.graph = JsonUtility.FromJson(textGraph, graphType) as IGraph;
				if(graphObject.graph == null)
                {
					graphObject.graph = new GeometryGraph();
                }
				graphObject.graph.OnEnable();
				graphObject.graph.ValidateGraph();

				graphEditorView = new GraphEditorView(this, m_GraphObject.graph as AbstructGeometryGraph, asset.name) { persistenceKey = selectedGuid };
				graphEditorView.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

				titleContent = new GUIContent(asset.name);

				Repaint();
			}
			catch (Exception e)
			{
				m_HasError = true;
				m_GraphEditorView = null;
				graphObject = null;
				Debug.LogException(e);
				throw;
			}
		}

		void OnGeometryChanged(GeometryChangedEvent evt)
		{
			graphEditorView.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
			graphEditorView.graphView.FrameAll();
		}
	}
}
