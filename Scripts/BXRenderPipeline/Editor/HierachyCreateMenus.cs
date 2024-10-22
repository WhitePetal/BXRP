using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace BXRenderPipeline
{
    public class HierachyCreateMenus
    {
        [MenuItem("GameObject/BXRenderPipeline/RenderSettings/BXCubeVolume", priority = CoreUtils.Sections.section2 + CoreUtils.Priorities.gameObjectMenuPriority)]
        public static void CreateBXCubeVolume(MenuCommand menuCommand)
		{
            var go = CoreEditorUtils.CreateGameObject("BXCubeVolume", menuCommand.context);
            var collider = go.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            var volume = go.AddComponent<BXRenderSettingsVolume>();
            volume.isGlobal = false;
            volume.blendDistance = 1f;
		}
    }
}
