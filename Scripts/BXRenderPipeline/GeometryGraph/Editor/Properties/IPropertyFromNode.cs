using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    internal interface IPropertyFromNode
    {
        IGeometryProperty AsGeometryProperty();
        int outputSlotID { get; }
    }
}
