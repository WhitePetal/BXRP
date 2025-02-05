#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace BXRenderPipeline
{
    // Everything here is only needed in editor for baking or managing baking sets
    public partial class ProbeVolumeBakingSet
    {
        internal class SceneBakeData
        {
            public bool hasProbeVolume = false;
            public bool bakeScene = true;
            public Bounds bounds = new();
        }

        [SerializeField]
        private SerializedDictionary<string, SceneBakeData> m_SceneBakeData = new();
        internal static Dictionary<string, ProbeVolumeBakingSet> sceneToBakingSet = new Dictionary<string, ProbeVolumeBakingSet>();

        /// <summary>
        /// Tries to add a scene to the baking set.
        /// </summary>
        /// <param name="guid">The GUID of the scene to add.</param>
        /// <returns>Whether the scene was successfull added to the baking set.</returns>
        public bool TryAddScene(string guid)
        {
            var sceneSet = GetBakingSetForScene(guid);
            if (sceneSet != null)
                return false;
            AddScene(guid);
            return true;
        }

        internal void AddScene(string guid, SceneBakeData bakeData = null)
        {
            m_SceneGUIDs.Add(guid);
            m_SceneBakeData.Add(guid, bakeData != null ? bakeData : new SceneBakeData());
            sceneToBakingSet[guid] = this;

            EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// Removes a scene from the baking set
        /// </summary>
        /// <param name="guid">The GUID of the scene to remove.</param>
        public void RemoveScene(string guid)
        {
            m_SceneGUIDs.Remove(guid);
            m_SceneBakeData.Remove(guid);
            sceneToBakingSet.Remove(guid);

            EditorUtility.SetDirty(this);
        }

        internal void SetScene(string guid, int index, SceneBakeData bakeData = null)
        {
            var previousSceneGUID = m_SceneGUIDs[index];
            m_SceneGUIDs[index] = guid;
            sceneToBakingSet.Remove(previousSceneGUID);
            sceneToBakingSet[guid] = this;
            m_SceneBakeData.Add(guid, bakeData != null ? bakeData : new SceneBakeData());

            EditorUtility.SetDirty(this);
        }

        internal void MoveSceneToBakingSet(string guid, int index)
        {
            var oldBakingSet = GetBakingSetForScene(guid);
            var oldBakeData = oldBakingSet.GetSceneBakeData(guid);

            if (oldBakingSet.singleSceneMode)
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(oldBakingSet));
            else
                oldBakingSet.RemoveScene(guid);

            if (index == -1)
                AddScene(guid, oldBakeData);
            else
                SetScene(guid, index, oldBakeData);
        }

        /// <summary>
        /// Changes the baking status of a scene. Objects in scenes disabled for baking will still contribute to
        /// lighting for other scenes.
        /// </summary>
        /// <param name="guid">The GUID of the scene to remove.</param>
        /// <param name="enableForBaking">Wheter or not this scene should be included when baking lighting.</param>
        public void SetSceneBaking(string guid, bool enableForBaking)
        {
            if (m_SceneBakeData.TryGetValue(guid, out var sceneData))
                sceneData.bakeScene = enableForBaking;

            EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// Changes the baking status of all scenes. Objects in scenes disabled for baking will still contribute to
        /// lighting for other scenes.
        /// </summary>
        /// <param name="enableForBaking">Wheter or not scenes should be included when baking lighting.</param>
        public void SetAllSceneBaking(bool enableForBaking)
        {
            foreach (var kvp in m_SceneBakeData)
                kvp.Value.bakeScene = enableForBaking;

            EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// Tries to add a lighting scenario to be baking set
        /// </summary>
        /// <param name="name">The name of the scenario to add.</param>
        /// <returns>Whether the scenario was successfully created.</returns>
        public bool TryAddScenario(string name)
        {
            if (m_LightingScenarios.Contains(name))
                return false;
            m_LightingScenarios.Add(name);
            EditorUtility.SetDirty(this);

            return true;
        }

        internal string CreateScenario(string name)
        {
            int index = 1;
            string renamed = name;
            while (!TryAddScenario(renamed))
                renamed = $"{name} ({index++})";

            return renamed;
        }

        /// <summary>
        /// Tries to remove a given scenario from the baking set. This will delete associated baked data.
        /// </summary>
        /// <param name="name">The name of the scenario to remove</param>
        /// <returns>Whether the scenario was successfully deleted.</returns>
        public bool RemoveScenario(string name)
        {
            if(scenarios.TryGetValue(name, out var scenarioData))
            {
                AssetDatabase.DeleteAsset(scenarioData.cellDataAsset.GetAssetPath());
                AssetDatabase.DeleteAsset(scenarioData.cellOptionalDataAsset.GetAssetPath());
                AssetDatabase.DeleteAsset(scenarioData.cellProbeOcclusionDataAsset.GetAssetPath());
                EditorUtility.SetDirty(this);
            }

            foreach(var cellData in cellDataMap.Values)
            {
                if(cellData.scenarios.TryGetValue(name, out var cellScenarioData))
                {
                    cellData.CleanupPerScenarioData(cellScenarioData);
                    cellData.scenarios.Remove(name);
                }
            }

            scenarios.Remove(name);

            EditorUtility.SetDirty(this);
            return m_LightingScenarios.Remove(name);
        }

        internal ProbeVolumeBakingSet Clone()
        {
            var newSet = CreateInstance<ProbeVolumeBakingSet>();

            // We don't want to clone everything in the set
            // Especially don't copy reference to baked data !
            newSet.probeOffset = probeOffset;
            newSet.simplificationLevels = simplificationLevels;
            newSet.minDistanceBetweenProbes = minDistanceBetweenProbes;
            newSet.renderersLayerMask = renderersLayerMask;
            newSet.minRendererVolumeSize = minRendererVolumeSize;
            newSet.skyOcclusion = skyOcclusion;
            newSet.skyOcclusionBakingSamples = skyOcclusionBakingSamples;
            newSet.skyOcclusionBakingBounces = skyOcclusionBakingBounces;
            newSet.skyOcclusionAverageAlbedo = skyOcclusionAverageAlbedo;
            newSet.skyOcclusionBackFaceCulling = skyOcclusionBackFaceCulling;
            newSet.skyOcclusionShadingDirection = skyOcclusionShadingDirection;
            newSet.useRenderingLayers = useRenderingLayers;
            newSet.renderingLayerMasks = renderingLayerMasks != null ? (ProbeLayerMask[])renderingLayerMasks.Clone() : null;

            return newSet;
        }

        /// <summary>
        /// Determines if the Probe Reference Volume Profile is equivalent to another one.
        /// </summary>
        /// <param name="otherProfile">The profile to compare with.</param>
        /// <returns>Whether the Probe Reference Volume Profile is equivalent to another one.</returns>
        public bool IsEquivalent(ProbeVolumeBakingSet otherProfile)
        {
            return minDistanceBetweenProbes == otherProfile.minDistanceBetweenProbes &&
                cellSizeInMeters == otherProfile.cellSizeInMeters &&
                simplificationLevels == otherProfile.simplificationLevels &&
                renderersLayerMask == otherProfile.renderersLayerMask;
        }

        internal void Clear()
        {
            try
            {
                AssetDatabase.StartAssetEditing();
                if(cellBricksDataAsset != null)
                {
                    DeleteAsset(cellBricksDataAsset.GetAssetPath());
                    DeleteAsset(cellSharedDataAsset.GetAssetPath());
                    DeleteAsset(cellSupportDataAsset.GetAssetPath());
                    cellBricksDataAsset = null;
                    cellSharedDataAsset = null;
                    cellSupportDataAsset = null;
                }
                foreach(var scenarioData in scenarios.Values)
                {
                    if (scenarioData.IsValid())
                    {
                        DeleteAsset(scenarioData.cellDataAsset.GetAssetPath());
                        DeleteAsset(scenarioData.cellOptionalDataAsset.GetAssetPath());
                        DeleteAsset(scenarioData.cellProbeOcclusionDataAsset.GetAssetPath());
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
                EditorUtility.SetDirty(this);
            }

            cellDescs.Clear();
            scenarios.Clear();

            // All cells should have been released through unloading the scenes first
            Debug.Assert(cellDataMap.Count == 0);

            perSceneCellLists.Clear();
            foreach (var sceneGUID in sceneGUIDs)
                perSceneCellLists.Add(sceneGUID, new List<int>());
        }

        public string RenameScenario(string scenario, string newName)
        {
            return "";
        }

        internal static ProbeVolumeBakingSet GetBakingSetForScene(string sceneGUID) => sceneToBakingSet.GetValueOrDefault(sceneGUID, null);
        internal static ProbeVolumeBakingSet GetBakingSetForScene(Scene scenne) => GetBakingSetForScene(scenne.GetGUID());

        private void DeleteAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return;

            AssetDatabase.DeleteAsset(assetPath);
        }
    }
}
#endif