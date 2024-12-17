using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    [GenerationAPI]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Event | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    class TitleAttribute : ContextFilterableAttribute
    {
        public string[] title;
        public TitleAttribute(params string[] title) { this.title = title; }
    }
}