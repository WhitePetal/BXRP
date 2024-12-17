using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    [AttributeUsage(AttributeTargets.Struct)]
    public class GenerateBlocksAttribute : Attribute
    {
        internal string path { get; set; }

        public GenerateBlocksAttribute(string path = "")
        {
            this.path = path;
        }
    }
}
