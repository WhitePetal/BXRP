using BXGraphing;
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
using UnityEditor.UIElements;
using System;

namespace BXGeometryGraph
{
	public sealed class GeometryNodeView : Node
	{
		private PreviewRenderData m_PreviewRenderData;
		private Image m_PreviewImage;
		private VisualElement m_PreviewContainer;
		private VisualElement m_ControlItems;
		private VisualElement m_PreviewFilter;
		private VisualElement m_ControlsDivider;
		private IEdgeConnectorListener m_ConnectorListerner;
		private VisualElement m_PortInputContainer;
		private VisualElement m_SettingsContainer;
		private bool m_ShowSettings = false;
		private VisualElement m_SettingsButton;
		private VisualElement m_Settings;
		private VisualElement m_NodeSettingsView;

		public void Initialize(AbstractGeometryNode inNode, PreviewManager previewManager, IEdgeConnectorListener connectorListener)
		{
			this.AddStyleSheetPath("Assets/Scripts/BXRenderPipeline/GeometryGraph/Editor/Resource/Styles/GeometryNodeView.uss");
			AddToClassList("GeometryNode");

			if (inNode == null)
				return;

			var contents = this.Q("contents");

			m_ConnectorListerner = connectorListener;
			node = inNode;
			persistenceKey = node.guid.ToString();
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
				m_PreviewRenderData = previewManager.GetPreview(inNode);
				m_PreviewRenderData.onPreviewChanged += UpdatePreviewTexture;
				UpdatePreviewTexture();

				// Add fake preview which pads out the node to provide space for the floating preview
				m_PreviewFilter = new VisualElement { name = "previewFiller" };
				m_PreviewFilter.AddToClassList("expanded");
				{
					var previewDivider = new VisualElement { name = "divider" };
					previewDivider.AddToClassList("horizontal");
					m_PreviewFilter.Add(previewDivider);

					var expandPreviewButton = new VisualElement { name = "expand" };
					expandPreviewButton.Add(new VisualElement { name = "icon" });
					expandPreviewButton.AddManipulator(new Clickable(() =>
					{
						node.owner.owner.RegisterCompleteObjectUndo("Expand Preview");
						UpdatePreviewExpandedState(true);
					}));
					m_PreviewFilter.Add(expandPreviewButton);
				}
				contents.Add(m_PreviewFilter);

				UpdatePreviewExpandedState(node.previewExpanded);
			}

			// Add port input container, which acts as a pixel cache for all port inputs
			m_PortInputContainer = new VisualElement
			{
				name = "portInputContainer",
				//clippingOptions = ClippingOptions.ClipAndCacheContents,
				pickingMode = PickingMode.Ignore
			};
			Add(m_PortInputContainer);

			AddSlots(node.GetSlots<GeometrySlot>());
			UpdatePortInputs();
			base.expanded = node.drawState.expanded;
			RefreshExpandedState();//This should not be needed. GraphView needs to improve the extension api here
			UpdatePortInputVisiblities();

			SetPosition(new Rect(node.drawState.position.x, node.drawState.position.y, 0, 0));

			if(node is SubGraphNode)
			{
				RegisterCallback<MouseDownEvent>(OnSubGraphDoubleClick);
			}

			var masterNode = node as IMasterNode;
			if(masterNode != null)
			{
				if (!masterNode.IsPipelineCompatible(RenderPipelineManager.currentPipeline))
				{
					IconBadge wrongPipeline = IconBadge.CreateError("The current render pipeline is not compatible with this node preview.");
					Add(wrongPipeline);
					VisualElement title = this.Q("title");
					wrongPipeline.AttachTo(title, SpriteAlignment.LeftCenter);
				}
			}

			m_PortInputContainer.SendToBack();

			// Remove this after updated to the correct API call has landed in trunk. ------------
			VisualElement m_TitleContainer;
			VisualElement m_ButtonContainer;
			m_TitleContainer = this.Q("title");
			// -----------------------------------------------------------------------------------

			var settings = node as IHasSettings;
			if(settings != null)
			{
				m_NodeSettingsView = new NodeSettingsView();
				m_NodeSettingsView.visible = false;

				Add(m_NodeSettingsView);

				m_SettingsButton = new VisualElement { name = "settings-button" };
				m_SettingsButton.Add(new VisualElement { name = "icon" });

				m_Settings = settings.CreateSettingsElement();

				m_SettingsButton.AddManipulator(new Clickable(() =>
				{
					UpdateSettingsExpandedState();
				}));

				// Remove this after updated to the correct API call has landed in trunk. ------------
				m_ButtonContainer = new VisualElement { name = "button-container" };
				m_ButtonContainer.style.flexDirection = FlexDirection.Row;
				m_ButtonContainer.Add(m_SettingsButton);
				m_ButtonContainer.Add(m_CollapseButton);
				m_TitleContainer.Add(m_ButtonContainer);
				// -----------------------------------------------------------------------------------
				//titleButtonContainer.Add(m_SettingsButton);
				//titleButtonContainer.Add(m_CollapseButton);

				RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
			}
		}

		private void OnGeometryChanged(GeometryChangedEvent evt)
		{
			// style.positionTop and style.positionLeft are in relation to the parent,
			// so we translate the layout of the settings button to be in the coordinate
			// space of the settings view's parent.

			var settingsButtonLayout = m_SettingsButton.ChangeCoordinatesTo(m_NodeSettingsView.parent, m_SettingsButton.layout);
			m_NodeSettingsView.style.top = settingsButtonLayout.yMax - 18f;
			m_NodeSettingsView.style.left = settingsButtonLayout.xMin - 16f;
		}

		private void OnSubGraphDoubleClick(MouseDownEvent evt)
		{
			if(evt.clickCount == 2 && evt.button == 0)
			{
				SubGraphNode subgraphNode = node as SubGraphNode;

				//var path = AssetDatabase.GetAssetPath(subgraphNode.subGraphAsset);
				//GeometryGraphImporterEditor.ShowGraphEditorWindow(path);
			}
		}

		public AbstractGeometryNode node { get; private set; }

		public override bool expanded 
		{	get { return base.expanded; }
			set
			{
				if (base.expanded != value)
					base.expanded = value;

				if(node.drawState.expanded != value)
				{
					var ds = node.drawState;
					ds.expanded = value;
					node.drawState = ds;
				}

				RefreshExpandedState();//This should not be needed. GraphView needs to improve the extension api here
				UpdatePortInputVisiblities();
			}
		}

		public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
		{
			if (evt.target is Node)
				evt.menu.AppendAction("Copy Geometry", ConvertToGeometry, node.hasPreview ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Hidden);
			base.BuildContextualMenu(evt);
		}

		private void ConvertToGeometry(DropdownMenuAction act)
		{
			List<PropertyCollector.TextureInfo> textureInfo;
			var masterNode = node as IMasterNode;
			if(masterNode != null)
			{
				var geometry = masterNode.GetGeometry(GenerationMode.ForReals, node.name, out textureInfo);
				GUIUtility.systemCopyBuffer = geometry;
			}
			else
			{
				var graph = (AbstructGeometryGraph)node.owner;
				GUIUtility.systemCopyBuffer = graph.GetGeometry(node, GenerationMode.ForReals, node.name).geometry;
			}
		}

		private void UpdateSettingsExpandedState()
		{
			m_ShowSettings = !m_ShowSettings;
			if (m_ShowSettings)
			{
				m_NodeSettingsView.Add(m_Settings);
				m_NodeSettingsView.visible = true;

				m_SettingsButton.AddToClassList("clicked");
			}
			else
			{
				m_Settings.RemoveFromHierarchy();

				m_NodeSettingsView.visible = false;
				m_SettingsButton.RemoveFromClassList("clicked");
			}
		}

		private void UpdatePreviewExpandedState(bool expanded)
		{
			node.previewExpanded = expanded;
			if (m_PreviewFilter == null)
				return;

			if (expanded)
			{
				if(m_PreviewContainer.parent != this)
				{
					Add(m_PreviewContainer);
				}
				m_PreviewFilter.AddToClassList("expanded");
				m_PreviewFilter.RemoveFromClassList("collapsed");
			}
			else
			{
				if(m_PreviewContainer.parent == m_PreviewFilter)
				{
					m_PreviewContainer.RemoveFromHierarchy();
				}
				m_PreviewFilter.RemoveFromClassList("expanded");
				m_PreviewFilter.AddToClassList("collapsed");
			}
		}

		private void UpdateTitle()
		{
			var subGraphNode = node as SubGraphNode;
			//if (subGraphNode != null && subGraphNode.subGraphAsset != null)
				//title = subGraphNode.subGraphAsset.name;
			//else
				title = node.name;
		}

		public void OnModified(ModificationScope scope)
		{
			UpdateTitle();
			if (node.hasPreview)
				UpdatePreviewExpandedState(node.previewExpanded);

			base.expanded = node.drawState.expanded;

			// Update slots to match node modification
			if(scope == ModificationScope.Topological)
			{
				var slots = node.GetSlots<GeometrySlot>().ToList();

				var inputPorts = inputContainer.Children().OfType<GeometryPort>().ToList();
				foreach(var port in inputPorts)
				{
					var currentSlot = port.slot;
					var newSlot = slots.FirstOrDefault(s => s.id == currentSlot.id);
					if(newSlot == null)
					{
						// Slot doesn't exist anymore, remove it
						inputContainer.Remove(port);

						// We also need to remove the inline input
						var portInputView = m_PortInputContainer.Query<PortInputView>().Where(v => Equals(v.slot, port.slot)).First();
						if (portInputView != null)
							portInputView.RemoveFromHierarchy();
					}
					else
					{
						port.slot = newSlot;
						var portInputView = m_PortInputContainer.Query<PortInputView>().Where(x => x.slot.id == currentSlot.id).First();
						portInputView.UpdateSlot(newSlot);

						slots.Remove(newSlot);
					}
				}

				var outputPorts = outputContainer.Children().OfType<GeometryPort>().ToList();
				foreach(var port in outputPorts)
				{
					var currentSlot = port.slot;
					var newSlot = slots.FirstOrDefault(s => s.id == currentSlot.id);
					if(newSlot == null)
					{
						outputContainer.Remove(port);
					}
					else
					{
						port.slot = newSlot;
						slots.Remove(newSlot);
					}
				}

				AddSlots(slots);

				slots.Clear();
				slots.AddRange(node.GetSlots<GeometrySlot>());

				if (inputContainer.childCount > 0)
					inputContainer.Sort((x, y) => slots.IndexOf(((GeometryPort)x).slot) - slots.IndexOf(((GeometryPort)y).slot));
				if (outputContainer.childCount > 0)
					outputContainer.Sort((x, y) => slots.IndexOf(((GeometryPort)x).slot) - slots.IndexOf(((GeometryPort)y).slot));
			}

			RefreshExpandedState(); //This should not be needed. GraphView needs to improve the extension api here
			UpdatePortInputs();
			UpdatePortInputVisiblities();

			foreach(var control in m_ControlItems.Children())
			{
				var listener = control as INodeModificationListener;
				if (listener != null)
					listener.OnNodeModified(scope);
			}
		}

		private void AddSlots(IEnumerable<GeometrySlot> slots)
		{
			foreach(var slot in slots)
			{
				if (slot.hidden)
					continue;

				var port = GeometryPort.Create(slot, m_ConnectorListerner);
				if (slot.isOutputSlot)
					outputContainer.Add(port);
				else
					inputContainer.Add(port);
			}
		}

		private void UpdatePortInputs()
		{
			inputContainer.Query<GeometryPort>().ForEach(port =>
			{
				if (!(m_PortInputContainer.Query<PortInputView>().Where(a => Equals(a.slot, port.slot)).First() != null))
				{
					var portInputView = new PortInputView(port.slot) { style = { position = Position.Absolute } };
					m_PortInputContainer.Add(portInputView);
					port.RegisterCallback<GeometryChangedEvent>(evt => UpdatePortInput((GeometryPort)evt.target));
				}
			});
		}

		private void UpdatePortInput(GeometryPort port)
		{
			var inputView = m_PortInputContainer.Query<PortInputView>().Where(x => Equals(x.slot, port.slot)).First();

			var currentRect = new Rect(inputView.style.left.value.value, inputView.style.top.value.value, inputView.style.width.value.value, inputView.style.height.value.value);
			var targetRect = new Rect(0f, 0f, port.layout.width, port.layout.height);
			targetRect = port.ChangeCoordinatesTo(inputView.parent, targetRect);
			var centerY = targetRect.center.y;
			var centerX = targetRect.xMax - currentRect.width;
			currentRect.center = new Vector2(centerX, centerY);

			inputView.style.top = currentRect.yMin;
			var newHeight = inputView.parent.layout.height;
			foreach (var element in inputView.parent.Children())
				newHeight = Mathf.Max(newHeight, element.style.top.value.value + element.layout.height);
			if (Math.Abs(inputView.parent.style.height.value.value - newHeight) > 1e-3)
				inputView.parent.style.height = newHeight;
		}

		public void UpdatePortInputVisiblities()
		{
			m_PortInputContainer.Query<PortInputView>().ForEach(portInputView =>
			{
				var slot = portInputView.slot;
				var oldVisiblity = portInputView.visible;
				portInputView.visible = expanded && !node.owner.GetEdges(node.GetSlotReference(slot.id)).Any();
				if (portInputView.visible != oldVisiblity)
					m_PortInputContainer.MarkDirtyRepaint();
			});
		}

		public void UpdatePortInputTypes()
		{
			void UpdateAnchor(GeometryPort anchor)
			{
				var slot = anchor.slot;
				anchor.portName = slot.displayName;
				anchor.visualClass = slot.concreteValueType.ToClassName();
			}
			inputContainer.Query<GeometryPort>().ForEach(UpdateAnchor);
			outputContainer.Query<GeometryPort>().ForEach(UpdateAnchor);

			m_PortInputContainer.Query<PortInputView>().ForEach(portInputView =>
			{
				portInputView.UpdateSlotType();
			});

			foreach(var control in m_ControlItems.Children())
			{
				var listerner = control as INodeModificationListener;
				if (listerner != null)
					listerner.OnNodeModified(ModificationScope.Graph);
			}
		}

		private void OnResize(Vector2 deltaSize)
		{
			var updatedWidth = topContainer.layout.width + deltaSize.x;
			var updatedHeight = m_PreviewImage.layout.height + deltaSize.y;

			// TODO
			//var previewNode = node as PreviewNode;
			//if(previewNode != null)
			//{
			//	previewNode.SetDimensions(updatedWidth, updatedHeight);
			//	UpdateSize();
			//}
		}

		private void UpdatePreviewTexture()
		{
			if(m_PreviewRenderData.texture == null || !node.previewExpanded)
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
			}
		}

		private void UpdateSize()
		{
			//var previewNode = node as PreviewNode;

			//if (previewNode == null)
			//	return;

			//var width = previewNode.width;
			//var height = previewNode.height;

			//m_PreviewImage.style.height = height;
			//m_PreviewImage.style.width = width;
		}

		public void Dispose()
		{
			m_PortInputContainer.Query<PortInputView>().ForEach(portInputView =>
			{
				portInputView.Dispose();
			});

			node = null;
			if (m_PreviewRenderData != null)
			{
				m_PreviewRenderData.onPreviewChanged -= UpdatePreviewTexture;
				m_PreviewRenderData = null;
			}
		}

		public string persistenceKey { get; set; }
	}
}
