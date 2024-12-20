using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    class RedirectNodeView : RedirectNode, IGeometryNodeView
    {
        IEdgeConnectorListener m_ConnectorListener;

        // Tie the nodeView to its data
        public void ConnectToData(AbstractGeometryNode inNode, IEdgeConnectorListener connectorListener)
        {
            if (inNode == null)
                return;

            // Set references
            node = inNode;
            title = "";
            m_ConnectorListener = connectorListener;

            viewDataKey = node.objectId;

            // Set the VisualElement's position
            SetPosition(new Rect(node.drawState.position.x, node.drawState.position.y, 0, 0));
            AddSlots(node.GetSlots<GeometrySlot>());

            // Removing a divider that made the ui a bit ugly
            VisualElement contents = mainContainer.Q("contents");
            VisualElement divider = contents?.Q("divider");

            if (divider != null)
            {
                divider.RemoveFromHierarchy();
            }
        }

        public void AddSlots(IEnumerable<GeometrySlot> slots)
        {
            foreach (var slot in slots)
            {
                if (slot.hidden)
                    continue;

                var port = GeometryPort.Create(slot, m_ConnectorListener);

                if (slot.isOutputSlot)
                    outputContainer.Add(port);
                else
                    inputContainer.Add(port);
            }
        }

        public Node gvNode => this;
        public AbstractGeometryNode node { get; private set; }
        public VisualElement colorElement { get { return this; } }

        public void Dispose()
        {
            node = null;
            userData = null;
            m_ConnectorListener = null;
            inputContainer.Clear();
            outputContainer.Clear();
        }

        public void UpdatePortInputTypes()
        {
            foreach (var anchor in inputContainer.Children().Concat(outputContainer.Children()).OfType<GeometryPort>())
            {
                var slot = anchor.slot;
                anchor.portName = slot.displayName;
                anchor.visualClass = slot.concreteValueType.ToClassName();
            }
        }

        public void OnModified(ModificationScope scope)
        {
            if (scope == ModificationScope.Topological)
            {
                var slots = node.GetSlots<GeometrySlot>().ToList();

                var inputPorts = inputContainer.Children().OfType<GeometryPort>().ToList();
                foreach (var port in inputPorts)
                {
                    var currentSlot = port.slot;
                    var newSlot = slots.FirstOrDefault(s => s.id == currentSlot.id);
                    if (newSlot == null)
                    {
                        // Slot doesn't exist anymore, remove it
                        inputContainer.Remove(port);
                    }
                    else
                    {
                        port.slot = newSlot;
                        slots.Remove(newSlot);
                    }
                }

                var outputPorts = outputContainer.Children().OfType<GeometryPort>().ToList();
                foreach (var port in outputPorts)
                {
                    var currentSlot = port.slot;
                    var newSlot = slots.FirstOrDefault(s => s.id == currentSlot.id);
                    if (newSlot == null)
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
        }

        public bool FindPort(SlotReference slot, out GeometryPort port)
        {
            port = contentContainer.Q("top")?.Query<GeometryPort>().Where(p => p.slot.slotReference.Equals(slot)).First();
            return port != null;
        }

        public void AttachMessage(string errString, GeometryCompilerMessageSeverity severity)
        {
            ClearMessage();
            IconBadge badge;
            badge = IconBadge.CreateComment(errString);

            Add(badge);
            badge.AttachTo(outputContainer, SpriteAlignment.RightCenter);
        }

        public void ClearMessage()
        {
            var badge = this.Q<IconBadge>();
            if (badge != null)
            {
                badge.Detach();
                badge.RemoveFromHierarchy();
            }
        }

        public void SetColor(Color newColor)
        {
        }

        public void ResetColor()
        {
        }

        public void UpdateDropdownEntries()
        {
        }
    }
}
