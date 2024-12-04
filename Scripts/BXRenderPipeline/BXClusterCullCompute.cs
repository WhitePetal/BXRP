using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace BXRenderPipeline
{
    public class BXClusterCullCompute : BXClusterCullBase
    {
        public override void Dispose()
        {
            throw new NotImplementedException();
        }

        public override void Upload(CommandBuffer commandBuffer)
        {
            throw new NotImplementedException();
        }

        public override void Setup(Camera camera, BXLightsBase lights, int width, int height)
        {
            throw new NotImplementedException();
        }
    }
}
