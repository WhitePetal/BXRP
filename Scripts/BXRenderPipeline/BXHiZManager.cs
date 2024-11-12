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
	public class BXHiZManager : IBXHiZ
	{
		private static readonly Lazy<BXHiZManager> s_Instance = new Lazy<BXHiZManager>(() => new BXHiZManager());
        public static BXHiZManager instance = s_Instance.Value;

        private BXHiZModuleBase bXHiZ;

        public void Initialize()
        {
            bXHiZ = new BXHiZManagerComputeShader();
            bXHiZ.Initialize();
        }

        public void BeforeSRPCull(BXMainCameraRenderBase mainRender)
        {
            bXHiZ.BeforeSRPCull(mainRender);
        }
        public void AfterSRPCull()
        {
            bXHiZ.AfterSRPCull();
        }
        public void AfterSRPRender(CommandBuffer commandBuffer)
        {
            bXHiZ.AfterSRPRender(commandBuffer);
        }

        public void Dispose()
        {
            bXHiZ.Dispose();
        }

        public void Register(Renderer renderer, int instanceID)
        {
            bXHiZ.Register(renderer, instanceID);
        }
    }
}
