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
    sealed class ContextView : StackNode, IDisposable
    {
        private ContextData m_ContextData;

        // Currently we only need one Port per context
        // As the Contexts are hardcoded we know their directions
        private Port m_Port;

        //need this from graph view specifically for nodecreation
        private EditorWindow m_EditorWindow;

        // When dealing with more Contexts, `name` should be serialized in the ContextData
        // Right now we dont do this so we dont overcommit to serializing unknowns
        public ContextView(string name, ContextData contextData, EditorWindow editorWindow)
        {
            // Set data
            m_ContextData = contextData;
            m_EditorWindow = editorWindow;

            // Header
            var headerLabel = new Label() { name = "headerLabel" };
            headerLabel.text = name;
            headerContainer.Add(headerLabel);
            inputContainer.style.position = Position.Absolute;
            inputContainer.style.left = 0;
            inputContainer.style.right = 205;
            inputContainer.style.top = 30;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            // Disable the context menu for block nodes. This prevents a duplicate "disconnect all"
            // option from getting registered which grays out stack block node's option.
            if (evt.target is GeometryNodeView) return;

            // If the user didn't click on a block node (i.e. the stack frame), include the "Add Block Node" item.
            InsertCreateNodeAction(evt, childCount, 0);
            evt.menu.InsertSeparator(null, 1);
        }

        public ContextData contextData => m_ContextData;
        public Port port => m_Port;

        // We need to use graphViewChange.movedElements to check whether a BlockNode has moved onto the GraphView
        // but Nodes return in movedElements when they are mid-drag because they are removed from the stack (placeholder)
        // StackNode has `dragEntered` but its protected so we need `isDragging`
        public bool isDragging => dragEntered;

        public void AddPort(Direction direction)
        {
            var capacity = direction == Direction.Input ? Port.Capacity.Single : Port.Capacity.Multi;
            var container = direction == Direction.Input ? inputContainer : outputContainer;
            m_Port = Port.Create<UnityEditor.Experimental.GraphView.Edge>(Orientation.Horizontal, direction, capacity, null);
            m_Port.portName = "";

            // Vertical ports have no representation in Model
            // Therefore we need to disable interaction
            m_Port.pickingMode = PickingMode.Ignore;

            container.Add(m_Port);
        }

        public void InsertBlock(GeometryNodeView nodeView)
        {
            if (!(nodeView.userData is BlockNode blockNode))
                return;

            // If index is -1 the node is being added to the end of the Stack
            if (blockNode.index == -1)
            {
                AddElement(nodeView);
                return;
            }

            // Add or Insert based on index
            if (blockNode.index >= contentContainer.childCount)
            {
                AddElement(nodeView);
            }
            else
            {
                InsertElement(blockNode.index, nodeView);
            }
        }

        public void InsertElements(int insertIndex, IEnumerable<GraphElement> elements)
        {
            var blockDatas = elements.Select(x => x.userData as BlockNode).ToArray();
            for (int i = 0; i < blockDatas.Length; i++)
            {
                contextData.blocks.Remove(blockDatas[i]);
            }

            int count = elements.Count();
            var refs = new JsonRef<BlockNode>[count];
            for (int i = 0; i < count; i++)
            {
                refs[i] = blockDatas[i];
            }

            contextData.blocks.InsertRange(insertIndex, refs);

            var window = m_EditorWindow as GeometryGraphEditWindow;
            window?.graphEditorView?.graphView?.graph?.ValidateCustomBlockLimit();
        }

        protected override bool AcceptsElement(GraphElement element, ref int proposedIndex, int maxIndex)
        {
            return element.userData is BlockNode blockNode && blockNode.descriptor != null &&
                blockNode.descriptor.geometryStage == contextData.geometryStage;
        }

        protected override void OnSeparatorContextualMenuEvent(ContextualMenuPopulateEvent evt, int separatorIndex)
        {
            base.OnSeparatorContextualMenuEvent(evt, separatorIndex);
            InsertCreateNodeAction(evt, separatorIndex, 0);
        }

        void InsertCreateNodeAction(ContextualMenuPopulateEvent evt, int separatorIndex, int itemIndex)
        {
            //we need to arbitrarily add the editor position values because node creation context
            //exptects a non local coordinate
            var mousePosition = evt.mousePosition + m_EditorWindow.position.position;
            var graphView = GetFirstAncestorOfType<GeometryGraphView>();

            evt.menu.InsertAction(itemIndex, "Add Block Node", (e) =>
            {
                var context = new NodeCreationContext
                {
                    screenMousePosition = mousePosition,
                    target = this,
                    index = separatorIndex,
                };
                graphView.nodeCreationRequest(context);
            });
        }

        public void Dispose()
        {
            m_Port = null;
            m_ContextData = null;
            m_EditorWindow = null;
            inputContainer.Clear();
            outputContainer.Clear();
        }
    }
}