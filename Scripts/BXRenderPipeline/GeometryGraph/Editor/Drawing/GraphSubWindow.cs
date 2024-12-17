using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace BXGeometryGraph
{
    interface ISelectionProvider
    {
        List<ISelectable> GetSelection { get; }
    }
}
