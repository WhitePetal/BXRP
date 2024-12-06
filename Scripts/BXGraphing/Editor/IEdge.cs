using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGraphing
{
    public interface IEdge
    {
        SlotReference outputSlot { get; }
        SlotReference inputSlot { get; }
    }
}
