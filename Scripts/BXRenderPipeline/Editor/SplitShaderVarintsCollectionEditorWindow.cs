#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BXRenderPipeline
{
    public class SplitShaderVarintsCollectionEditorWindow : EditorWindow
    {
        private static SplitShaderVarintsCollectionEditorWindow s_Instance;
        public ShaderVariantCollection svc;

        [MenuItem("Window/Rendering/SplitShaderVarintsCollection")]
        public static void ShowWindow()
        {
            s_Instance = GetWindow<SplitShaderVarintsCollectionEditorWindow>();
        }

        private void OnGUI()
        {
            svc = EditorGUILayout.ObjectField("ShaderVarintsCollection", svc, typeof(ShaderVariantCollection), false) as ShaderVariantCollection;

            if (GUILayout.Button("Split"))
            {
                var so = new SerializedObject(svc);
                var shaders = so.FindProperty("m_Shaders");

                var svcPath = AssetDatabase.GetAssetPath(svc);
                var svcNameW = System.IO.Path.GetFileName(svcPath);
                var svcName = System.IO.Path.GetFileNameWithoutExtension(svcPath);

                SVCC svcc = ScriptableObject.CreateInstance<SVCC>();
                svcc.collections = new ShaderVariantCollection[shaders.arraySize];

                for (int i = 0; i < shaders.arraySize; ++i)
                {
                    EditorUtility.DisplayProgressBar("Creating", "Create SVCC...", i * 1f / shaders.arraySize);
                    var shaderSVC = new ShaderVariantCollection();
                    var entryProp = shaders.GetArrayElementAtIndex(i);
                    var shader = (Shader)entryProp.FindPropertyRelative("first").objectReferenceValue;
                    var variantsProp = entryProp.FindPropertyRelative("second.variants");

                    var shaderPath = AssetDatabase.GetAssetPath(shader);
                    var shaderName = shader.name.Replace("/", "_").Replace(" ", "_");

                    for (int j = 0; j < variantsProp.arraySize; ++j)
                    {
                        var varint = variantsProp.GetArrayElementAtIndex(j);
                        var keywords = varint.FindPropertyRelative("keywords").stringValue;
                        var passTypeS = varint.FindPropertyRelative("passType");
                        var passType = (PassType)passTypeS.intValue;

                        bool createVarintSuccess = true;
                        ShaderVariantCollection.ShaderVariant sv = new ShaderVariantCollection.ShaderVariant();
                        try
                        {
                            sv = new ShaderVariantCollection.ShaderVariant(shader, passType, keywords);
                        }
                        catch (System.Exception e)
                        {
                            createVarintSuccess = false;
                            Debug.LogError(e);
                        }

                        if (createVarintSuccess)
                            shaderSVC.Add(sv);
                    }

                    var newPath = svcPath.Replace(svcNameW, svcName + "_" + shaderName + ".shadervariants");
                    AssetDatabase.CreateAsset(shaderSVC, newPath);

                    svcc.collections[i] = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(newPath);
                }
                EditorUtility.ClearProgressBar();

                AssetDatabase.CreateAsset(svcc, svcPath.Replace(svcNameW, svcName + "_SVC" + ".asset"));
            }
        }
    }
}
#endif