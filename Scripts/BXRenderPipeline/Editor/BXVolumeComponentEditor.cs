using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace BXRenderPipeline
{
    // TODO:NEED IMPLEMENT
    [CustomEditor(typeof(BXVolumeComponment), true)]
    public class BXVolumeComponentEditor : Editor
    {
        private const string k_KeyPrefix = "CoreRP:BXVolumeComponent:UI_State:";

        private EditorPrefBool m_EditorPrefBool;

        internal string categoryTitle { get; set; }

        /// <summary>
        /// If the editor for this <see cref="BXVolumeComponment"/> is expanded or not in the inspector
        /// </summary>
        public bool expanded
        {
            get => m_EditorPrefBool.value;
            set => m_EditorPrefBool.value = value;
        }

        internal bool visible { get; private set; }

        private static class Styles
        {
            public static readonly GUIContent k_OverrideSettingText = EditorGUIUtility.TrTextContent("", "Override this setting for this volume.");

            public static readonly GUIContent k_AllText =
                EditorGUIUtility.TrTextContent("ALL", "Toggle all overrides on. To maximize performances you should only toggle overrides that you actually need.");

            public static readonly GUIContent k_NoneText = EditorGUIUtility.TrTextContent("NONE", "Toggle all overrides off.");

            public static string toggleAllText { get; } = L10n.Tr("Toggle All");

            public const int overrideCheckboxWidth = 14;
            public const int overrideCheckboxOffset = 9;
        }

        Vector2? m_OverrideToggleSize;

        internal Vector2 overrideToggleSize
        {
            get
            {
                if (!m_OverrideToggleSize.HasValue)
                    m_OverrideToggleSize = CoreEditorStyles.smallTickbox.CalcSize(Styles.k_OverrideSettingText);
                return m_OverrideToggleSize.Value;
            }
        }

        /// <summary>
        /// Specifies the <see cref="VolumeComponent"/> this editor is drawing.
        /// </summary>
        public BXVolumeComponment volumeComponent => target as BXVolumeComponment;

    }
}
