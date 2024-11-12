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
	public interface IBXHiZ : IDisposable
	{
        public void BeforeSRPCull(BXMainCameraRenderBase mainRender);
        public void AfterSRPCull();
        public void AfterSRPRender(CommandBuffer commandBuffer);
        public void Register(Renderer renderer, int instanceID);
    }
}
