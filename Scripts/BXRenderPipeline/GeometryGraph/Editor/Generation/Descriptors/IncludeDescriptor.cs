using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    [GenerationAPI]
    [Serializable]
    internal class IncludeDescriptor : IConditional
    {
        public FieldCondition[] fieldConditions => throw new NotImplementedException();
    }
}
