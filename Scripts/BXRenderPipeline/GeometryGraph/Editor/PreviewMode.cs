using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    public enum PreviewMode
    {
        Inherit,   // this usually means: 2D, unless a connected input node is 3D, in which case it is 3D
        Preview2D,
        Preview3D
    }
}
