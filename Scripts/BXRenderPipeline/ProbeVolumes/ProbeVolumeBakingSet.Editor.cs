#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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
    }
}
#endif