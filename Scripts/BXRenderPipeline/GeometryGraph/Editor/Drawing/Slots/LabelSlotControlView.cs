using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    class LabelSlotControlView : VisualElement
    {
        public LabelSlotControlView(string label)
        {
            var labelField = new Label(label);
            Add(labelField);
        }
    }
}
