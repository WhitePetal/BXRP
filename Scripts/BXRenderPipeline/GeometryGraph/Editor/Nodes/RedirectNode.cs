using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    public class RedirectNode : Node
    {
        public RedirectNode()
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/RedirectNode"));
        }
    }
}
