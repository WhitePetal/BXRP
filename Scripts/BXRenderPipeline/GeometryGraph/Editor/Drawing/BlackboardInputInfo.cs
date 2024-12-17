using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    [AttributeUsage(AttributeTargets.Class)]
    public class BlackboardInputInfo : Attribute
    {
        public float priority;
        public string name;

        /// <summary>
        /// Provide additional information to provide the blackboard for order and name of the ShaderInput item.
        /// </summary>
        /// <param name="priority">Priority of the item, higher values will result in lower positions in the menu.</param>
        /// <param name="name">Name of the item. If null, the class name of the item will be used instead.</param>
        public BlackboardInputInfo(float priority, string name = null)
        {
            this.priority = priority;
            this.name = name;
        }
    }
}
