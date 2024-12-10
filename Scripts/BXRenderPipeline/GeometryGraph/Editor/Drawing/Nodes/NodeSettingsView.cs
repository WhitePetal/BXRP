using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
	public class NodeSettingsView : VisualElement
	{
		private VisualElement m_ContentContainer;

		public NodeSettingsView()
		{
			pickingMode = PickingMode.Ignore;
			this.AddStyleSheetPath("Assets/Scripts/BXRenderPipeline/GeometryGraph/Editor/Resources/Styles/NodeSettingsView.uss");
			var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Scripts/BXRenderPipeline/GeometryGraph/Editor/Resources/UXML/NodeSettingsView.uxml");
			uxml.CloneTree(this);
			// Get the element we want to use as content container
			m_ContentContainer = this.Q("contentContainer");
			RegisterCallback<MouseDownEvent>(OnMouseDown);
			RegisterCallback<MouseUpEvent>(OnMouseUp);
		}

		void OnMouseUp(MouseUpEvent evt)
		{
			evt.StopPropagation();
		}

		void OnMouseDown(MouseDownEvent evt)
		{
			evt.StopPropagation();
		}

		public override VisualElement contentContainer
		{
			get { return m_ContentContainer; }
		}
	}
}
