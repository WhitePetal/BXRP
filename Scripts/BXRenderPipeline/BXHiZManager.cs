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

		public RenderTexture hizBuffer0;
		public RenderTexture hizBuffer1;
		public RenderTexture hizBuffer2;

		public bool isInitialized { get; private set; }

		public int frameIndex;

		public void Initialize()
		{
			Debug.Assert(!isInitialized);

			isInitialized = true;

			frameIndex = 0;
		}

		public void Dispose()
		{
			commandBuffer.Release();
		}
	}
}
