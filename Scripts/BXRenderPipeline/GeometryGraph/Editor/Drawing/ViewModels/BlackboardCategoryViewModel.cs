using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    class BlackboardCategoryViewModel : IGGViewModel
    {
        public VisualElement parentView { get; set; }
        internal string name { get; set; }
        internal string associatedCategoryGuid { get; set; }
        internal bool isExpanded { get; set; }
        internal Action<IGraphDataAction> requestModelChangeAction { get; set; }

        // Wipes all data in this view-model
        public void ResetViewModelData()
        {
            name = String.Empty;
            associatedCategoryGuid = String.Empty;
            isExpanded = false;
            requestModelChangeAction = null;
        }
    }
}