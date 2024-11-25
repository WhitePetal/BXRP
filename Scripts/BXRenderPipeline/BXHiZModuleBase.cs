using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;
using Unity.Burst;

namespace BXRenderPipeline
{
	public abstract class BXHiZModuleBase : IBXHiZ
	{
        public static class BaseShaderProperties
        {
            public static readonly int _HiZMap_ID = Shader.PropertyToID("_HizMap");
            public static readonly RenderTargetIdentifier _HiZMap_TargetID = new RenderTargetIdentifier(_HiZMap_ID);
            public static readonly int _MipOffset_ID = Shader.PropertyToID("_MipOffset");

            public static readonly int _BoundCentersTex_ID = Shader.PropertyToID("_BoundCentersTex");
            public static readonly int _BoundSizesTex_ID = Shader.PropertyToID("_BoundSizesTex");
            public static readonly int _HizMapInput_ID = Shader.PropertyToID("_HizMapInput");
            public static readonly int _HizParams_ID = Shader.PropertyToID("_HizParams");
            public static readonly int _HizTexSize_ID = Shader.PropertyToID("_HizTexSize");
            public static readonly int _HizProjectionMatrix_ID = Shader.PropertyToID("_HizProjectionMatrix");
            public static readonly int _HizMipSize_ID = Shader.PropertyToID("_HizMipSize");
            public static readonly int _HizResultRT_ID = Shader.PropertyToID("_HizResultRT");
        }

        protected const string SampleName = "Hi-Z";

        public abstract void BeforeSRPCull(BXMainCameraRenderBase mainRender);

        public abstract void AfterSRPCull();

        public abstract void AfterSRPRender(CommandBuffer commandBuffer);

        public abstract void Dispose();

        public abstract void Initialize();

        public abstract void Register(Renderer renderer, int instanceID);
    }
}
