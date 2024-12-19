using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    class NoColors : IColorProvider
    {
        public const string Title = "<None>";
        public string GetTitle() => Title;

        public bool AllowCustom() => false;
        public bool ClearOnDirty() => false;

        public void ApplyColor(IGeometryNodeView nodeView)
        {
        }

        public void ClearColor(IGeometryNodeView nodeView)
        {
        }
    }
}
