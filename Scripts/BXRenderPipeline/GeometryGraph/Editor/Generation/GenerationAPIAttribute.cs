using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface, Inherited = true, AllowMultiple = false)]
    public class GenerationAPIAttribute : Attribute
    {
        public GenerationAPIAttribute() { }
    }
}
