using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    public class FormerNameAttribute : Attribute
    {
        public string fullName { get; private set; }

        public FormerNameAttribute(string fullName)
        {
            this.fullName = fullName;
        }
    }
}
