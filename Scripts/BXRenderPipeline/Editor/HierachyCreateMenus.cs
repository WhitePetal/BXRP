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
        [MenuItem("GameObject/BXRenderPipeline/RenderSettings/BXGlobalVolume", priority = CoreUtils.Sections.section2 + CoreUtils.Priorities.gameObjectMenuPriority)]
        public static void CreateBXGlobalVolume(MenuCommand menuCommand)
        {
            var go = CoreEditorUtils.CreateGameObject("BXGlobalVolume", menuCommand.context);
            var volume = go.AddComponent<BXRenderSettingsVolume>();
            volume.isGlobal = true;
            volume.blendDistance = 1f;
        }

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





        [MenuItem("GameObject/BXRenderPipeline/BXLights/DirectionalLight", priority = CoreUtils.Sections.section1 + CoreUtils.Priorities.gameObjectMenuPriority)]
        public static void CreateBXDirectionalLight(MenuCommand menuCommand)
        {
            var go = CoreEditorUtils.CreateGameObject("BXDirectionalLight", menuCommand.context);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            var lightSetting = go.AddComponent<BXPhysicsLightSetting>();
            lightSetting.intensityType = BXPhysicsLightSetting.IntensityType.Illuminance;
        }

        [MenuItem("GameObject/BXRenderPipeline/BXLights/PointLight", priority = CoreUtils.Sections.section1 + CoreUtils.Priorities.gameObjectMenuPriority)]
        public static void CreateBXPointLight(MenuCommand menuCommand)
        {
            var go = CoreEditorUtils.CreateGameObject("BXPointLight", menuCommand.context);
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            var lightSetting = go.AddComponent<BXPhysicsLightSetting>();
            lightSetting.intensityType = BXPhysicsLightSetting.IntensityType.RadiantPower;
        }

        [MenuItem("GameObject/BXRenderPipeline/BXLights/SpotLight", priority = CoreUtils.Sections.section1 + CoreUtils.Priorities.gameObjectMenuPriority)]
        public static void CreateBXSpotLight(MenuCommand menuCommand)
        {
            var go = CoreEditorUtils.CreateGameObject("BXSpotLight", menuCommand.context);
            var light = go.AddComponent<Light>();
            light.type = LightType.Spot;
            var lightSetting = go.AddComponent<BXPhysicsLightSetting>();
            lightSetting.intensityType = BXPhysicsLightSetting.IntensityType.RadiantPower;
        }
    }
}
