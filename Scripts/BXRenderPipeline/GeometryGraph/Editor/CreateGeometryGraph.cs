using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BXGeometryGraph
{
    static class CreateGeometryGraph
    {
        //[MenuItem("Assets/Create/Geometry Graph/Blank Geometry Graph", priority = CoreUtils.Sections.section1 + CoreUtils.Priorities.assetsCreateShaderMenuPriority)]
        //public static void CreateBlankGeometryGraph()
        //{
        //    GraphUtil.CreateNewGraph();
        //}

        [MenuItem("Assets/Create/Geometry Graph/Default Geometry Graph", priority = CoreUtils.Priorities.assetsCreateShaderMenuPriority)]
        public static void CreateBXGeometryGraph()
        {
            var target = (BXGGTarget)Activator.CreateInstance(typeof(BXGGTarget));
            //target.TrySetActiveSubTarget(typeof(BXGGSubTarget));

            var blockDescriptors = new[]
            {
                BlockFields.GeometryDescription.Geometry
            };

            GraphUtil.CreateNewGraphWithOutputs(new[] { target }, blockDescriptors);
        }
    }
}
