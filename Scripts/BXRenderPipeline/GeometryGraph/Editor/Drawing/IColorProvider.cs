using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    public interface IColorProvider
    {
        string GetTitle();

        bool AllowCustom();

        bool ClearOnDirty();

        void ApplyColor(IGeometryNodeView nodeview);
        void ClearColor(IGeometryNodeView, nodeview);
    }
}
