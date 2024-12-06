using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    public interface IGeometrySlotHasValue<T>
    {
        T defaultValue { get; }
        T value { get; }
    }
}
