using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static BXGeometryGraph.GeometryInputPropertyDrawer;

namespace BXGeometryGraph
{
    interface IGeometryPropertyDrawer
    {
        internal void HandlePropertyField(PropertySheet propertySheet, PreChangeValueCallback preChangeValueCallback, PostChangeValueCallback postChangeValueCallback);
    }
}
