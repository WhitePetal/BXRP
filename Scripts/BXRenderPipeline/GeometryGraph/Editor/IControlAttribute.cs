using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    public interface IControlAttribute
    {
        VisualElement InstantiateControl(AbstractGeometryNode node, PropertyInfo propertyInfo);
    }
}
