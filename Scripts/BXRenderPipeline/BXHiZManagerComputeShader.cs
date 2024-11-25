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
	public class BXHiZManagerComputeShader : BXHiZModuleBase
	{
		private static Dictionary<int, Renderer> rendererDic;

		private struct CullingObjectData
		{
			[ReadOnly]
			public float3 boundCenter;
			[ReadOnly]
			public float3 boundSize;
			[ReadOnly]
			public int instanceID;

			public Renderer renderer => rendererDic[instanceID];
		};

		public bool isInitialized { get; private set; }

		private const int dataTexWidth = 64;
		private const int dataTexHeight = 64;

		private Texture2D boundCentersTex;
		private Texture2D boundSizesTex;
		private RenderTexture hizCullResultRT;
		private RenderTexture hizMap;

		private int2 screenSize;
		private int2 texSize;
		private Vector4[] mipSizes; // x,y offset, z,w mip size
		private Matrix4x4 projectionMatrix;

        private int mipCount;
		private int cullingObjectCount;

		private ComputeShader cs;
		private NativeArray<float> hizReadbackDatas;

		private NativeArray<CullingObjectData> cullingObjects;

		private bool isReadbacking;

		private bool isViewCamera;

		public override void Initialize()
		{
			if (isInitialized) return;
			rendererDic = new Dictionary<int, Renderer>(2048);
			texSize = math.int2(0, 0);
			cullingObjects = new NativeArray<CullingObjectData>(dataTexWidth * dataTexHeight, Allocator.Persistent);
			mipSizes = new Vector4[16];
			boundCentersTex = new Texture2D(dataTexWidth, dataTexHeight, TextureFormat.RGBAFloat, 0, true);
			boundSizesTex = new Texture2D(dataTexWidth, dataTexHeight, TextureFormat.RGBAFloat, 0, true);
			RenderTextureDescriptor rtd = new RenderTextureDescriptor(dataTexWidth, dataTexHeight, UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat, 0, 0);
			rtd.sRGB = false;
			hizCullResultRT = new RenderTexture(rtd);
			hizCullResultRT.hideFlags = HideFlags.DontSave;
			hizCullResultRT.enableRandomWrite = true;
			hizCullResultRT.filterMode = FilterMode.Point;
			hizCullResultRT.Create();
			boundCentersTex.filterMode = FilterMode.Point;
			boundSizesTex.filterMode = FilterMode.Point;
			isReadbacking = false;
			isInitialized = true;
			boundCentersTex.hideFlags = HideFlags.DontSave;
			boundSizesTex.hideFlags = HideFlags.DontSave;
			GameObject.DontDestroyOnLoad(boundCentersTex);
			GameObject.DontDestroyOnLoad(boundSizesTex);
			GameObject.DontDestroyOnLoad(hizCullResultRT);
		}

		private void Setup(BXMainCameraRenderBase mainRender)
		{
			isViewCamera = mainRender.camera.cameraType == CameraType.SceneView;
			if (isViewCamera) return;
            this.cs = mainRender.commonSettings.hizCompute;
			this.projectionMatrix = GL.GetGPUProjectionMatrix(mainRender.camera.projectionMatrix, false) * mainRender.camera.worldToCameraMatrix;
			mipCount = 0;
			this.screenSize = math.int2(mainRender.width, mainRender.height);
			int texW = ((mainRender.width >> 3) + (mainRender.width >> 4)) + 1;
			int texH = mainRender.height >> 3;
			//texW = Mathf.IsPowerOfTwo(texW) ? texW : Mathf.NextPowerOfTwo(texW);
			//texH = Mathf.IsPowerOfTwo(texH) ? texH : Mathf.NextPowerOfTwo(texH);
			if (texW != texSize.x || texH != texSize.y)
			{
				GenerateHizBuffers(texW, texH);
				texSize = math.int2(texW, texH);
			}
            if (!isReadbacking)
            {
				cullingObjectCount = 0;
            }
        }

		private void Render(CommandBuffer commandBuffer)
		{
			if (isViewCamera) return;
			commandBuffer.BeginSample(SampleName);
            commandBuffer.SetComputeTextureParam(cs, 0, BXShaderPropertyIDs._EncodeDepthBuffer_ID, BXShaderPropertyIDs._EncodeDepthBuffer_TargetID);
			commandBuffer.SetComputeTextureParam(cs, 0, BaseShaderProperties._HiZMap_ID, hizMap);
			commandBuffer.DispatchCompute(cs, 0, Mathf.CeilToInt(screenSize.x / 8), Mathf.CeilToInt(screenSize.y / 8), 1);

			float2 mipSize = math.float2(screenSize.x >> 3, texSize.y);
			float4 mipOffset = math.float4(0, 0, mipSize.x, mipSize.y);
			mipSizes[mipCount++] = mipOffset;
			while (mipSize.x > 1 && mipSize.y > 1)
			{
				commandBuffer.SetComputeVectorParam(cs, BaseShaderProperties._MipOffset_ID, mipOffset);
				commandBuffer.SetComputeTextureParam(cs, 1, BaseShaderProperties._HiZMap_ID, hizMap);
				commandBuffer.DispatchCompute(cs, 1, Mathf.CeilToInt(mipSize.x / 8f), Mathf.CeilToInt(mipSize.y / 8f), 1);
				mipOffset += math.float4(mipSize.x, 0, 0, 0);
				mipSize *= 0.5f;
				mipSizes[mipCount++] = math.float4(mipOffset.xy, mipSize.xy);
				if (mipSize.x <= 1 || mipSize.y <= 1) break;
				mipOffset += math.float4(0, mipSize.y, 0, 0);
				mipSize *= 0.5f;
				mipSizes[mipCount++] = math.float4(mipOffset.xy, mipSize.xy);
				if (mipSize.x <= 1 || mipSize.y <= 1) break;
				mipOffset += math.float4(mipSize.x, 0, 0, 0);
				mipSize *= 0.5f;
				mipOffset.z = mipSize.x;
				mipOffset.w = mipSize.y;
				mipSizes[mipCount++] = math.float4(mipOffset.xy, mipSize.xy);
			}

			if (!isReadbacking && cullingObjectCount > 0)
			{
				commandBuffer.SetComputeTextureParam(cs, 2, BaseShaderProperties._BoundCentersTex_ID, boundCentersTex);
				commandBuffer.SetComputeTextureParam(cs, 2, BaseShaderProperties._BoundSizesTex_ID, boundSizesTex);
				commandBuffer.SetComputeTextureParam(cs, 2, BaseShaderProperties._HizMapInput_ID, hizMap);
				commandBuffer.SetComputeVectorParam(cs, BaseShaderProperties._HizParams_ID, new Vector4(screenSize.x, screenSize.y, mipCount - 1, cullingObjectCount));
				commandBuffer.SetComputeVectorParam(cs, BaseShaderProperties._HizTexSize_ID, new Vector4(texSize.x, texSize.y));
				commandBuffer.SetComputeMatrixParam(cs, BaseShaderProperties._HizProjectionMatrix_ID, projectionMatrix);
				commandBuffer.SetComputeVectorArrayParam(cs, BaseShaderProperties._HizMipSize_ID, mipSizes);
				commandBuffer.SetComputeTextureParam(cs, 2, BaseShaderProperties._HizResultRT_ID, hizCullResultRT);
				float xgroup = cullingObjectCount / 8f;
				float ygroup = xgroup / 8f;
				commandBuffer.DispatchCompute(cs, 2, 8, 8, 1);

				hizReadbackDatas = new NativeArray<float>(dataTexWidth * dataTexHeight, Allocator.Persistent);
				commandBuffer.RequestAsyncReadbackIntoNativeArray(ref hizReadbackDatas, hizCullResultRT, ReadBack);
				isReadbacking = true;
			}
			commandBuffer.EndSample(SampleName);
		}

		private void ApplyCollectDatas()
        {
			if (isViewCamera || isReadbacking) return;
			boundCentersTex.Apply();
			boundSizesTex.Apply();
		}

		public override void Dispose()
		{
			if (!isInitialized) return;
			if (isReadbacking)
			{
				AsyncGPUReadback.WaitAllRequests();
				isReadbacking = false;
			}

			cullingObjects.Dispose();
			hizCullResultRT.Release();
			hizMap.Release();
			rendererDic.Clear();
			mipSizes = null;
			GameObject.DestroyImmediate(hizCullResultRT);
			GameObject.DestroyImmediate(boundCentersTex);
			GameObject.DestroyImmediate(boundSizesTex);
			boundCentersTex = null;
			boundSizesTex = null;
			hizCullResultRT = null;
			hizMap = null;
			rendererDic = null;
			isInitialized = false;
		}

		private void GenerateHizBuffers(int texW, int texH)
		{
			texSize = math.int2(texW, texH);
			if (hizMap != null && hizMap.IsCreated())
			{
				hizMap.Release();
			}
			//Vector2 mipSize = new Vector2(texW >> 1, texH);
			//while(mipSize.x > 1)
			RenderTextureDescriptor rd = new RenderTextureDescriptor(texW, texH, UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat, 0);
			rd.sRGB = false;
			hizMap = new RenderTexture(rd);
			hizMap.filterMode = FilterMode.Point;
			hizMap.enableRandomWrite = true;
			hizMap.Create();
		}

		private void ReadBack(AsyncGPUReadbackRequest obj)
		{
			if (obj.done)
			{
				if (obj.hasError)
				{
					hizReadbackDatas.Dispose();
					throw new Exception("Hi-Z Readback Error");
				}

				for (int i = 0; i < cullingObjectCount; ++i)
				{
					if (hizReadbackDatas[i] <= 0f)
						cullingObjects[i].renderer.renderingLayerMask = 0;
					else
						cullingObjects[i].renderer.renderingLayerMask = 1;
				}
				hizReadbackDatas.Dispose();
				isReadbacking = false;
			}
		}

        public override void BeforeSRPCull(BXMainCameraRenderBase mainRender)
        {
			Setup(mainRender);
        }

        public override void AfterSRPCull()
        {
			ApplyCollectDatas();
        }

        public override void AfterSRPRender(CommandBuffer commandBuffer)
        {
			Render(commandBuffer);
        }

		public override void Register(Renderer renderer, int instanceID)
		{
			if (isViewCamera || isReadbacking) return;
			rendererDic[instanceID] = renderer;
			var bounds = renderer.bounds;
			Vector3 center = bounds.center;
			Vector3 size = bounds.size;
			CullingObjectData data = new CullingObjectData()
			{
				boundCenter = center,
				boundSize = size,
				instanceID = instanceID
			};
			int y = cullingObjectCount / 64;
			int x = cullingObjectCount % 64;
			boundCentersTex.SetPixel(x, y, new Color(center.x, center.y, center.z, 1f));
			boundSizesTex.SetPixel(x, y, new Color(size.x, size.y, size.z, 1f));
			cullingObjects[cullingObjectCount++] = data;
		}
	}
}
