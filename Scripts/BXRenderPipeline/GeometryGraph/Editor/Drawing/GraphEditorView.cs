using BXGraphing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
	[Serializable]
	class FloatingWindowsLayout
	{
		public WindowDockingLayout previewLayout = new WindowDockingLayout();
		public WindowDockingLayout blackBoardLayout = new WindowDockingLayout();
		public Vector2 masterPreviewSize = new Vector2(200, 400);
	}

	public class GraphEditorView : VisualElement, IDisposable
	{
		private GeometryGraphView m_GraphView;
		//private MasterPreviewViews m_MasterPreviewView;

		private EditorWindow m_EditorWindow;

		private AbstructGeometryGraph m_Graph;
		private PreviewManager m_PreviewManager;
		private SearchWindowProvider m_SearchWindowProvider;
		private IEdgeConnectorListener m_EdgeConnectorListener;
		//private BlackboardFieldProvider m_BlackboardProvider;

		private const string k_FloatingWindowsLayoutKey = "BXGeometryGraph.FloatingWindowsLayout";
		private FloatingWindowsLayout m_FloatingWindowsLayout;

		public string persistenceKey { get; set; }

		public Action saveRequested { get; set; }

		public Action convertToSubgraphRequested
		{
			get { return m_GraphView.onConvertToSubgraphClick; }
			set { m_GraphView.onConvertToSubgraphClick = value; }
		}

		public Action showInProjectRequested { get; set; }

		public GeometryGraphView graphView
		{
			get { return m_GraphView; }
		}

		private PreviewManager previewManager
		{
			get { return m_PreviewManager; }
			set { m_PreviewManager = value; }
		}

		public GraphEditorView(EditorWindow editorWindow, AbstructGeometryGraph graph, string assetName)
		{
			m_Graph = graph;
			this.AddStyleSheetPath("Assets/Scripts/BXRenderPipeline/GeometryGraph/Editor/Resource/Styles/GraphEditorView.uss");
			m_EditorWindow = editorWindow;
			//previewManager = new PreviewManager(graph);

			string serializedWindowLayout = EditorUserSettings.GetConfigValue(k_FloatingWindowsLayoutKey);
			if (!string.IsNullOrEmpty(serializedWindowLayout))
			{
				m_FloatingWindowsLayout = JsonUtility.FromJson<FloatingWindowsLayout>(serializedWindowLayout);
				//if (m_FloatingWindowsLayout.masterPreviewSize.x > 0f && m_FloatingWindowsLayout.masterPreviewSize.y > 0f)
					//previewManager.ResizeMasterPreview(m_FloatingWindowsLayout.masterPreviewSize);
			}

			//previewManager.RenderPreviews();

			var toolbar = new IMGUIContainer(() =>
			{
				GUILayout.BeginHorizontal(EditorStyles.toolbar);
				if (GUILayout.Button("Save Asset", EditorStyles.toolbarButton))
				{
					if (saveRequested != null)
						saveRequested();
				}
				GUILayout.Space(6);
				if (GUILayout.Button("Show In Project", EditorStyles.toolbarButton))
				{
					if (showInProjectRequested != null)
						showInProjectRequested();
				}
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
			});
			Add(toolbar);

			var content = new VisualElement { name = "content" };
			{
				m_GraphView = new GeometryGraphView(graph) { name = "GraphView", persistenceKey = "GeometryGraphView" };
				m_GraphView.SetupZoom(0.05f, ContentZoomer.DefaultMaxScale);
				m_GraphView.AddManipulator(new ContentDragger());
				m_GraphView.AddManipulator(new SelectionDragger());
				m_GraphView.AddManipulator(new RectangleSelector());
				m_GraphView.AddManipulator(new ClickSelector());
				m_GraphView.AddManipulator(new GraphDropTarget(graph));
				m_GraphView.RegisterCallback<KeyDownEvent>(OnSpaceDown);
				content.Add(m_GraphView);

				// TODO
				//m_BlackboardProvider = new BlackboardProvider(assetName, graph);
				//m_GraphView.Add(m_BlackboardProvider.blackboard);
				//Rect blackboardLayout = m_BlackboardProvider.blackboard.layout;
				//blackboardLayout.x = 10f;
				//blackboardLayout.y = 10f;
				//m_BlackboardProvider.blackboard.layout = blackboardLayout;

				//m_MasterPreviewView = new MasterPreviewView(assetName, previewManager, graph) { name = "masterPreview" };

				//WindowDraggable masterPreviewViewDraggable = new WindowDraggable(null, this);
				//m_MasterPreviewView.AddManipulator(masterPreviewViewDraggable);
				//m_GraphView.Add(m_MasterPreviewView);

				// //m_BlackboardProvider.onDragFinished += UpdateSerializedWindowLayout;
				// //m_BlackboardProvider.onResizeFinished += UpdateSerializedWindowLayout;
				// masterPreviewViewDraggable.OnDragFinished += UpdateSerializedWindowLayout;
				// m_MasterPreviewView.previewResizeBorderFrame.OnResizeFinished += UpdateSerializedWindowLayout;

				m_GraphView.graphViewChanged = GraphViewChanged;

				RegisterCallback<GeometryChangedEvent>(ApplySerializewindowLayouts);
			}

			m_SearchWindowProvider = ScriptableObject.CreateInstance<SearchWindowProvider>();
			//m_SearchWindowProvider.Initialize(editorWindow, m_Graph, m_GraphView);
			//m_GraphView.nodeCreationRequest = (c) =>
			//{
			//	m_SearchWindowProvider.connectedPort = null;
			//	SearchWindow.Open(new SearchWindowContext(c.screenMousePosition), m_SearchWindowProvider);
			//};

			m_EdgeConnectorListener = new EdgeConnectorListener(m_Graph, m_SearchWindowProvider);

			foreach (var node in graph.GetNodes<INode>())
				AddNode(node);

			foreach (var edge in graph.edges)
				AddEdge(edge);

			Add(content);
		}

		private void OnSpaceDown(KeyDownEvent evt)
		{
			if(evt.keyCode == KeyCode.Space && !evt.shiftKey && !evt.altKey && !evt.ctrlKey && !evt.commandKey)
			{
				if (graphView.nodeCreationRequest == null)
					return;

				Vector2 referencePosition;
				referencePosition = evt.imguiEvent.mousePosition;
				Vector2 screenPoint = m_EditorWindow.position.position + referencePosition;

				graphView.nodeCreationRequest(new NodeCreationContext() { screenMousePosition = screenPoint });
			}
			else if (evt.keyCode == KeyCode.F1)
			{
				if(m_GraphView.selection.OfType<GeometryNodeView>().Count() == 1)
				{
					var nodeView = (GeometryNodeView)graphView.selection.First();
					if (nodeView.node.documentationURL != null)
						System.Diagnostics.Process.Start(nodeView.node.documentationURL);
				}
			}
		}

		private GraphViewChange GraphViewChanged(GraphViewChange graphViewChange)
		{
			if(graphViewChange.edgesToCreate != null)
			{
				foreach(var edge in graphViewChange.edgesToCreate)
				{
					var leftSlot = edge.output.GetSlot();
					var rightSlot = edge.input.GetSlot();
					if(leftSlot != null && rightSlot != null)
					{
						m_Graph.owner.RegisterCompleteObjectUndo("Connect Edge");
						m_Graph.Connect(leftSlot.slotReference, rightSlot.slotReference);
					}
				}
				graphViewChange.edgesToCreate.Clear();
			}

			if(graphViewChange.movedElements != null)
			{
				foreach(var element in graphViewChange.movedElements)
				{
					var node = element.userData as INode;
					if (node == null)
						continue;

					var drawState = node.drawState;
					drawState.position = element.GetPosition();
					node.drawState = drawState;
				}
			}

			var nodesToUpdate = m_NodeViewHashSet;
			nodesToUpdate.Clear();

			if(graphViewChange.elementsToRemove != null)
			{
				m_Graph.owner.RegisterCompleteObjectUndo("Remove Elements");
				m_Graph.RemoveElements(graphViewChange.elementsToRemove.OfType<GeometryNodeView>().Select(v => (INode)v.node),
					graphViewChange.elementsToRemove.OfType<UnityEditor.Experimental.GraphView.Edge>().Select(e => (IEdge)e.userData));
				foreach(var edge in graphViewChange.elementsToRemove.OfType<UnityEditor.Experimental.GraphView.Edge>())
				{
					if(edge.input != null)
					{
						var geometryNodeView = edge.input.node as GeometryNodeView;
						if (geometryNodeView != null)
							nodesToUpdate.Add(geometryNodeView);
					}
					if(edge.output != null)
					{
						var geometryNodeView = edge.output.node as GeometryNodeView;
						if (geometryNodeView != null)
							nodesToUpdate.Add(geometryNodeView);
					}
				}
			}

			foreach (var node in nodesToUpdate)
				node.UpdatePortInputVisiblities();

			UpdateEdgeColors(nodesToUpdate);

			return graphViewChange;
		}

		private void OnNodeChanged(INode inNode, ModificationScope scope)
		{
			if (m_GraphView == null)
				return;

			var dependentNodes = new List<INode>();
			NodeUtils.CollectNodesNodeFeedsInto(dependentNodes, inNode);
			foreach(var node in dependentNodes)
			{
				var theViews = m_GraphView.nodes.ToList().OfType<GeometryNodeView>();
				var viewsFound = theViews.Where(x => x.node.guid == node.guid).ToList();
				foreach (var drawableNodeData in viewsFound)
					drawableNodeData.OnModified(scope);
			}
		}

		private HashSet<GeometryNodeView> m_NodeViewHashSet = new HashSet<GeometryNodeView>();

		public void HandleGraphChanges()
		{
			//previewManager.HandleGraphChanges();
			//previewManager.RenderPreviews();
			//m_BlackboardProvider.HandleGraphChanges();

			foreach(var node in m_Graph.removedNodes)
			{
				node.UnregisterCallback(OnNodeChanged);
				var nodeView = m_GraphView.nodes.ToList().OfType<GeometryNodeView>().FirstOrDefault(p => p.node != null && p.node.guid == node.guid);
				if(nodeView != null)
				{
					nodeView.Dispose();
					nodeView.userData = null;
					m_GraphView.RemoveElement(nodeView);
				}
			}

			foreach(var node in m_Graph.addedNodes)
			{
				AddNode(node);
			}

			foreach(var node in m_Graph.pastedNodes)
			{
				var nodeView = m_GraphView.nodes.ToList().OfType<GeometryNodeView>().FirstOrDefault(p => p.node != null && p.node.guid == node.guid);
				m_GraphView.AddToSelection(nodeView);
			}

			var nodesToUpdate = m_NodeViewHashSet;
			nodesToUpdate.Clear();

			foreach(var edge in m_Graph.removedEdges)
			{
				var edgeView = m_GraphView.graphElements.ToList().OfType<UnityEditor.Experimental.GraphView.Edge>().FirstOrDefault(p => p.userData is IEdge && Equals((IEdge)p.userData, edge));
				if(edgeView != null)
				{
					var nodeView = edgeView.input.node as GeometryNodeView;
					if(nodeView != null && nodeView.node != null)
					{
						nodesToUpdate.Add(nodeView);
					}
					edgeView.output.Disconnect(edgeView);
					edgeView.input.Disconnect(edgeView);

					edgeView.output = null;
					edgeView.input = null;

					m_GraphView.RemoveElement(edgeView);
				}
			}

			foreach(var edge in m_Graph.addedEdges)
			{
				var edgeView = AddEdge(edge);
				if (edgeView != null)
					nodesToUpdate.Add((GeometryNodeView)edgeView.input.node);
			}

			foreach (var node in nodesToUpdate)
				node.UpdatePortInputVisiblities();

			UpdateEdgeColors(nodesToUpdate);
		}

		private void AddNode(INode node)
		{
			var nodeView = new GeometryNodeView { userData = node };
			m_GraphView.AddElement(nodeView);
			nodeView.Initialize(node as AbstractGeometryNode, m_PreviewManager, m_EdgeConnectorListener);
			node.RegisterCallback(OnNodeChanged);
			nodeView.MarkDirtyRepaint();

			//if (m_SearchWindowProvider.nodeNeedsRepositioning && m_SearchWindowProvider.targetSlotReference.nodeGuid.Equals(node.guid))
			//{
			//	m_SearchWindowProvider.nodeNeedsRepositioning = false;
			//	foreach (var element in nodeView.inputContainer.Union(nodeView.outputContainer))
			//	{
			//		var port = element as ShaderPort;
			//		if (port == null)
			//			continue;
			//		if (port.slot.slotReference.Equals(m_SearchWindowProvider.targetSlotReference))
			//		{
			//			port.RegisterCallback<GeometryChangedEvent>(RepositionNode);
			//			return;
			//		}
			//	}
			//}
		}

		private static void RepositionNode(GeometryChangedEvent evt)
		{
			var port = evt.target as GeometryPort;
			if (port == null)
				return;
			port.UnregisterCallback<GeometryChangedEvent>(RepositionNode);
			var nodeView = port.node as GeometryNodeView;
			if (nodeView == null)
				return;
			var offset = nodeView.mainContainer.WorldToLocal(port.GetGlobalCenter() + new Vector3(3f, 3f, 0f));
			var position = nodeView.GetPosition();
			position.position -= offset;
			nodeView.SetPosition(position);
			var drawState = nodeView.node.drawState;
			drawState.position = position;
			nodeView.node.drawState = drawState;
			nodeView.MarkDirtyRepaint();
			port.MarkDirtyRepaint();
		}

		private UnityEditor.Experimental.GraphView.Edge AddEdge(IEdge edge)
		{
			var sourceNode = m_Graph.GetNodeFromGuid(edge.outputSlot.nodeGuid);
			if(sourceNode == null)
			{
				Debug.LogWarning("Source node is null");
				return null;
			}
			var sourceSlot = sourceNode.FindOutputSlot<GeometrySlot>(edge.outputSlot.slotId);

			var targetNode = m_Graph.GetNodeFromGuid(edge.inputSlot.nodeGuid);
			if(targetNode == null)
			{
				Debug.LogWarning("Target node is null");
				return null;
			}
			var targetSlot = targetNode.FindInputSlot<GeometrySlot>(edge.inputSlot.slotId);

			var sourceNodeView = m_GraphView.nodes.ToList().OfType<GeometryNodeView>().FirstOrDefault(x => x.node == sourceNode);
			if(sourceNodeView != null)
			{
				var sourceAnchor = sourceNodeView.outputContainer.Children().OfType<GeometryPort>().FirstOrDefault(x => x.slot.Equals(sourceSlot));

				var targetNodeView = m_GraphView.nodes.ToList().OfType<GeometryNodeView>().FirstOrDefault(x => x.node == targetNode);
				var targetAnchor = targetNodeView.inputContainer.Children().OfType<GeometryPort>().FirstOrDefault(x => x.slot.Equals(targetSlot));

				var edgeView = new UnityEditor.Experimental.GraphView.Edge
				{
					userData = edge,
					output = sourceAnchor,
					input = targetAnchor
				};
				edgeView.output.Connect(edgeView);
				edgeView.input.Connect(edgeView);
				m_GraphView.AddElement(edgeView);
				sourceNodeView.RefreshPorts();
				targetNodeView.RefreshPorts();
				sourceNodeView.UpdatePortInputTypes();
				targetNodeView.UpdatePortInputTypes();

				return edgeView;
			}

			return null;
		}

		private Stack<GeometryNodeView> m_NodeStack = new Stack<GeometryNodeView>();

		private void UpdateEdgeColors(HashSet<GeometryNodeView> nodeViews)
		{
			var nodeStack = m_NodeStack;
			nodeStack.Clear();
			foreach (var nodeView in nodeViews)
				nodeStack.Push(nodeView);
			while (nodeStack.Any())
			{
				var nodeView = nodeStack.Pop();
				nodeView.UpdatePortInputTypes();
				foreach(var anchorView in nodeView.outputContainer.Children().OfType<Port>())
				{
					foreach(var edgeView in anchorView.connections.OfType<UnityEditor.Experimental.GraphView.Edge>())
					{
						var targetSlot = edgeView.input.GetSlot();
						if(targetSlot.valueType == SlotValueType.DynamicVector || targetSlot.valueType == SlotValueType.DynamicMatrix || targetSlot.valueType == SlotValueType.Dynamic)
						{
							var connectedNodeView = edgeView.input.node as GeometryNodeView;
							if(connectedNodeView != null && !nodeViews.Contains(connectedNodeView))
							{
								nodeStack.Push(connectedNodeView);
								nodeView.Add(connectedNodeView);
							}
						}
					}
				}
				foreach(var anchorView in nodeView.inputContainer.Children().OfType<Port>())
				{
					var targetSlot = anchorView.GetSlot();
					if (targetSlot.valueType != SlotValueType.DynamicVector)
						continue;
					foreach(var edgeView in anchorView.connections.OfType<UnityEditor.Experimental.GraphView.Edge>())
					{
						var connectedNodeView = edgeView.output.node as GeometryNodeView;
						if(connectedNodeView != null && !nodeViews.Contains(connectedNodeView))
						{
							nodeStack.Push(connectedNodeView);
							nodeViews.Add(connectedNodeView);
						}
					}
				}
			}
		}

		private void HandleEditorViewChanged(GeometryChangedEvent evt)
		{
			//m_BlackboardProvider.blackboard.layout = m_FloatingWindowsLayout.blackboardLayout.GetLayout(m_GraphView.layout);
		}

		private void StoreBlackboardLayoutOnGeometryChanged(GeometryChangedEvent evt)
		{
			UpdateSerializedWindowLayout();
		}

		private void ApplySerializewindowLayouts(GeometryChangedEvent evt)
		{
			UnregisterCallback<GeometryChangedEvent>(ApplySerializewindowLayouts);

			if(m_FloatingWindowsLayout != null)
			{
				// Restore master preview layout
				//m_FloatingWindowsLayout.previewLayout.ApplyPosition(m_MasterPreviewView);
				//m_MasterPreviewView.previewTextureView.style.width = StyleValue<float>.Create(m_FloatingWindowsLayout.masterPreviewSize.x);
				//m_MasterPreviewView.previewTextureView.style.height = StyleValue<float>.Create(m_FloatingWindowsLayout.masterPreviewSize.y);

				//// Restore blackboard layout
				//m_BlackboardProvider.blackboard.layout = m_FloatingWindowsLayout.blackboardLayout.GetLayout(this.layout);

				//previewManager.ResizeMasterPreview(m_FloatingWindowsLayout.masterPreviewSize);
			}
			else
			{
				m_FloatingWindowsLayout = new FloatingWindowsLayout();
			}

			// After the layout is restored from the previous session, start tracking layout changes in the blackboard.
			//m_BlackboardProvider.blackboard.RegisterCallback<GeometryChangedEvent>(StoreBlackboardLayoutOnGeometryChanged);

			// After the layout is restored, track changes in layout and make the blackboard have the same behavior as the preview w.r.t. docking.
			RegisterCallback<GeometryChangedEvent>(HandleEditorViewChanged);
		}

		private void UpdateSerializedWindowLayout()
		{
			if (m_FloatingWindowsLayout == null)
				m_FloatingWindowsLayout = new FloatingWindowsLayout();

			//m_FloatingWindowsLayout.previewLayout.CalculateDockingCornerAndOffset(m_MasterPreviewView.layout, m_GraphView.layout);
			m_FloatingWindowsLayout.previewLayout.ClampToParentWindow();

			//m_FloatingWindowsLayout.blackboardLayout.CalculateDockingCornerAndOffset(m_BlackboardProvider.blackboard.layout, m_GraphView.layout);
			//m_FloatingWindowsLayout.blackboardLayout.ClampToParentWindow();

			//if (m_MasterPreviewView.expanded)
			//{
			//	m_FloatingWindowsLayout.masterPreviewSize = m_MasterPreviewView.previewTextureView.layout.size;
			//}

			string serializedWindowLayout = JsonUtility.ToJson(m_FloatingWindowsLayout);
			EditorUserSettings.SetConfigValue(k_FloatingWindowsLayoutKey, serializedWindowLayout);
		}

		public void Dispose()
		{
			if (m_GraphView != null)
			{
				saveRequested = null;
				convertToSubgraphRequested = null;
				showInProjectRequested = null;
				foreach (var node in m_GraphView.Children().OfType<GeometryNodeView>())
					node.Dispose();
				m_GraphView = null;
			}
			if (previewManager != null)
			{
				//previewManager.Dispose();
				previewManager = null;
			}
			if (m_SearchWindowProvider != null)
			{
				UnityEngine.Object.DestroyImmediate(m_SearchWindowProvider);
				m_SearchWindowProvider = null;
			}
		}
	}
}
