using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    [AttributeUsage(AttributeTargets.Class)]
    class HasDependenciesAttribute : Attribute
    {
        public Type minimalType { get; }

        public HasDependenciesAttribute(Type minimalType)
        {
            this.minimalType = minimalType;
        }
    }
}
