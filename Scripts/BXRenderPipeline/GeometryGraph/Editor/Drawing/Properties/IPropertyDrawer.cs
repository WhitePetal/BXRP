using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    // Interface that should be implemented by any property drawer for the inspector view
    public interface IPropertyDrawer
    {
        Action inspectorUpdateDelegate { get; set; }

        VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, InspectableAttribute attribute);

        void DisposePropertyDrawer();
    }
}
