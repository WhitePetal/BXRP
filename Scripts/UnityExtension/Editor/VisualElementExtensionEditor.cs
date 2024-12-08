using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

public static class VisualElementExtensionEditor
{
    public static void AddStyleSheetPath(this VisualElement element, string sheetPath)
    {
        StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(sheetPath);
        if(styleSheet == null)
        {
            Debug.LogWarning($"Style sheet not found for path \"{sheetPath}\"");
        }
        else
        {
            element.styleSheets.Add(styleSheet);
        }
    }

	//public static VisualElement GetRootVisualContainer(this VisualElement element)
	//{
	//	VisualElement topMostRootContainer = null;
	//	var hierarchyParent = element;
	//	while (hierarchyParent != null)
	//	{
	//		if (hierarchyParent.isRootVisualContainer)
	//		{
	//			topMostRootContainer = hierarchyParent;
	//		}

	//		hierarchyParent = hierarchyParent.hierarchy.parent;
	//	}

	//	return topMostRootContainer;
	//}
}
