using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace BXGeometryGraph
{
    interface IGGResizable : IResizable
    {
        // Depending on the return value, the ElementResizer either allows resizing past parent view edge (like in case of StickyNote) or clamps the size at the edges of parent view (like for GraphSubWindows)
        bool CanResizePastParentBounds();
    }
}
