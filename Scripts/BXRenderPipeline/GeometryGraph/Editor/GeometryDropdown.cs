using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    [Serializable]
    public class GeometryDropdown : GeometryInput
    {
        internal override ConcreteSlotValueType concreteShaderValueType => throw new System.NotImplementedException();

        internal override bool isExposable => throw new System.NotImplementedException();

        internal override bool isRenamable => throw new System.NotImplementedException();

        internal override GeometryInput Copy()
        {
            throw new System.NotImplementedException();
        }
    }
}
