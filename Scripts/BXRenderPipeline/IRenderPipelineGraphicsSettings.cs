using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXRenderPipeline
{
    public interface IRenderPipelineGraphicsSettings
    {
        bool isAvailableInPlayerBuild => false;

        void Reset() { }
    }
}
