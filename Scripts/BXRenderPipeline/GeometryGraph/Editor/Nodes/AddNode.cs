using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BXGeometryGraph.Runtime;
using UnityEngine;

namespace BXGeometryGraph
{
    [Title("Math", "Basic", "Add")]
    class AddNode : CodeFunctionNode
    {
        public AddNode()
        {
            name = "Add";
            synonyms = new string[] { "addition", "sum", "plus" };
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("BX_Add", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string BX_Add(
            [Slot(0, Binding.None)] DynamicDimensionVector A,
            [Slot(1, Binding.None)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
            @"
            {
                Out = A + B;
            }
            ";
        }

        public override AbstractGeometryJob BuildGeometryJob()
        {
            throw new System.NotImplementedException();
        }
    }
}
