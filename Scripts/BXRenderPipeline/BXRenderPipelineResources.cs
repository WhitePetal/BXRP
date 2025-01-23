using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BXRenderPipeline
{
    /// <summary>
    /// Classes implementing this interface contain resources for SRP. 
    /// They appear in GraphicsSettings as other <see cref="IRenderPipelineGraphicsSettings"/>.
    /// Inside it, all fields with <see cref="ResourcePathAttribute"/> will be reloaded to the given resource if their value is null.
    /// </summary>
    public interface IRenderPipelineResources
    { }

    internal static class BXRenderPipelineResourcesEditorUtils
    {
        public enum ResultStatus
        {
            NothingToUpdate, //There was nothing to reload
            InvalidPathOrNameFound, //Encountered a path that do not exist
            ResourceReloaded, //Some resources got reloaded
            SkipedDueToNotMainThread, //Worker Thread cannot fully load resources
        }

        internal static bool TryReloadContainedNullFields(IRenderPipelineResources resource, out ResultStatus result, out string message)
        {
#if UNITY_EDITOR
            message = string.Empty;

            result = ResultStatus.NothingToUpdate;

            try
            {
                if (AssetDatabase.IsAssetImportWorkerProcess())
                    result = ResultStatus.SkipedDueToNotMainThread;
                else if (new Reloader(resource).hasChanged)
                    result = ResultStatus.ResourceReloaded;
            }
            catch (BXInvalidImportException e)
            {
                message = e.Message;
                result = ResultStatus.InvalidPathOrNameFound;
                return false;
            }


            return true;
#else
            return true;
#endif
        }

        struct Reloader
        {
            IRenderPipelineResources mainContainer;
            //string root;
            public bool hasChanged { get; private set; }

            public Reloader(IRenderPipelineResources container)
            {
                mainContainer = container;
                hasChanged = false;
                //root = GetRootPathForType(container.GetType());
                ReloadNullFields(container);
            }

            //static string GetRootPathForType(Type type)
            //{
            //    //Warning: PackageManager.PackageInfo.FindForAssembly will always provide null in Worker thread
            //    var packageInfo = PackageManager.PackageInfo.FindForAssembly(type.Assembly);
            //    return packageInfo == null ? "Assets/" : $"Packages/{packageInfo.name}/";
            //}

            (string[] paths, SearchType location, bool isField) GetResourcesPaths(FieldInfo fieldInfo)
            {
                var attr = fieldInfo.GetCustomAttribute<ResourcePathsBaseAttribute>(inherit: false);
                return (attr?.paths, attr?.location ?? default, attr?.isField ?? default);
            }

            //string GetFullPath(string path, SearchType location)
            //    => location == SearchType.ProjectPath
            //    ? $"{root}{path}"
            //    : path;
            string GetFullPath(string path, SearchType location)
                => path;

            bool IsNull(System.Object container, FieldInfo info)
                => IsNull(info.GetValue(container));

            bool IsNull(System.Object field)
                => field == null || field.Equals(null);

            bool ConstructArrayIfNeeded(System.Object container, FieldInfo info, int length)
            {
                if (IsNull(container, info) || ((Array)info.GetValue(container)).Length != length)
                {
                    info.SetValue(container, Activator.CreateInstance(info.FieldType, length));
                    return true;
                }

                return false;
            }

            void ReloadNullFields(System.Object container)
            {
                foreach (var fieldInfo in container.GetType()
                    .GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
                {
                    //Skip element that do not have path
                    (string[] paths, SearchType location, bool isField) = GetResourcesPaths(fieldInfo);
                    if (paths == null)
                        continue;

                    //Field case: reload if null
                    if (isField)
                    {
                        hasChanged |= SetAndLoadIfNull(container, fieldInfo, GetFullPath(paths[0], location), location);
                        continue;
                    }

                    //Array case: Find each null element and reload them
                    hasChanged |= ConstructArrayIfNeeded(container, fieldInfo, paths.Length);
                    var array = (Array)fieldInfo.GetValue(container);
                    for (int index = 0; index < paths.Length; ++index)
                        hasChanged |= SetAndLoadIfNull(array, index, GetFullPath(paths[index], location), location);
                }
            }

            bool SetAndLoadIfNull(System.Object container, FieldInfo info, string path, SearchType location)
            {
                if (IsNull(container, info))
                {
                    info.SetValue(container, Load(path, info.FieldType, location));
                    return true;
                }

                return false;
            }

            bool SetAndLoadIfNull(Array array, int index, string path, SearchType location)
            {
                var element = array.GetValue(index);
                if (IsNull(element))
                {
                    array.SetValue(Load(path, array.GetType().GetElementType(), location), index);
                    return true;
                }

                return false;
            }

            UnityEngine.Object Load(string path, Type type, SearchType location)
            {
                // Check if asset exist.
                // Direct loading can be prevented by AssetDatabase being reloading.
                var guid = AssetDatabase.AssetPathToGUID(path);
                if (location == SearchType.ProjectPath && String.IsNullOrEmpty(guid))
                    throw new BXInvalidImportException($"Failed to find {path} in {location}.");

                UnityEngine.Object result = type == typeof(Shader)
                    ? LoadShader(path, location)
                    : LoadNonShaderAssets(path, type, location);

                if (IsNull(result))
                    switch (location)
                    {
                        case SearchType.ProjectPath: throw new BXInvalidImportException($"Cannot load. Path {path} is correct but AssetDatabase cannot load now.");
                        case SearchType.ShaderName: throw new BXInvalidImportException($"Failed to find {path} in {location}.");
                        case SearchType.BuiltinPath: throw new BXInvalidImportException($"Failed to find {path} in {location}.");
                        case SearchType.BuiltinExtraPath: throw new BXInvalidImportException($"Failed to find {path} in {location}.");
                    }

                return result;
            }

            UnityEngine.Object LoadShader(string path, SearchType location)
            {
                switch (location)
                {
                    case SearchType.ShaderName:
                    case SearchType.BuiltinPath:
                    case SearchType.BuiltinExtraPath:
                        return Shader.Find(path);
                    case SearchType.ProjectPath:
                        return AssetDatabase.LoadAssetAtPath(path, typeof(Shader));
                    default:
                        throw new NotImplementedException($"Unknown {location}");
                }
            }

            UnityEngine.Object LoadNonShaderAssets(string path, Type type, SearchType location)
                => location switch
                {
                    SearchType.BuiltinPath => UnityEngine.Resources.GetBuiltinResource(type, path), //log error if path is wrong and return null
                    SearchType.BuiltinExtraPath => AssetDatabase.GetBuiltinExtraResource(type, path), //log error if path is wrong and return null
                    SearchType.ProjectPath => AssetDatabase.LoadAssetAtPath(path, type), //return null if path is wrong
                    SearchType.ShaderName => throw new ArgumentException($"{nameof(SearchType.ShaderName)} is only available for Shaders."),
                    _ => throw new NotImplementedException($"Unknown {location}")
                };
        }
    }
}