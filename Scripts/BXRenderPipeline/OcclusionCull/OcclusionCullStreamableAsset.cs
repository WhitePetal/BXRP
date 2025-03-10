using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace BXRenderPipeline.OcclusionCulling
{
    [Serializable]
    public class OcclusionCullStreamableAsset
    {
#if UNITY_EDITOR
        [SerializeField]
        [FormerlySerializedAs("assetGUID")]
        private string m_AssetGUID = ""; // In the editor, allows us to load the asset through the AssetDatabase

        [SerializeField]
        private TextAsset m_Asset;
#endif
    }
}
