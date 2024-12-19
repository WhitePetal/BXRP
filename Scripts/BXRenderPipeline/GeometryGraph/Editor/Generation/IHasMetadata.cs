using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    [GenerationAPI]
    interface IHasMetadata
    {
        string identifier { get; }
        ScriptableObject GetMetadataObject(GraphDataReadOnly graph);
    }
}
