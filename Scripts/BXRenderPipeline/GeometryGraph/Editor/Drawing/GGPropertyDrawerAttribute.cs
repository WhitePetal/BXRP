using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    [AttributeUsage(AttributeTargets.Class)]
    public class GGPropertyDrawerAttribute : Attribute
    {
        public Type propertyType { get; private set; }

        public GGPropertyDrawerAttribute(Type propertyType)
        {
            this.propertyType = propertyType;
        }
    }
}
