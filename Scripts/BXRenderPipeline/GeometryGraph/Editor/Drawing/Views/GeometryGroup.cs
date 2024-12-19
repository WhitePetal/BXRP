using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    sealed class GeometryGroup : Group
    {
        private GraphData m_Graph;
        public new GroupData userData
        {
            get => (GroupData)base.userData;
            set => base.userData = value;
        }

        public GeometryGroup()
        {
            VisualElementExtensions.AddManipulator(this, new ContextualMenuManipulator(BuildContextualMenu));
            style.backgroundColor = new StyleColor(new Color(25 / 255f, 25 / 255f, 25 / 255f, 25 / 255f));
            capabilities |= Capabilities.Ascendable;
        }

        public void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
        }

        public override bool AcceptsElement(GraphElement element, ref string reasonWhyNotAccepted)
        {
            if(element is StackNode stackNode)
            {
                reasonWhyNotAccepted = "Geometry, Vertex and Pixel Stacks cannot be grouped";
                return false;
            }

            var nodeView = element as IGeometryNodeView;
            if(nodeView == null)
            {
                // sticky notes are not nodes, but still groupable
                return true;
            }

            if (nodeView.node is BlockNode)
            {
                reasonWhyNotAccepted = "Block Nodes cannot be grouped";
                return false;
            }

            return true;
        }

        protected override void SetScopePositionOnly(Rect newPos)
        {
            base.SetScopePositionOnly(newPos);

            userData.position = newPos.position;
        }
    }
}
