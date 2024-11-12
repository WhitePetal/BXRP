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
        public abstract void BeforeSRPCull(BXMainCameraRenderBase mainRender);

        public abstract void AfterSRPCull();

        public abstract void AfterSRPRender(CommandBuffer commandBuffer);

        public abstract void Dispose();

        public abstract void Initialize();

        public abstract void Register(Renderer renderer, int instanceID);
    }
}
