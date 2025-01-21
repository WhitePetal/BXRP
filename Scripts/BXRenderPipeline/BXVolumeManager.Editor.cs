using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
namespace BXRenderPipeline
{
    public sealed partial class BXVolumeManager
    {
        public Type[] baseComponentTypeArray { get; internal set; } // internal only for tests
        public IEnumerable<Type> baseComponentTypes => baseComponentTypeArray;
        static readonly Dictionary<Type, List<(string, Type)>> s_SupportedVolumeComponentsForRenderPipeline = new();

        internal void LoadBaseTypes(Type pipelineAssetType)
        {
            // Grab all the component types we can find that are compatible with current pipeline
            using (ListPool<Type>.Get(out var list))
            {
                foreach (var t in CoreUtils.GetAllTypesDerivedFrom<BXVolumeComponment>())
                {
                    if (t.IsAbstract)
                        continue;

                    //var isSupported = SupportedOnRenderPipelineAttribute.IsTypeSupportedOnRenderPipeline(t, pipelineAssetType) ||
                    //                  IsSupportedByObsoleteVolumeComponentMenuForRenderPipeline(t, pipelineAssetType);
                    var isSupported = true;

                    if (isSupported)
                        list.Add(t);
                }

                baseComponentTypeArray = list.ToArray();
            }
        }

        public List<(string, Type)> GetVolumeComponentsForDisplay(Type currentPipelineAssetType)
        {
            if (currentPipelineAssetType == null)
                return new List<(string, Type)>();
			if (!currentPipelineAssetType.IsSubclassOf(typeof(RenderPipelineAsset)))
				throw new ArgumentException(nameof(currentPipelineAssetType));
			if (s_SupportedVolumeComponentsForRenderPipeline.TryGetValue(currentPipelineAssetType, out var supportedVolumeComponents))
                return supportedVolumeComponents;

            if (baseComponentTypeArray == null)
                LoadBaseTypes(currentPipelineAssetType);

            supportedVolumeComponents = BuildVolumeComponentDisplayList(baseComponentTypeArray);
            s_SupportedVolumeComponentsForRenderPipeline[currentPipelineAssetType] = supportedVolumeComponents;


            return supportedVolumeComponents;
        }

        List<(string, Type)> BuildVolumeComponentDisplayList(Type[] types)
        {
            if (types == null)
                throw new ArgumentNullException(nameof(types));

            var volumes = new List<(string, Type)>();
            foreach (var t in types)
            {
                string path = string.Empty;
                bool skipComponent = false;

                // Look for the attributes of this volume component and decide how is added and if it needs to be skipped
                var attrs = t.GetCustomAttributes(false);
                foreach (var attr in attrs)
                {
                    switch (attr)
                    {
                        case BXVolumeComponentMenu attrMenu:
                            {
                                path = attrMenu.menu;
                                break;
                            }
                        case HideInInspector:
                        case ObsoleteAttribute:
                            skipComponent = true;
                            break;
                    }
                }

                if (skipComponent)
                    continue;

                // If no attribute or in case something went wrong when grabbing it, fallback to a
                // beautified class name
                if (string.IsNullOrEmpty(path))
                {
#if UNITY_EDITOR
                    path = ObjectNames.NicifyVariableName(t.Name);
#else
                    path = t.Name;
#endif
                }


                volumes.Add((path, t));
            }

            return volumes
                .OrderBy(i => i.Item1)
                .ToList();
        }
    }
}
#endif
