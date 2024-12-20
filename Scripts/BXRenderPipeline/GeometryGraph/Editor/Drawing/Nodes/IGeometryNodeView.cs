using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor.Experimental.GraphView;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    interface IGeometryNodeView : IDisposable
    {
        Node gvNode { get; }
        AbstractGeometryNode node { get; }
        VisualElement colorElement { get; }
        void SetColor(Color newColor);
        void ResetColor();
        void UpdatePortInputTypes();
        void UpdateDropdownEntries();
        void OnModified(ModificationScope scope);
        void AttachMessage(string errString, GeometryCompilerMessageSeverity severity);
        void ClearMessage();
        // Searches the ports on this node for one that matches the given slot.
        // Returns true if found, false if not.
        bool FindPort(SlotReference slot, out GeometryPort port);
    }
}
