
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

using System;

namespace BXGeometryGraph
{
	sealed class GeometryNodeView : Node, IGeometryNodeView, IInspectable
	{
		private PreviewRenderData m_PreviewRenderData;
		private Image m_PreviewImage;
		// Remove this after updated to the correct API call has landed in trunk. ------------
		private VisualElement m_TitleContainer;

		private VisualElement m_PreviewContainer;
		private VisualElement m_PreviewFiller;
		private VisualElement m_ControlItems;
		private VisualElement m_ControlsDivider;
		private VisualElement m_DropdownItems;
		private VisualElement m_DropdownsDivider;
		private Action m_UnregisterAll;

		private IEdgeConnectorListener m_ConnectorListener;

		private GeometryGraphView m_GraphView;

		public string inspectorTitle => $"{node.name} (Node)";

		public void Initialize(AbstractGeometryNode inNode, PreviewManager previewManager, IEdgeConnectorListener connectorListener, GeometryGraphView graphView)
		{
			this.AddStyleSheetPath("Assets/Scripts/BXRenderPipeline/GeometryGraph/Editor/Resources/Styles/GeometryNodeView.uss");
			this.AddStyleSheetPath("Assets/Scripts/BXRenderPipeline/GeometryGraph/Editor/Resources/Styles/ColorMode.uss");
			AddToClassList("GeometryNode");

			if (inNode == null)
				return;

			var contents = this.Q("contents");

			m_GraphView = graphView;
			mainContainer.style.overflow = StyleKeyword.None; // Override explicit style set in base class
			m_ConnectorListener = connectorListener;
			node = inNode;
			UpdateTitle();

			// Add controls container
			var controlsContainer = new VisualElement { name = "controls" };
			{
				m_ControlsDivider = new VisualElement { name = "divider" };
				m_ControlsDivider.AddToClassList("horizontal");
				controlsContainer.Add(m_ControlsDivider);
				m_ControlItems = new VisualElement { name = "items" };
				controlsContainer.Add(m_ControlItems);

				// Instantiate control views from node
				foreach (var propertyInfo in node.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
					foreach (IControlAttribute attribute in propertyInfo.GetCustomAttributes(typeof(IControlAttribute), false))
						m_ControlItems.Add(attribute.InstantiateControl(node, propertyInfo));
			}
			if (m_ControlItems.childCount > 0)
				contents.Add(controlsContainer);

			if (node.hasPreview)
			{
				// Add actual preview which floats on top of the node
				m_PreviewContainer = new VisualElement
				{
					name = "previewContainer",
					//clippingOptions = ClippingOptions.ClipAndCacheContents,
					pickingMode = PickingMode.Ignore
				};
				m_PreviewImage = new Image
				{
					name = "preview",
					pickingMode = PickingMode.Ignore,
					image = Texture2D.whiteTexture
				};
				{
					// Add preview collapse button on top of preview
					var collapsePreviewButton = new VisualElement { name = "collapse" };
					collapsePreviewButton.Add(new VisualElement { name = "icon" });
					collapsePreviewButton.AddManipulator(new Clickable(() =>
					{
						node.owner.owner.RegisterCompleteObjectUndo("Collapse Preview");
						UpdatePreviewExpandedState(false);
					}));
					m_PreviewImage.Add(collapsePreviewButton);
				}
				m_PreviewContainer.Add(m_PreviewImage);

				// Hook up preview image to preview manager
				m_PreviewRenderData = previewManager.GetPreviewRenderData(inNode);
				m_PreviewRenderData.onPreviewChanged += UpdatePreviewTexture;
				UpdatePreviewTexture();

				// Add fake preview which pads out the node to provide space for the floating preview
				m_PreviewFiller = new VisualElement { name = "previewFiller" };
				m_PreviewFiller.AddToClassList("expanded");
				{
					var previewDivider = new VisualElement { name = "divider" };
					previewDivider.AddToClassList("horizontal");
					m_PreviewFiller.Add(previewDivider);

					var expandPreviewButton = new VisualElement { name = "expand" };
					expandPreviewButton.Add(new VisualElement { name = "icon" });
					expandPreviewButton.AddManipulator(new Clickable(() =>
					{
						node.owner.owner.RegisterCompleteObjectUndo("Expand Preview");
						UpdatePreviewExpandedState(true);
					}));
					m_PreviewFiller.Add(expandPreviewButton);
				}
				contents.Add(m_PreviewFiller);

				UpdatePreviewExpandedState(node.previewExpanded);
			}

			base.expanded = node.drawState.expanded;
			AddSlots(node.GetSlots<GeometrySlot>());

            switch (node)
            {
				case SubGraphNode:
					RegisterCallback<MouseDownEvent>(OnSubGraphDoubleClick);
					m_UnregisterAll += () => { UnregisterCallback<MouseDownEvent>(OnSubGraphDoubleClick); };
					break;
			}

			m_TitleContainer = this.Q("title");

			if (node is BlockNode blockData)
			{
				AddToClassList("blockData");
				m_TitleContainer.RemoveFromHierarchy();
			}
			else
			{
				SetPosition(new Rect(node.drawState.position.x, node.drawState.position.y, 0, 0));
			}

			// Update active state
			SetActive(node.isActive);

			// Register OnMouseHover callbacks for node highlighting
			RegisterCallback<MouseEnterEvent>(OnMouseHover);
			m_UnregisterAll += () => { UnregisterCallback<MouseEnterEvent>(OnMouseHover); };

			RegisterCallback<MouseLeaveEvent>(OnMouseHover);
			m_UnregisterAll += () => { UnregisterCallback<MouseLeaveEvent>(OnMouseHover); };

			GeometryGraphPreferences.onAllowDeprecatedChanged += UpdateTitle;
		}

		public bool FindPort(SlotReference slotRef, out GeometryPort port)
		{
			port = inputContainer.Query<GeometryPort>().ToList()
				.Concat(outputContainer.Query<GeometryPort>().ToList())
				.First(p => p.slot.slotReference.Equals(slotRef));

			return port != null;
		}

		public void AttachMessage(string errString, GeometryCompilerMessageSeverity severity)
		{
			ClearMessage();
			IconBadge badge;
			if (severity == GeometryCompilerMessageSeverity.Error)
			{
				badge = IconBadge.CreateError(errString);
			}
			else
			{
				badge = IconBadge.CreateComment(errString);
			}

			Add(badge);

			if (node is BlockNode)
			{
				FindPort(node.GetSlotReference(0), out var port);
				badge.AttachTo(port.parent, SpriteAlignment.RightCenter);
			}
			else
			{
				badge.AttachTo(m_TitleContainer, SpriteAlignment.RightCenter);
			}
		}

		public void SetActive(bool state)
		{
			// Setup
			var disabledString = "disabled";
			var portDisabledString = "inactive";


			if (!state)
			{
				// Add elements to disabled class list
				AddToClassList(disabledString);

				var inputPorts = inputContainer.Query<GeometryPort>().ToList();
				foreach (var port in inputPorts)
				{
					port.AddToClassList(portDisabledString);
				}
				var outputPorts = outputContainer.Query<GeometryPort>().ToList();
				foreach (var port in outputPorts)
				{
					port.AddToClassList(portDisabledString);
				}
			}
			else
			{
				// Remove elements from disabled class list
				RemoveFromClassList(disabledString);

				var inputPorts = inputContainer.Query<GeometryPort>().ToList();
				foreach (var port in inputPorts)
				{
					port.RemoveFromClassList(portDisabledString);
				}
				var outputPorts = outputContainer.Query<GeometryPort>().ToList();
				foreach (var port in outputPorts)
				{
					port.RemoveFromClassList(portDisabledString);
				}
			}
		}

		public void ClearMessage()
		{
			var badge = this.Q<IconBadge>();
			badge?.Detach();
			badge?.RemoveFromHierarchy();
		}

		public void UpdateDropdownEntries()
		{
			if (node is SubGraphNode subGraphNode && subGraphNode.asset != null)
			{
				m_DropdownItems.Clear();
				var dropdowns = subGraphNode.asset.dropdowns;
				foreach (var dropdown in dropdowns)
				{
					if (dropdown.isExposed)
					{
						var name = subGraphNode.GetDropdownEntryName(dropdown.referenceName);
						if (!dropdown.ContainsEntry(name))
						{
							name = dropdown.entryName;
							subGraphNode.SetDropdownEntryName(dropdown.referenceName, name);
						}

						var field = new PopupField<string>(dropdown.entries.Select(x => x.displayName).ToList(), name);

						// Create anonymous lambda
						EventCallback<ChangeEvent<string>> eventCallback = (evt) =>
						{
							subGraphNode.owner.owner.RegisterCompleteObjectUndo("Change Dropdown Value");
							subGraphNode.SetDropdownEntryName(dropdown.referenceName, field.value);
							subGraphNode.Dirty(ModificationScope.Topological);
						};

						field.RegisterValueChangedCallback(eventCallback);

						// Setup so we can unregister this callback later
						m_UnregisterAll += () => field.UnregisterValueChangedCallback(eventCallback);

						m_DropdownItems.Add(new PropertyRow(new Label(dropdown.displayName)), (row) =>
						{
							row.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyRow"));
							row.Add(field);
						});
					}
				}
			}
		}

		public VisualElement colorElement
		{
			get { return this; }
		}

		static readonly StyleColor noColor = new StyleColor(StyleKeyword.Null);
		public void SetColor(Color color)
		{
			m_TitleContainer.style.borderBottomColor = color;
		}

		public void ResetColor()
		{
			m_TitleContainer.style.borderBottomColor = noColor;
		}

		public Color GetColor()
		{
			return m_TitleContainer.resolvedStyle.borderBottomColor;
		}

		void OnSubGraphDoubleClick(MouseDownEvent evt)
		{
			if (evt.clickCount == 2 && evt.button == 0)
			{
				SubGraphNode subgraphNode = node as SubGraphNode;

				var path = AssetDatabase.GUIDToAssetPath(subgraphNode.subGraphGuid);
				GeometryGraphImporterEditor.ShowGraphEditWindow(path);
			}
		}

		public Node gvNode => this;

		[Inspectable("Node", null)]
		public AbstractGeometryNode node { get; private set; }

		public override bool expanded
		{
			get => base.expanded;
			set
			{
				if (base.expanded == value)
					return;

				base.expanded = value;

				if (node.drawState.expanded != value)
				{
					var ds = node.drawState;
					ds.expanded = value;
					node.drawState = ds;
				}

				foreach (var inputPort in inputContainer.Query<GeometryPort>().ToList())
				{
					inputPort.parent.style.visibility = inputPort.style.visibility;
				}

				RefreshExpandedState(); // Necessary b/c we can't override enough Node.cs functions to update only what's needed
			}
		}

		public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
		{
			if (evt.target is Node)
			{
				var canViewShader = node.hasPreview || node is SubGraphOutputNode;
				evt.menu.AppendAction("Copy Shader", CopyToClipboard,
					_ => canViewShader ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Hidden,
					GenerationMode.ForReals);
				evt.menu.AppendAction("Show Generated Code", ShowGeneratedCode,
					_ => canViewShader ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Hidden,
					GenerationMode.ForReals);

				if (Unsupported.IsDeveloperMode())
				{
					evt.menu.AppendAction("Show Preview Code", ShowGeneratedCode,
						_ => canViewShader ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Hidden,
						GenerationMode.Preview);
				}
			}

			base.BuildContextualMenu(evt);
		}

		void CopyToClipboard(DropdownMenuAction action)
		{
			GUIUtility.systemCopyBuffer = ConvertToGeometry((GenerationMode)action.userData);
		}

		public string SanitizeName(string name)
		{
			return new string(name.Where(c => !Char.IsWhiteSpace(c)).ToArray());
		}

		public void ShowGeneratedCode(DropdownMenuAction action)
		{
			string name = GetFirstAncestorOfType<GraphEditorView>().assetName;
			var mode = (GenerationMode)action.userData;

			string path = String.Format("Temp/GeneratedFromGraph-{0}-{1}-{2}{3}.shader", SanitizeName(name),
				SanitizeName(node.name), node.objectId, mode == GenerationMode.Preview ? "-Preview" : "");
			if (GraphUtil.WriteToFile(path, ConvertToGeometry(mode)))
				GraphUtil.OpenFile(path);
		}

		string ConvertToGeometry(GenerationMode mode)
		{
			//var generator = new Generator(node.owner, node, mode, node.name);
			//return generator.generatedShader;
			return "";
		}

		void SetNodesAsDirty()
		{
			var editorView = GetFirstAncestorOfType<GraphEditorView>();
			var nodeList = m_GraphView.Query<GeometryNodeView>().ToList();
			editorView.colorManager.SetNodesDirty(nodeList);
		}

		void UpdateNodeViews()
		{
			var editorView = GetFirstAncestorOfType<GraphEditorView>();
			var nodeList = m_GraphView.Query<GeometryNodeView>().ToList();
			editorView.colorManager.UpdateNodeViews(nodeList);
		}

		public object GetObjectToInspect()
		{
			return node;
		}

		public void SupplyDataToPropertyDrawer(IPropertyDrawer propertyDrawer, Action inspectorUpdateDelegate)
		{
			if (propertyDrawer is IGetNodePropertyDrawerPropertyData nodePropertyDrawer)
			{
				nodePropertyDrawer.GetPropertyData(SetNodesAsDirty, UpdateNodeViews);
			}
		}

		private void SetSelfSelected()
		{
			m_GraphView.ClearSelection();
			m_GraphView.AddToSelection(this);
		}

		protected override void ToggleCollapse()
		{
			node.owner.owner.RegisterCompleteObjectUndo(!expanded ? "Expand Nodes" : "Collapse Nodes");
			expanded = !expanded;

			// If selected, expand/collapse the other applicable nodes that are also selected
			if (selected)
			{
				m_GraphView.SetNodeExpandedForSelectedNodes(expanded, false);
			}
		}

		void SetPreviewExpandedStateOnSelection(bool state)
		{
			// If selected, expand/collapse the other applicable nodes that are also selected
			if (selected)
			{
				m_GraphView.SetPreviewExpandedForSelectedNodes(state);
			}
			else
			{
				node.owner.owner.RegisterCompleteObjectUndo(state ? "Expand Previews" : "Collapse Previews");
				node.previewExpanded = state;
			}
		}

		public bool CanToggleNodeExpanded()
		{
			return !(node is BlockNode) && m_CollapseButton.enabledInHierarchy;
		}

		void UpdatePreviewExpandedState(bool expanded)
		{
			node.previewExpanded = expanded;
			if (m_PreviewFiller == null)
				return;
			if (expanded)
			{
				if (m_PreviewContainer.parent != this)
				{
					Add(m_PreviewContainer);
					m_PreviewContainer.PlaceBehind(this.Q("selection-border"));
				}
				m_PreviewFiller.AddToClassList("expanded");
				m_PreviewFiller.RemoveFromClassList("collapsed");
			}
			else
			{
				if (m_PreviewContainer.parent == m_PreviewFiller)
				{
					m_PreviewContainer.RemoveFromHierarchy();
				}
				m_PreviewFiller.RemoveFromClassList("expanded");
				m_PreviewFiller.AddToClassList("collapsed");
			}
			UpdatePreviewTexture();
		}

		void UpdateTitle()
		{
			if (node is SubGraphNode subGraphNode && subGraphNode.asset != null)
				title = subGraphNode.asset.name;
			else
			{
				if (node.ggVersion < node.latestVersion)
				{
					if (node is IHasCustomDeprecationMessage customDeprecationMessage)
					{
						title = customDeprecationMessage.GetCustomDeprecationLabel();
					}
					else
					{
						title = node.name + $" (Legacy v{node.ggVersion})";
					}
				}
				else
				{
					title = node.name;
				}
			}
		}

		void UpdateGeometryPortsForSlots(bool inputSlots, List<GeometrySlot> allSlots, GeometryPort[] slotGeometryPorts)
		{
			VisualElement portContainer = inputSlots ? inputContainer : outputContainer;
			var existingPorts = portContainer.Query<GeometryPort>().ToList();
			foreach (GeometryPort geometryPort in existingPorts)
			{
				var currentSlotId = geometryPort.slot.id;
				int newSlotIndex = allSlots.FindIndex(s => s.id == currentSlotId);
				if (newSlotIndex < 0)
				{
					// slot doesn't exist anymore, remove it
					if (inputSlots)
						portContainer.Remove(geometryPort.parent);    // remove parent (includes the InputView)
					else
						portContainer.Remove(geometryPort);
				}
				else
				{
					var newSlot = allSlots[newSlotIndex];
					slotGeometryPorts[newSlotIndex] = geometryPort;

					// these should probably be in an UpdateShaderPort(geometryPort, newSlot) function
					geometryPort.slot = newSlot;
					geometryPort.portName = newSlot.displayName;

					if (inputSlots) // input slots also have to update the InputView
						UpdatePortInputView(geometryPort);
				}
			}
		}

		public void OnModified(ModificationScope scope)
		{
			UpdateTitle();
			SetActive(node.isActive);
			if (node.hasPreview)
				UpdatePreviewExpandedState(node.previewExpanded);

			base.expanded = node.drawState.expanded;

			switch (scope)
			{
				// Update slots to match node modification
				case ModificationScope.Topological:
					{
						var slots = node.GetSlots<GeometrySlot>().ToList();
						// going to record the corresponding ShaderPort to each slot, so we can order them later
						GeometryPort[] slotGeometryPorts = new GeometryPort[slots.Count];

						// update existing input and output ports
						UpdateGeometryPortsForSlots(true, slots, slotGeometryPorts);
						UpdateGeometryPortsForSlots(false, slots, slotGeometryPorts);

						// check if there are any new slots that must create new ports
						for (int i = 0; i < slots.Count; i++)
						{
							if (slotGeometryPorts[i] == null)
								slotGeometryPorts[i] = AddGeometryPortForSlot(slots[i]);
						}

						// make sure they are in the right order
						// by bringing each port to front in declaration order
						// note that this sorts input and output containers at the same time
						foreach (var geometryPort in slotGeometryPorts)
						{
							if (geometryPort != null)
							{
								if (geometryPort.slot.isInputSlot)
									geometryPort.parent.BringToFront();
								else
									geometryPort.BringToFront();
							}
						}

						break;
					}
			}

			RefreshExpandedState(); // Necessary b/c we can't override enough Node.cs functions to update only what's needed

			foreach (var listener in m_ControlItems.Children().OfType<INodeModificationListener>())
			{
				if (listener != null)
					listener.OnNodeModified(scope);
			}
		}

		GeometryPort AddGeometryPortForSlot(GeometrySlot slot)
		{
			if (slot.hidden)
				return null;

			GeometryPort port = GeometryPort.Create(slot, m_ConnectorListener);
			if (slot.isOutputSlot)
			{
				outputContainer.Add(port);
			}
			else
			{
				var portContainer = new VisualElement();
				portContainer.style.flexDirection = FlexDirection.Row;
				var portInputView = new PortInputView(slot) { style = { position = Position.Absolute } };
				portContainer.Add(portInputView);
				portContainer.Add(port);
				inputContainer.Add(portContainer);

				// Update active state
				Debug.Log("AddGeometryPortForSlot node.isActive: " + node.isActive);
				if (node.isActive)
				{
					portInputView.RemoveFromClassList("disabled");
				}
				else
				{
					portInputView.AddToClassList("disabled");
				}
			}
			port.OnDisconnect = OnEdgeDisconnected;

			return port;
		}

		void AddSlots(IEnumerable<GeometrySlot> slots)
		{
			foreach (var slot in slots)
				AddGeometryPortForSlot(slot);
			// Make sure the visuals are properly updated to reflect port list
			RefreshPorts();
		}

		void OnEdgeDisconnected(Port obj)
		{
			RefreshExpandedState();
		}

		static bool GetPortInputView(GeometryPort port, out PortInputView view)
		{
			view = port.parent.Q<PortInputView>();
			return view != null;
		}

		public void UpdatePortInputTypes()
		{
			var portList = inputContainer.Query<GeometryPort>().ToList();
			portList.AddRange(outputContainer.Query<GeometryPort>().ToList());
			foreach (var anchor in portList)
			{
				var slot = anchor.slot;
				anchor.portName = slot.displayName;
				anchor.visualClass = slot.concreteValueType.ToClassName();

				if (GetPortInputView(anchor, out var portInputView))
				{
					portInputView.UpdateSlotType();
					UpdatePortInputVisibility(portInputView, anchor);
				}
			}

			foreach (var control in m_ControlItems.Children())
			{
				if (control is INodeModificationListener listener)
					listener.OnNodeModified(ModificationScope.Graph);
			}
		}

		void UpdatePortInputView(GeometryPort port)
		{
			if (GetPortInputView(port, out var portInputView))
			{
				portInputView.UpdateSlot(port.slot);
				UpdatePortInputVisibility(portInputView, port);
			}
		}

		void UpdatePortInputVisibility(PortInputView portInputView, GeometryPort port)
		{
			SetElementVisible(portInputView, !port.slot.isConnected);
			port.parent.style.visibility = port.style.visibility;
			portInputView.MarkDirtyRepaint();
		}

		void SetElementVisible(VisualElement element, bool isVisible)
		{
			const string k_HiddenClassList = "hidden";

			if (isVisible)
			{
				// Restore default value for visibility by setting it to StyleKeyword.Null.
				// Setting it to Visibility.Visible would make it visible even if parent is hidden.
				element.style.visibility = StyleKeyword.Null;
				element.RemoveFromClassList(k_HiddenClassList);
			}
			else
			{
				element.style.visibility = Visibility.Hidden;
				element.AddToClassList(k_HiddenClassList);
			}
		}

		GGBlackboardRow GetAssociatedBlackboardRow()
		{
			var graphEditorView = GetFirstAncestorOfType<GraphEditorView>();
			if (graphEditorView == null)
				return null;

			var blackboardController = graphEditorView.blackboardController;
			if (blackboardController == null)
				return null;

			if (node is KeywordNode keywordNode)
			{
				return blackboardController.GetBlackboardRow(keywordNode.keyword);
			}

			if (node is DropdownNode dropdownNode)
			{
				return blackboardController.GetBlackboardRow(dropdownNode.dropdown);
			}
			return null;
		}

		void OnMouseHover(EventBase evt)
		{
			// Keyword/Dropdown nodes should be highlighted when Blackboard entry is hovered
			// TODO: Move to new NodeView type when keyword node has unique style
			var blackboardRow = GetAssociatedBlackboardRow();
			if (blackboardRow != null)
			{
				if (evt.eventTypeId == MouseEnterEvent.TypeId())
				{
					blackboardRow.AddToClassList("hovered");
				}
				else
				{
					blackboardRow.RemoveFromClassList("hovered");
				}
			}
		}

		void UpdatePreviewTexture()
		{
			if (m_PreviewRenderData.texture == null || !node.previewExpanded)
			{
				m_PreviewImage.visible = false;
				m_PreviewImage.image = Texture2D.blackTexture;
			}
			else
			{
				m_PreviewImage.visible = true;
				m_PreviewImage.AddToClassList("visible");
				m_PreviewImage.RemoveFromClassList("hidden");
				if (m_PreviewImage.image != m_PreviewRenderData.texture)
					m_PreviewImage.image = m_PreviewRenderData.texture;
				else
					m_PreviewImage.MarkDirtyRepaint();

				if (m_PreviewRenderData.shaderData.isOutOfDate)
					m_PreviewImage.tintColor = new Color(1.0f, 1.0f, 1.0f, 0.3f);
				else
					m_PreviewImage.tintColor = Color.white;
			}
		}

		public void Dispose()
		{
			ClearMessage();

			foreach (var portInputView in inputContainer.Query<PortInputView>().ToList())
				portInputView.Dispose();

			foreach (var geometryPort in outputContainer.Query<GeometryPort>().ToList())
				geometryPort.Dispose();

			var propRow = GetAssociatedBlackboardRow();
			// If this node view is deleted, remove highlighting from associated blackboard row
			if (propRow != null)
			{
				propRow.RemoveFromClassList("hovered");
			}

			styleSheets.Clear();
			inputContainer?.Clear();
			outputContainer?.Clear();
			m_DropdownsDivider?.Clear();
			m_ControlsDivider?.Clear();
			m_PreviewContainer?.Clear();
			m_ControlItems?.Clear();

			m_ConnectorListener = null;
			m_GraphView = null;
			m_DropdownItems = null;
			m_ControlItems = null;
			m_ControlsDivider = null;
			m_PreviewContainer = null;
			m_PreviewFiller = null;
			m_PreviewImage = null;
			m_DropdownsDivider = null;
			m_TitleContainer = null;
			node = null;
			userData = null;

			// Unregister callback
			m_UnregisterAll?.Invoke();
			m_UnregisterAll = null;

			if (m_PreviewRenderData != null)
			{
				m_PreviewRenderData.onPreviewChanged -= UpdatePreviewTexture;
				m_PreviewRenderData = null;
			}
			GeometryGraphPreferences.onAllowDeprecatedChanged -= UpdateTitle;

			Clear();
		}
    }
}
