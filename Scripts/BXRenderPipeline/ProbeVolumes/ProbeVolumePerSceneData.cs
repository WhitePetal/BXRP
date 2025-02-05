using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using System;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BXRenderPipeline
{
    /// <summary>
    /// A component that stores baked probe volume state and data references. Normally hidden in the hierarchy.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("")] // Hide
    public class ProbeVolumePerSceneData : MonoBehaviour
    {
        /// <summary>
        /// The baking set this scene is part of.
        /// </summary>
        public ProbeVolumeBakingSet bakingSet => serializedBakingSet;

        // Warning: this is the baking set this scene was part of during last bake
        // It shouldn't be used while baking as the scene may have been moved since then
        [SerializeField, FormerlySerializedAs("bakingSet")]
        internal ProbeVolumeBakingSet serializedBakingSet;
        [SerializeField]
        internal string sceneGUID = "";
        
        // All code bellow is only kept in order to be able to cleanup obsolete data.
        [Serializable]
        internal struct ObsoletePerScenarioData
        {
            public int sceneHash;
            public TextAsset cellDataAsset; // Contains L0 L1 SH data
            public TextAsset cellOptionalDataAsset; // Contains L2 SH data
        }

        [Serializable]
        struct ObsoleteSerializablePerScenarioDataItem
        {
#pragma warning disable 649 // is never assigned to, and will always have its default value
            public string scenario;
            public ObsoletePerScenarioData data;
#pragma warning restore 649
        }

        [SerializeField, FormerlySerializedAs("asset")]
        internal ObsoleteProbeVolumeAsset obsoleteAsset;
        [SerializeField, FormerlySerializedAs("cellSharedDataAsset")]
        internal TextAsset obsoleteCellSharedDataAsset; // Contains bricks and validity data
        [SerializeField, FormerlySerializedAs("cellSupportDataAsset")]
        internal TextAsset obsoleteCellSupportDataAsset; // Contains debug data
        [SerializeField, FormerlySerializedAs("serializedScenarios")]
        List<ObsoleteSerializablePerScenarioDataItem> obsoleteSerializedScenarios = new();

#if UNITY_EDITOR
        void DeleteAsset(Object asset)
        {
            if (asset != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long instanceID))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                AssetDatabase.DeleteAsset(assetPath);
            }
        }
#endif

        internal void Clear()
        {
            QueueSceneRemoval();
            serializedBakingSet = null;

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        internal void QueueSceneLoading()
        {
            if (serializedBakingSet == null)
                return;

#if UNITY_EDITOR
            // Check if we are trying to load APV data for a scene which has not enabled APV (or it was removed)
            var bakedData = serializedBakingSet.GetSceneBakeData(sceneGUID);
            if (bakedData != null && bakedData.hasProbeVolume == false)
                return;
#endif

            var refVol = ProbeReferenceVolume.instance;
            refVol.AddPendingSceneLoading(sceneGUID, serializedBakingSet);
        }
    }
}
