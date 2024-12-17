using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    internal interface IPropertyFromNode
    {
        AbstractGeometryProperty AsGeometryProperty();
        int outputSlotID { get; }
    }
}
