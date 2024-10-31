using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditorInternal;
using UnityEngine;

namespace BXRenderPipeline
{
    [CustomEditor(typeof(BXRenderSettingsVolume))]
    public class BXRenderSettingsVolumeInspector : Editor
    {
        private BXRenderSettingsVolume volume;
        private ReorderableList componentsReoLst;

        private void OnEnable()
        {
            volume = target as BXRenderSettingsVolume;
            componentsReoLst = new ReorderableList(serializedObject, serializedObject.FindProperty("components"), false, true, false, true);
            componentsReoLst.drawHeaderCallback += DrawComponentsLstHead;
            componentsReoLst.drawElementCallback += DrawComponentsLstElement;
            componentsReoLst.onRemoveCallback += OnComponentLstRemoveElement;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();

            componentsReoLst.DoLayoutList();

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


        private void DrawComponentsLstHead(Rect rect)
        {
            EditorGUI.LabelField(rect, EditorGUIUtility.TrTextContent("Components"));
        }

        private void DrawComponentsLstElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty element = componentsReoLst.serializedProperty.GetArrayElementAtIndex(index);
            EditorGUI.PropertyField(rect, element);
            if (Editor.CreateEditor(element.objectReferenceValue).DrawDefaultInspector())
            {
                element.serializedObject.ApplyModifiedProperties();
            }
            EditorGUILayout.Space();
        }

        private void OnComponentLstRemoveElement(ReorderableList lst)
        {
            var selectedIndices = lst.selectedIndices;
            var components = serializedObject.FindProperty("components");
            if (selectedIndices == null || selectedIndices.Count == 0)
            {
                if(components.arraySize > 0)
                    components.arraySize--;
            }
            else
            {
                for (int i = 0; i < selectedIndices.Count; ++i)
                {
                    int index = selectedIndices[i];
                    components.DeleteArrayElementAtIndex(index);
                }
            }
            serializedObject.ApplyModifiedProperties();
        }
	}
}