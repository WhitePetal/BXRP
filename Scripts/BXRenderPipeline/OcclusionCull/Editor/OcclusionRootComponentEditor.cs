using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BXRenderPipeline.OcclusionCulling
{
    [CustomEditor(typeof(OcclusionRootComponent))]
    public class OcclusionRootComponentEditor : Editor
    {
		private OcclusionRootComponent cmpt;

		private void OnEnable()
		{
			cmpt = target as OcclusionRootComponent;
		}

		public override void OnInspectorGUI()
		{
			if (GUILayout.Button("烘焙"))
			{
				cmpt.Bake();
			}
			base.OnInspectorGUI();
		}
	}
}
