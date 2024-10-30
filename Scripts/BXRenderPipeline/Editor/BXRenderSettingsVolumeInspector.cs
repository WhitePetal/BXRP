using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace BXRenderPipeline
{
    [CustomEditor(typeof(BXRenderSettingsVolume))]
    public class BXRenderSettingsVolumeInspector : Editor
    {
        private BXRenderSettingsVolume volume;

        private void OnEnable()
        {
            volume = target as BXRenderSettingsVolume;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();

            using (var hscope = new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(EditorGUIUtility.TrTextContent("Add Override"), EditorStyles.miniButton))
                {
                    var r = hscope.rect;
                    var pos = new Vector2(r.x + r.width / 2f, r.yMax + 18f);
                    FilterWindow.Show(pos, new BXVolumeComponentProvider(volume, this));
                }
            }
        }

		public void AddComponent(Type componentType)
		{
			serializedObject.Update();
			var component = (BXVolumeComponment)ScriptableObject.CreateInstance(componentType);
			component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
			component.name = componentType.Name;
			Undo.RegisterCreatedObjectUndo(component, "Add Volume Override");

			var components = serializedObject.FindProperty("components");
			components.arraySize++;
			var refComponent = components.GetArrayElementAtIndex(components.arraySize - 1);
			refComponent.objectReferenceValue = component;
			serializedObject.ApplyModifiedProperties();
		}

	}
}