using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXRenderPipeline
{
    public partial class ProbeVolume : MonoBehaviour
    {
        enum Version
        {
            Initial,
            LocalMode,
            InvertOverrideLevels,

            Count
        }

        [SerializeField]
        private Version version = Version.Initial;

        /// <summary>
        /// If is a global bolume
        /// </summary>
        [SerializeField, Obsolete("Use mode instead")]
        public bool globalVolume = false;

        private void Awake()
        {
            if (version == Version.Count)
                return;

            if(version == Version.LocalMode - 1)
            {
#pragma warning disable 618 // Type or member is obsolete
                mode = globalVolume ? Mode.Scene : Mode.Local;
#pragma warning restore 618

                version++;
            }
            if(version == Version.InvertOverrideLevels - 1)
            {
#if UNITY_EDITOR
                ProbeVolumeBakingSet.SyncBakingSets();
                var bakingSet = ProbeVolumeBakingSet.GetBakingSetForScene(gameObject.scene);
                if(bakingSet != null)
                {
                    int maxSubdiv = bakingSet.simplificationLevels;
                    int tmpLowest = lowestSubdivLevelOverride;
                    lowestSubdivLevelOverride = Mathf.Clamp(maxSubdiv - highestSubdivLevelOverride, 0, 5);
                    highestSubdivLevelOverride = Mathf.Clamp(maxSubdiv - tmpLowest, 0, 5);
                }
#endif

                version++;
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}
