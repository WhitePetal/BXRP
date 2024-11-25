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
	public class BXHiZManagerPixelShader : BXHiZModuleBase
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

		private Material mat;
		private NativeArray<float> hizReadbackDatas;

		private NativeArray<CullingObjectData> cullingObjects;

		private bool isReadbacking;

		private bool isViewCamera;

		private static int[] _HiZTempMaps_ID =
		{
			Shader.PropertyToID("_HiZTempMap0"),
			Shader.PropertyToID("_HiZTempMap1"),
			Shader.PropertyToID("_HiZTempMap2"),
			Shader.PropertyToID("_HiZTempMap3"),
			Shader.PropertyToID("_HiZTempMap4"),
			Shader.PropertyToID("_HiZTempMap5"),
			Shader.PropertyToID("_HiZTempMap6"),
			Shader.PropertyToID("_HiZTempMap7"),
			Shader.PropertyToID("_HiZTempMap8"),
			Shader.PropertyToID("_HiZTempMap9"),
			Shader.PropertyToID("_HiZTempMap10"),
			Shader.PropertyToID("_HiZTempMap11"),
		};
		private static RenderTargetIdentifier[] _HiZTempMaps_TargetID =
		{
			new RenderTargetIdentifier(_HiZTempMaps_ID[0]),
			new RenderTargetIdentifier(_HiZTempMaps_ID[1]),
			new RenderTargetIdentifier(_HiZTempMaps_ID[2]),
			new RenderTargetIdentifier(_HiZTempMaps_ID[3]),
			new RenderTargetIdentifier(_HiZTempMaps_ID[4]),
			new RenderTargetIdentifier(_HiZTempMaps_ID[5]),
			new RenderTargetIdentifier(_HiZTempMaps_ID[6]),
			new RenderTargetIdentifier(_HiZTempMaps_ID[7]),
			new RenderTargetIdentifier(_HiZTempMaps_ID[8]),
			new RenderTargetIdentifier(_HiZTempMaps_ID[9]),
			new RenderTargetIdentifier(_HiZTempMaps_ID[10]),
			new RenderTargetIdentifier(_HiZTempMaps_ID[11]),
		};

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
			hizCullResultRT.enableRandomWrite = false;
			hizCullResultRT.Create();
			boundCentersTex.filterMode = FilterMode.Point;
			boundSizesTex.filterMode = FilterMode.Point;
			isReadbacking = false;
			isInitialized = true;
		}

		private void Setup(BXMainCameraRenderBase mainRender)
		{
			isViewCamera = mainRender.camera.cameraType == CameraType.SceneView;
			if (isViewCamera) return;
            this.mat = mainRender.commonSettings.hizPixelMat;
			this.projectionMatrix = GL.GetGPUProjectionMatrix(mainRender.camera.projectionMatrix * mainRender.camera.worldToCameraMatrix, false);
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
			float2 mipSize = math.float2(screenSize.x >> 1, screenSize.y >> 1);
			commandBuffer.GetTemporaryRT(_HiZTempMaps_ID[0], (int)mipSize.x, (int)mipSize.y, 0, FilterMode.Point, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear, 1);
			DrawPostProcess(commandBuffer, BXShaderPropertyIDs._EncodeDepthBuffer_TargetID, _HiZTempMaps_TargetID[0], mat, 0);
			float4 mipOffset = math.float4(0, 0, mipSize.x, mipSize.y);
			mipSizes[mipCount++] = mipOffset;
			int index = 1;
			bool row = true;
			while (mipSize.x > 1 && mipSize.y > 1)
			{
				if (mipSize.x <= 1 || mipSize.y <= 1) break;
                if (row)
                {
					mipOffset += math.float4(mipSize.x, 0, 0, 0);
				}
                else
                {
					mipOffset += math.float4(0, mipSize.y, 0, 0);
                }
				row = !row;
				mipSize *= 0.5f;
				commandBuffer.GetTemporaryRT(_HiZTempMaps_ID[index], Mathf.CeilToInt(mipSize.x), Mathf.CeilToInt(mipSize.y), 0, FilterMode.Point, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear, 1);
				DrawPostProcess(commandBuffer, _HiZTempMaps_TargetID[index-1], _HiZTempMaps_TargetID[index], mat, 1);
				mipSizes[mipCount++] = math.float4(mipOffset.xy, mipSize.xy);
				++index;
			}
			commandBuffer.SetRenderTarget(hizMap, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
			commandBuffer.ClearRenderTarget(false, true, Color.clear);
			Vector2 baseOffset = mipSizes[2];
			for (int i = 2; i < mipCount; ++i)
            {
				float4 mipPort = mipSizes[i];
				Vector2 offset = mipPort.xy;
				offset = offset - baseOffset;
				mipSizes[i - 2] = new Vector4(offset.x, offset.y, mipPort.z, mipPort.w);
				commandBuffer.SetViewport(new Rect(offset, mipPort.zw));
				commandBuffer.SetGlobalTexture(BXShaderPropertyIDs._PostProcessInput_ID, _HiZTempMaps_TargetID[i]);
				commandBuffer.DrawProcedural(Matrix4x4.identity, mat, 2, MeshTopology.Triangles, 3);
			}
			for(int i = 0; i < mipCount; ++i)
            {
				commandBuffer.ReleaseTemporaryRT(_HiZTempMaps_ID[i]);
			}

			if (!isReadbacking && cullingObjectCount > 0)
			{
				commandBuffer.SetRenderTarget(hizCullResultRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
				commandBuffer.ClearRenderTarget(false, true, Color.clear);
				commandBuffer.SetGlobalTexture(BaseShaderProperties._BoundCentersTex_ID, boundCentersTex);
				commandBuffer.SetGlobalTexture(BaseShaderProperties._BoundSizesTex_ID, boundSizesTex);
				commandBuffer.SetGlobalVector(BaseShaderProperties._HizTexSize_ID, new Vector4(texSize.x, texSize.y));
				commandBuffer.SetGlobalTexture(BaseShaderProperties._HizMapInput_ID, hizMap);
				commandBuffer.SetGlobalVector(BaseShaderProperties._HizParams_ID, new Vector4(screenSize.x, screenSize.y, mipCount - 3, cullingObjectCount));
				commandBuffer.SetGlobalMatrix(BaseShaderProperties._HizProjectionMatrix_ID, projectionMatrix);
				commandBuffer.SetGlobalVectorArray(BaseShaderProperties._HizMipSize_ID, mipSizes);
				commandBuffer.DrawProcedural(Matrix4x4.identity, mat, 3, MeshTopology.Triangles, 3);


				hizReadbackDatas = new NativeArray<float>(dataTexWidth * dataTexHeight, Allocator.Persistent);
				commandBuffer.RequestAsyncReadbackIntoNativeArray(ref hizReadbackDatas, hizCullResultRT, ReadBack);
				isReadbacking = true;
			}
			commandBuffer.EndSample(SampleName);
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

		private void ApplyCollectDatas()
        {
			boundCentersTex.Apply();
			boundSizesTex.Apply();
		}

		public override void Dispose()
		{
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
			boundCentersTex = null;
			boundSizesTex = null;
			hizCullResultRT = null;
			hizMap = null;
			rendererDic = null;
			isInitialized = false;
		}

		private void DrawPostProcess(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, Material mat, int pass, bool clear = false)
		{
			cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
			cmd.SetGlobalTexture(BXShaderPropertyIDs._PostProcessInput_ID, source);
			if (clear)
			{
				cmd.ClearRenderTarget(false, true, Color.clear);
			}
			cmd.DrawProcedural(Matrix4x4.identity, mat, pass, MeshTopology.Triangles, 3);
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
			hizMap.enableRandomWrite = false;
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
    }
}
