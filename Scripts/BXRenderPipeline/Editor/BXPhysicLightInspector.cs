using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;

namespace BXRenderPipeline
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(BXPhysicsLightSetting))]
    public class BXPhysicLightInspector : Editor
    {
        private static GUIContent intensityTypeContent = new GUIContent("光强类型", "IntensityType");
        private static GUIContent colorSystemTypeContent = new GUIContent("颜色系统", "ColorSystem");
        private static GUIContent colorTemperatureContent = new GUIContent("色温(K)", "Temperature(K)");
        private static GUIContent radiantPowerContent = new GUIContent("辐射通量(W)", "RadiantPower(W)");
        private static GUIContent colorContent = new GUIContent("光源颜色(RGB)", "LightColor(RGB)");
        private static GUIContent lightEffectContent = new GUIContent("光源效率(/)", "LightEffect(/)");
        private static GUIContent luminousEfficacyContent = new GUIContent("光视效比(/)", "LuminousEfficacy(/)");
        private static GUIContent luminousPowerContent = new GUIContent("光通量(lm)", "LuminousPower(lm)");
        private static GUIContent luminous_intensity = new GUIContent("光强度(cd)", "LuminousIntensity(cd)");
        private static GUIContent illuminanceContent = new GUIContent("照度(lx)", "Illuminance(lx)");
        private static GUIContent luminanceContent = new GUIContent("亮度(cd/m^2)", "Luminance(cd/m^2)");
        private static GUIContent ev100Content = new GUIContent("EV100", "EV100");
        private static GUIContent iesContent = new GUIContent("IES Texture", "IES");

        private BXPhysicsLightSetting physicLight;
        private Light light;

        private void OnEnable()
        {
            physicLight = target as BXPhysicsLightSetting;
            light = physicLight.GetComponent<Light>();
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("intensityType"), intensityTypeContent);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("colorSystemType"), colorSystemTypeContent);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("color_temperature"), colorTemperatureContent);

            GUI.enabled = false;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("color"), colorContent);

            bool byRadiantPower = physicLight.intensityType == BXPhysicsLightSetting.IntensityType.RadiantPower;
            GUI.enabled = byRadiantPower;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("radiant_power"), radiantPowerContent);

            GUI.enabled = true;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("light_efficacy"), lightEffectContent);
            GUI.enabled = false;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("luminous_efficacy"), luminousEfficacyContent);

            GUI.enabled = physicLight.intensityType == BXPhysicsLightSetting.IntensityType.LuminousPower;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("luminous_power"), luminousPowerContent);

            GUI.enabled = physicLight.intensityType == BXPhysicsLightSetting.IntensityType.LuminousIntensity;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("luminous_intensity"), luminous_intensity);

            GUI.enabled = physicLight.intensityType == BXPhysicsLightSetting.IntensityType.Illuminance;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("illuminance"), illuminanceContent);

            GUI.enabled = physicLight.intensityType == BXPhysicsLightSetting.IntensityType.Luminance;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("luminance"), luminanceContent);

            GUI.enabled = physicLight.intensityType == BXPhysicsLightSetting.IntensityType.EV100;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ev100"), ev100Content);

            GUI.enabled = true;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ies"), iesContent);
            if (GUILayout.Button("根据IES设置光源参数") && physicLight.ies != null)
            {
                string iesProfilerPath = AssetDatabase.GetAssetPath(physicLight.ies);
                IESObject iesProfile = AssetDatabase.LoadAssetAtPath<IESObject>(iesProfilerPath);
                physicLight.luminous_intensity = iesProfile.iesMetaData.IESMaximumIntensity;
            }
          

            serializedObject.ApplyModifiedProperties();


            physicLight.UpdateColor();
            light.color = physicLight.color;
            switch (physicLight.intensityType)
            {
                case BXPhysicsLightSetting.IntensityType.RadiantPower:
                    physicLight.UpdateByRadiantPower();
                    break;
                case BXPhysicsLightSetting.IntensityType.LuminousIntensity:
                    physicLight.UpdateByLuminousIntensity();
                    break;
            }
        }
    }
}
