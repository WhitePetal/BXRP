using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace BXRenderPipelineEditor
{
    [ScriptedImporter(1, "urtshader")]
    public class UnifiedRTShaderImporter : ScriptedImporter
    {
        const string computeShaderTemplate =
            "#define UNIFIED_RT_BACKEND_COMPUTE\n" +
            "SHADERCODE\n" +
            "#include_with_pragmas \"Assets/Shaders/ShaderLibrarys/RayTracing/Compute/ComputeRaygenShader.hlsl\"\n";

        const string raytracingShaderTemplate =
            "#define UNIFIED_RT_BACKEND_HARDWARE\n" +
            "SHADERCODE\n" +
            "#include_with_pragmas \"Assets/Shaders/ShaderLibrarys/RayTracing/Hardware/HardwareRaygenShader.hlsl\"\n";

        public override void OnImportAsset(AssetImportContext ctx)
        {
            string source = File.ReadAllText(ctx.assetPath);

            var com = ShaderUtil.CreateComputeShaderAsset(ctx, computeShaderTemplate.Replace("SHADERCODE", source));
            var rt = CreateRayTracingShaderAsset(ctx, raytracingShaderTemplate.Replace("SHADERCODE", source));

            ctx.AddObjectToAsset("ComputeShader", com);
            ctx.AddObjectToAsset("RayTracingShader", rt);
            ctx.SetMainObject(com);
        }

        private static RayTracingShader CreateRayTracingShaderAsset(AssetImportContext ctx, string source)
        {
            var path = ctx.assetPath + ".raytrace";
            var fullPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), path);
            var split = ctx.assetPath.Split('/');
            var nameW = split[split.Length - 1];
            AssetDatabase.CreateAsset(new TextAsset(), path);
            File.WriteAllText(fullPath, source);
            RayTracingShader rt = AssetDatabase.LoadAssetAtPath<RayTracingShader>(path);
            rt = Object.Instantiate<RayTracingShader>(rt);
            rt.name = nameW.Replace(".urtshader", "");
            AssetDatabase.DeleteAsset(path);
            return rt;
        }
    }
}
