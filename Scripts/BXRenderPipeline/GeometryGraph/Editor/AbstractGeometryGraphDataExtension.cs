using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    [Serializable]
    internal abstract class AbstractGeometryGraphDataExtension : JsonObject
    {
        internal virtual int paddingIdentationFactor => 15;

        internal abstract string displayName { get; }

        internal abstract void OnPropertiesGUI(VisualElement context, Action onChange, Action<string> registerUndo, GraphData owner);

        internal static List<AbstractGeometryGraphDataExtension> ValidExtensions()
        {
            var result = new List<AbstractGeometryGraphDataExtension>();
            foreach (var type in TypeCache.GetTypesDerivedFrom(typeof(AbstractGeometryGraphDataExtension)))
            {
                if (type.IsGenericType || type == typeof(MultiJsonInternal.UnknownGraphDataExtension))
                    continue;

                var subData = (AbstractGeometryGraphDataExtension)Activator.CreateInstance(type);
                result.Add(subData);
            }
            return result;
        }
    }
}
