using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    interface IConditional
    {
        FieldCondition[] fieldConditions { get; }
    }
}
