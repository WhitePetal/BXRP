using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    // An action takes in a reference to a GraphData object and performs some modification on it
    interface IGraphDataAction
    {
        Action<GraphData> modifyGraphDataAction { get; }
    }

}