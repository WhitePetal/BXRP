using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    class GeometryInputViewModel : IGGViewModel
    {
        public GeometryInput model { get; set; }

        public VisualElement parentView { get; set; }

        internal bool isSubGraph { get; set; }
        internal bool isInputExposed { get; set; }

        internal string inputName { get; set; }

        internal string inputTypeName { get; set; }

        internal Action<IGraphDataAction> requestModelChangeAction { get; set; }

        public void ResetViewModelData()
        {
            isSubGraph = false;
            isInputExposed = false;
            inputName = String.Empty;
            inputTypeName = String.Empty;
            requestModelChangeAction = null;
        }
    }
}
