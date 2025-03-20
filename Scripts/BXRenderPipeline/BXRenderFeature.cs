using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BXRenderPipeline
{
    public enum RenderFeatureStep
	{
        BeforeRender = 0,
        OnDirShadows = 1,
        BeforeOpaque = 2,
        AfterOpaque = 3,
        BeforeTransparent = 4,
        AfterTransparent = 5,
        OnPostProcess = 6,
        MAX = 7
	}
}
