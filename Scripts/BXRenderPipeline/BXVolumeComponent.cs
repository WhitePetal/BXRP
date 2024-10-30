using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXRenderPipeline
{
    [System.Serializable]
    public abstract class BXVolumeComponment : ScriptableObject
    {
        public abstract void OverrideData(BXVolumeComponment component, float interpFactor);

		public abstract BXVolumeComponment CopyCreate();

        public abstract void Render();
    }
}
