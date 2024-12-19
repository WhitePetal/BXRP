using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    internal interface IGetNodePropertyDrawerPropertyData
    {
        void GetPropertyData(Action setNoddesAsDirtyCallback, Action updateNodeViewCallback);
    }

    [GGPropertyDrawer(typeof(AbstractGeometryNode))]
    public class AbstractGeometryNodePropertyDrawer : IPropertyDrawer, IGetNodePropertyDrawerPropertyData
    {
        public Action inspectorUpdateDelegate { get; set; }

        Action m_setNodesAsDirtyCallback;
        Action m_updateNodeViewsCallback;

        public void GetPropertyData(Action setNodesAsDirtyCallback, Action updateNodeViewCallback)
        {
            m_setNodesAsDirtyCallback = setNodesAsDirtyCallback;
            m_updateNodeViewsCallback = updateNodeViewCallback;
        }

        internal virtual void AddCustomNodeProperties(VisualElement parentElement, AbstractGeometryNode node, Action setNodesAsDirtyCallback, Action updateNodeViewsCallback)
        {
        }

        VisualElement CreateGUI(AbstractGeometryNode node, InspectableAttribute attribute, out VisualElement propertyVisualElement)
        {
            VisualElement nodeSettings = new VisualElement();
            var nameLabel = PropertyDrawerUtils.CreateLabel($"{node.displayName} Node", 0, FontStyle.Bold);
            nodeSettings.Add(nameLabel);
            if (node.ggVersion < node.latestVersion)
            {
                string deprecationText = null;
                string buttonText = null;
                string labelText = null;
                MessageType messageType = MessageType.Warning;
                if (node is IHasCustomDeprecationMessage nodeWithCustomDeprecationSettings)
                {
                    nodeWithCustomDeprecationSettings.GetCustomDeprecationMessage(out deprecationText, out buttonText, out labelText, out messageType);
                }

                Action dismissAction = null;
                if (node.dismissedUpdateVersion < node.latestVersion)
                {
                    dismissAction = () =>
                    {   // dismiss
                        m_setNodesAsDirtyCallback?.Invoke();
                        node.owner.owner.RegisterCompleteObjectUndo($"Dismiss {node.name} Node Upgrade Flag");
                        node.dismissedUpdateVersion = node.latestVersion;
                        node.owner.messageManager.ClearNodesFromProvider(node.owner, new AbstractGeometryNode[] { node });
                        node.Dirty(ModificationScope.Graph);
                        inspectorUpdateDelegate?.Invoke();
                        m_updateNodeViewsCallback?.Invoke();
                    };
                }

                var help = HelpBoxRow.TryGetDeprecatedHelpBoxRow($"{node.name} Node",
                    () =>
                    {   // upgrade
                        m_setNodesAsDirtyCallback?.Invoke();
                        node.owner.owner.RegisterCompleteObjectUndo($"Update {node.name} Node");
                        node.ChangeVersion(node.latestVersion);
                        node.owner.messageManager.ClearNodesFromProvider(node.owner, new AbstractGeometryNode[] { node });
                        node.Dirty(ModificationScope.Graph);
                        inspectorUpdateDelegate?.Invoke();
                        m_updateNodeViewsCallback?.Invoke();
                    },
                    dismissAction, deprecationText, buttonText, labelText, messageType);

                if (help != null)
                {
                    nodeSettings.Insert(0, help);
                }
            }

            PropertyDrawerUtils.AddDefaultNodeProperties(nodeSettings, node, m_setNodesAsDirtyCallback, m_updateNodeViewsCallback);
            AddCustomNodeProperties(nodeSettings, node, m_setNodesAsDirtyCallback, m_updateNodeViewsCallback);

            propertyVisualElement = null;

            return nodeSettings;
        }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, InspectableAttribute attribute)
        {
            return this.CreateGUI(
                (AbstractGeometryNode)actualObject,
                attribute,
                out var propertyVisualElement);
        }

        internal virtual void DisposePropertyDrawer()
        {
        }

        void IPropertyDrawer.DisposePropertyDrawer() { DisposePropertyDrawer(); }
    }
}
