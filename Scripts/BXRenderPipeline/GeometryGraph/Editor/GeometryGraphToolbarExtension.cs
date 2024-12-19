using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    internal interface IGeometryGraphToolbarExtension
    {
        abstract void OnGUI(GeometryGraphView graphView);
    }
}
