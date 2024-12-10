using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    [Serializable]
    public class ShaderKeyword : GeometryInput
    {
        internal override ConcreteSlotValueType concreteShaderValueType => throw new NotImplementedException();

        internal override bool isExposable => throw new NotImplementedException();

        internal override bool isRenamable => throw new NotImplementedException();

        internal override GeometryInput Copy()
        {
            throw new NotImplementedException();
        }
    }
}
