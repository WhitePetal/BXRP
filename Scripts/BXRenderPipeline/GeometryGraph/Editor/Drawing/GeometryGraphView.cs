using BXGraphing;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Graphs;
using UnityEngine.UIElements;
using Node = UnityEditor.Experimental.GraphView.Node;
using Edge = UnityEditor.Experimental.GraphView.Edge;
using UnityEngine;

namespace BXGeometryGraph
{
	public class GeometryGraphView : GraphView
	{
		public string persistenceKey { get; set; }

		public GeometryGraphView()
		{
			this.AddStyleSheetPath("Assets/Scripts/BXRenderPipeline/GeometryGraph/Editor/Resource/Styles/GeometryGraphView.uss");
		}

		protected override bool canCopySelection
		{
			get { return selection.OfType<Node>().Any() || selection.OfType<GroupNode>().Any() || selection.OfType<BlackboardField>().Any(); }
		}

		public GeometryGraphView(AbstructGeometryGraph graph) : this()
		{
			this.graph = graph;
		}

		public AbstructGeometryGraph graph { get; private set; }

		public Action onConvertToSubgraphClick { get; set; }

		public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
		{
			var compatibleAnchors = new List<Port>();
			var startSlot = startPort.GetSlot();
			if (startSlot == null)
				return compatibleAnchors;

			foreach(var candidateAnchor in ports.ToList())
			{
				var candidateSlot = candidateAnchor.GetSlot();
				if (!startSlot.IsCompatibleWith(candidateSlot))
					continue;

				//if (startStage != ShaderStage.Dynamic)
				//{
				//	var candidateStage = candidateSlot.shaderStage;
				//	if (candidateStage == ShaderStage.Dynamic)
				//		candidateStage = NodeUtils.FindEffectiveShaderStage(candidateSlot.owner, !startSlot.isOutputSlot);
				//	if (candidateStage != ShaderStage.Dynamic && candidateStage != startStage)
				//		continue;
				//}

				compatibleAnchors.Add(candidateAnchor);
			}
			return compatibleAnchors;
		}

		public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
		{
			base.BuildContextualMenu(evt);
			if(evt.target is GraphView || evt.target is Node)
			{
				evt.menu.AppendAction("Convert To Sub-graph", ConvertToSubgraph, ConvertToSubgraphStatus);
				evt.menu.AppendAction("Convert To Inline Node", ConvertToInlineNode, ConvertToInlineNodeStatus);
				evt.menu.AppendAction("Convert To Property", ConvertToProperty, ConvertToPropertyStatus);
				if(selection.OfType<GeometryNodeView>().Count() == 1)
				{
					evt.menu.AppendSeparator();
					evt.menu.AppendAction("Open Documentation", SeeDocumentation, SeeDocumentationStatus);
				}
				if(selection.OfType<GeometryNodeView>().Count() == 1 && selection.OfType<GeometryNodeView>().First().node is SubGraphNode)
				{
					evt.menu.AppendSeparator();
					evt.menu.AppendAction("Open Sub Graph", OpenSubGraph, DropdownMenuAction.Status.Normal);
				}
			}
			else if(evt.target is BlackboardField)
			{
				evt.menu.AppendAction("Delete", (e) => DeleteSelectionImplementation("Delete", AskUser.DontAskUser), (e) => canDeleteSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
			}
			if(evt.target is GeometryGraphView)
			{
				evt.menu.AppendAction("Collapse Previews", CollapsePreviews, DropdownMenuAction.Status.Normal);
				evt.menu.AppendAction("Expand Previews", ExpandPreviews, DropdownMenuAction.Status.Normal);
				evt.menu.AppendSeparator();
			}
		}

		private void CollapsePreviews(DropdownMenuAction act)
		{
			graph.owner.RegisterCompleteObjectUndo("Collapse Previews");
			foreach(var node in graph.GetNodes<AbstractGeometryNode>())
			{
				node.previewExpanded = false;
			}
		}

		private void ExpandPreviews(DropdownMenuAction act)
		{
			graph.owner.RegisterCompleteObjectUndo("Expand Previews");
			foreach(var node in graph.GetNodes<AbstractGeometryNode>())
			{
				node.previewExpanded = false;
			}
		}

		private void SeeDocumentation(DropdownMenuAction act)
		{
			var node = selection.OfType<GeometryNodeView>().First().node;
			if (node.documentationURL != null)
				System.Diagnostics.Process.Start(node.documentationURL);
		}

		private void OpenSubGraph(DropdownMenuAction act)
		{
			//SubGraphNode subgraphNode = selection.OfType<GeometryNodeView>().First().node as SubGraphNode;

			//var path = AssetDatabase.GetAssetPath(subgraphNode.subGraphAsset);
			//GeometryGraphImporterEditor.ShowGraphEditWindow(path);
		}

		private DropdownMenuAction.Status SeeDocumentationStatus(DropdownMenuAction act)
		{
			if (selection.OfType<GeometryNodeView>().First().node.documentationURL == null)
				return DropdownMenuAction.Status.Disabled;

			return DropdownMenuAction.Status.Normal;
		}

		private DropdownMenuAction.Status ConvertToPropertyStatus(DropdownMenuAction act)
		{
			if (selection.OfType<GeometryNodeView>().Any(v => v.node != null))
			{
				if (selection.OfType<GeometryNodeView>().Any(v => v.node is IPropertyFromNode))
					return DropdownMenuAction.Status.Normal;
				return DropdownMenuAction.Status.Disabled;
			}
			return DropdownMenuAction.Status.Hidden;
		}

		private void ConvertToProperty(DropdownMenuAction act)
		{
			var selectedNodeViews = selection.OfType<GeometryNodeView>().Select(x => x.node).ToList();
			foreach(var node in selectedNodeViews)
			{
				if (!(node is IPropertyFromNode))
					continue;

				var converter = node as IPropertyFromNode;
				var prop = converter.AsGeometryProperty();
				graph.AddGeometryProperty(prop);

				var propNode = new PropertyNode();
				propNode.drawState = node.drawState;
				graph.AddNode(propNode);
				propNode.propertyGuid = prop.guid;

				var oldSlot = node.FindSlot<GeometrySlot>(converter.outputSlotID);
				var newSlot = propNode.FindSlot<GeometrySlot>(PropertyNode.OutputSlotId);

				foreach (var edge in graph.GetEdges(oldSlot.slotReference))
					graph.Connect(newSlot.slotReference, edge.inputSlot);

				graph.RemoveNode(node);
			}
		}

		private DropdownMenuAction.Status ConvertToInlineNodeStatus(DropdownMenuAction act)
		{
			if(selection.OfType<GeometryNodeView>().Any(v => v.node != null))
			{
				if (selection.OfType<GeometryNodeView>().Any(v => v.node is PropertyNode))
					return DropdownMenuAction.Status.Normal;
				return DropdownMenuAction.Status.Disabled;
			}
			return DropdownMenuAction.Status.Hidden;
		}

		private void ConvertToInlineNode(DropdownMenuAction act)
		{
			var selectedNodeViews = selection.OfType<GeometryNodeView>()
				.Select(x => x.node)
				.OfType<PropertyNode>();

			foreach (var propNode in selectedNodeViews)
				((AbstructGeometryGraph)propNode.owner).ReplacePropertyNodeWithConcreteNode(propNode);
		}

		private DropdownMenuAction.Status ConvertToSubgraphStatus(DropdownMenuAction act)
		{
			if (onConvertToSubgraphClick == null) return DropdownMenuAction.Status.Hidden;
			return selection.OfType<GeometryNodeView>().Any(v => v.node != null) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Hidden;
		}

		private void ConvertToSubgraph(DropdownMenuAction act)
		{
			onConvertToSubgraphClick();
		}


		private string SerializeGraphElementsImplementation(IEnumerable<GraphElement> elements)
		{
			var nodes = elements.OfType<GeometryNodeView>().Select(x => (INode)x.node);
			var edges = elements.OfType<UnityEditor.Experimental.GraphView.Edge>().Select(x => x.userData).OfType<IEdge>();
			var properties = selection.OfType<BlackboardField>().Select(x => x.userData as IGeometryProperty);

			// Collect the property nodes and get the corresponding properties
			var propertyNodeGuids = nodes.OfType<PropertyNode>().Select(x => x.propertyGuid);
			var metaProperties = this.graph.properties.Where(x => propertyNodeGuids.Contains(x.guid));

			var graph = new CopyPasteGraph(this.graph.guid, nodes, edges, properties, metaProperties);
			return JsonUtility.ToJson(graph, true);
		}

		private bool CanPasteSerializedDataImplementation(string serializedData)
		{
			return CopyPasteGraph.FromJson(serializedData) != null;
		}

		private void UnserializedAndPasteImplementation(string operationName, string serializedData)
		{
			graph.owner.RegisterCompleteObjectUndo(operationName);
			var pastedGraph = CopyPasteGraph.FromJson(serializedData);
			this.InsertCopyPasteGraph(pastedGraph);
		}

		private void DeleteSelectionImplementation(string operationName, AskUser askUser)
		{
			foreach(var selectable in selection)
			{
				var field = selectable as BlackboardField;
				if(field != null && field.userData != null)
				{
					if (EditorUtility.DisplayDialog("Sub Graph Will Change", "If you remove a property and save the sub graph, you might change other graphs that are using this sub graph.\n\nDo you want to continue?", "Yes", "No"))
						break;
					return;
				}
			}

			graph.owner.RegisterCompleteObjectUndo(operationName);
			graph.RemoveElements(selection.OfType<GeometryNodeView>().Where(v => !(v.node is SubGraphOutputNode)).Select(x => (INode)x.node), selection.OfType<UnityEditor.Experimental.GraphView.Edge>().Select(x => x.userData).OfType<IEdge>());
			
			foreach(var selectable in selection)
			{
				var field = selectable as BlackboardField;
				if(field != null && field.userData != null)
				{
					var property = (IGeometryProperty)field.userData;
					graph.RemoveGeometryProperty(property.guid);
				}
			}

			selection.Clear();
		}


		private static void OnDragUpdateEvent(DragUpdatedEvent e)
		{
			var selection = DragAndDrop.GetGenericData("DragSelection") as List<ISelectable>;

			if(selection != null && (selection.OfType<BlackboardField>().Any()))
			{
				DragAndDrop.visualMode = e.ctrlKey ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Move;
			}
		}

		private void OnDragPerformEvent(DragPerformEvent e)
		{
			var selection = DragAndDrop.GetGenericData("DragSelection") as List<ISelectable>;
			if (selection == null)
				return;

			IEnumerable<BlackboardField> fields = selection.OfType<BlackboardField>();
			if (!fields.Any())
				return;

			Vector2 localPos = (e.currentTarget as VisualElement).ChangeCoordinatesTo(contentViewContainer, e.localMousePosition);

			foreach(BlackboardField field in fields)
			{
				IGeometryProperty property = field.userData as IGeometryProperty;
				if (property == null)
					continue;

				var node = new PropertyNode();

				var drawState = node.drawState;
				var position = drawState.position;
				position.x = localPos.x;
				position.y = localPos.y;
				drawState.position = position;
				node.drawState = drawState;

				graph.owner.RegisterCompleteObjectUndo("Added Property");
				graph.AddNode(node);
				node.propertyGuid = property.guid;
			}
		}
	}

	public static class GraphViewExtensions
	{
		internal static void InsertCopyPasteGraph(this GeometryGraphView graphView, CopyPasteGraph copyGraph)
		{
			if (copyGraph == null)
				return;

			// Make new properties from the copied graph
			foreach(IGeometryProperty property in copyGraph.properties)
			{
				string propertyName = graphView.graph.SanitizePropertyName(property.displayName);
				IGeometryProperty copiedProperty = property.Copy();
				copiedProperty.displayName = propertyName;
				graphView.graph.AddGeometryProperty(copiedProperty);

				// Update the property nodes that depends on the copied node
				var dependentPropertyNodes = copyGraph.GetNodes<PropertyNode>().Where(x => x.propertyGuid == property.guid);
				foreach(var node in dependentPropertyNodes)
				{
					node.owner = graphView.graph;
					node.propertyGuid = copiedProperty.guid;
				}
			}

			using(var remappedNodesDisposable = ListPool<INode>.GetDisposable())
			{
				using(var remappedEdgesDisposable = ListPool<IEdge>.GetDisposable())
				{
					var remappedNodes = remappedNodesDisposable.value;
					var remappedEdges = remappedEdgesDisposable.value;

					graphView.graph.PasteGraph(copyGraph, remappedNodes, remappedEdges);

					if(graphView.graph.guid != copyGraph.sourceGraphGuid)
					{
						// Compute the mean of the copied nodes.
						Vector2 centroid = Vector2.zero;
						var count = 1;
						foreach(var node in remappedNodes)
						{
							var position = node.drawState.position.position;
							centroid = centroid + (position - centroid) / count;
							++count;
						}

						// Get the center of the current view
						var viewCenter = graphView.contentViewContainer.WorldToLocal(graphView.layout.center);

						foreach(var node in remappedNodes)
						{
							var darwState = node.drawState;
							var positionRect = darwState.position;
							var positon = positionRect.position;
							positon += viewCenter - centroid;
							positionRect.position = positon;
							darwState.position = positionRect;
							node.drawState = darwState;
						}
					}

					// Add new elements to selection
					graphView.ClearSelection();
					graphView.graphElements.ForEach(element =>
					{
						var edge = element as UnityEditor.Experimental.GraphView.Edge;
						if (edge != null && remappedEdges.Contains(edge.userData as IEdge))
							graphView.AddToSelection(edge);

						var nodeView = element as GeometryNodeView;
						if (nodeView != null && remappedNodes.Contains(nodeView.node))
							graphView.AddToSelection(nodeView);
					});
				}
			}
		}
	}
}
