using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    interface IColorProvider
    {
        string GetTitle();

        bool AllowCustom();

        bool ClearOnDirty();

        void ApplyColor(IGeometryNodeView nodeView);
        void ClearColor(IGeometryNodeView nodeView);
    }
}
