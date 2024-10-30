using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXRenderPipeline
{
    [System.Serializable]
    public abstract class BXVolumeComponment : ScriptableObject
    {
        public abstract void OverrideData<T>(T volume, float interpFactor) where T : BXVolumeComponment;

        public abstract void Render();
    }
}
