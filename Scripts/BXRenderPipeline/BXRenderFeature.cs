using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BXRenderPipeline
{
    public enum RenderFeatureStep
	{
        BeforeRender,
        OnDirShadows,
        BeforeOpaque,
        AfterOpaque,
        BeforeTransparent,
        AfterTransparent,
        OnPostProcess
	}

    public abstract class BXRenderFeature : ScriptableObject
    {
        public bool isDynamic;
        public RenderFeatureStep step;
        public abstract void Init(BXRenderCommonSettings commonSettings);
        public abstract void Setup(BXMainCameraRender render);
        public abstract void Render(CommandBuffer cmd, BXMainCameraRender render);
        public abstract void Dispose();
    }
}
