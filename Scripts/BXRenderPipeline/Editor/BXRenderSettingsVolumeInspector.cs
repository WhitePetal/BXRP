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
    }
}