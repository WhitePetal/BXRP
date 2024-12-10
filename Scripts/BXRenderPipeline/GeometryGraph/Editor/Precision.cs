using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    // this is generally used for user-selectable precision
    [Serializable]
    enum Precision
    {
        Inherit,    // automatically choose the precision based on the inputs
        Single,     // force single precision (float)
        Half,       // force half precision
        Graph,      // use the graph default (for subgraphs this will properly switch based on the subgraph node setting)
    }

    // this is used when calculating precision within a graph
    // it basically represents the precision after applying the automatic inheritance rules
    // but before applying the fallback to the graph default
    // tracking this explicitly helps us build subgraph switchable precision behavior (any node using Graph can be switched)
    public enum GraphPrecision
    {
        Single = 0,     // the ordering is different here so we can use the min function to resolve inherit/automatic behavior
        Graph = 1,
        Half = 2
    }

    // this is the actual set of precisions we have, a shadergraph must resolve every node to one of these
    // in subgraphs, this concrete precision is only used for preview, and may not represent the actual precision of those nodes
    // when used in a shader graph
    [Serializable]
    public enum ConcretePrecision
    {
        Single,
        Half,
    }
}
