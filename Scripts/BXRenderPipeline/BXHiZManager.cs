using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BXRenderPipeline
{
	public class BXHiZManager : IDisposable
	{
		private static readonly Lazy<BXHiZManager> s_Instance = new Lazy<BXHiZManager>(() => new BXHiZManager());

		public static BXHiZManager instance = s_Instance.Value;

		private const string BufferName = "Hi-Z";
		private CommandBuffer commandBuffer = new CommandBuffer()
		{
			name = BufferName
		};

		public RenderTexture[] hizBuffers;

		public bool isInitialized { get; private set; }

		public int frameIndex;

		private Vector2 texSize;

		private ComputeShader cs;

		public void Initialize()
		{
			hizBuffers = new RenderTexture[3];
			frameIndex = 0;
		}

		public void Setup(BXMainCameraRenderBase mainRender)
        {
			this.cs = mainRender.commonSettings.hizCompute;
			int texW = mainRender.width >> 2;
			int texH = mainRender.height >> 3;
			texW = Mathf.IsPowerOfTwo(texW) ? texW : Mathf.NextPowerOfTwo(texW);
			texH = Mathf.IsPowerOfTwo(texH) ? texH : Mathf.NextPowerOfTwo(texH);
            if (!isInitialized || (texW != texSize.x || texH != texSize.y))
            {
				GenerateHizBuffers(texW, texH);
			}

        }

		public void Dispose()
		{
            if (isInitialized)
            {
				for(int i = 0; i < 3; ++i)
                {
					RenderTexture.ReleaseTemporary(hizBuffers[i]);
                }
				hizBuffers = null;
				isInitialized = false;
			}
			commandBuffer.Release();
		}

		private void GenerateHizBuffers(int texW, int texH)
        {
			texSize = new Vector2(texW, texH);
			for (int i = 0; i < 3; ++i)
			{
				hizBuffers[i] = RenderTexture.GetTemporary(texW, texH, 0, RenderTextureFormat.RFloat);
			}
		}
	}
}
