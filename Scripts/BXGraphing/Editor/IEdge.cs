using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    interface IEdge
    {
        SlotReference outputSlot { get; }
        SlotReference inputSlot { get; }
    }
}
