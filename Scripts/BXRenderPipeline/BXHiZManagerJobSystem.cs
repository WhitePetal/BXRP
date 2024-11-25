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
	public class BXHiZManagerJobSystem : BXHiZModuleBase
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

			public uint renderLayerMask;

			public Renderer renderer => rendererDic[instanceID];
		};

		public RenderTexture[] hizBuffers;

		public bool isInitialized { get; private set; }

		public int frameIndex;

		private NativeArray<int2> screenSizes;
		private NativeArray<int2> texSizes;
		private NativeArray<float4>[] mipSizeses; // x,y offset, z,w mip size

		private NativeArray<Matrix4x4> projectionMatrixs;

		private NativeArray<int> mipCounts;
		private NativeArray<int> cullingObjectCounts;

		private ComputeShader cs;
		private NativeArray<float>[] hizReadbackDatas;

		private NativeArray<CullingObjectData>[] cullingObjects;

		private Action<AsyncGPUReadbackRequest>[] readBackActions;

		private bool[] isReadbacking;
		private bool[] isDone;
		private int[] readBackStartTime;

		private int willReadBackIndex;
		private int lastReadBackStartTime;
		private int compeleteReadBackCount;
		private int validReadBackIndex;

		private bool isViewCamera;

		public override void Initialize()
		{
			if (isInitialized) return;
			rendererDic = new Dictionary<int, Renderer>(2048);
			hizBuffers = new RenderTexture[3];
			isReadbacking = new bool[3];
			isDone = new bool[3];
			readBackStartTime = new int[3];
			hizReadbackDatas = new NativeArray<float>[3];
			readBackActions = new Action<AsyncGPUReadbackRequest>[]
			{
				ReadBack0, ReadBack1, ReadBack2
			};
			mipSizeses = new NativeArray<float4>[3];
			cullingObjects = new NativeArray<CullingObjectData>[3];
			projectionMatrixs = new NativeArray<Matrix4x4>(3, Allocator.Persistent);
			screenSizes = new NativeArray<int2>(3, Allocator.Persistent);
			texSizes = new NativeArray<int2>(3, Allocator.Persistent);
			mipCounts = new NativeArray<int>(3, Allocator.Persistent);
			cullingObjectCounts = new NativeArray<int>(3, Allocator.Persistent);
			for (int i = 0; i < 3; ++i)
			{
				isDone[i] = true;
				mipSizeses[i] = new NativeArray<float4>(16, Allocator.Persistent);
				cullingObjects[i] = new NativeArray<CullingObjectData>(2048, Allocator.Persistent);

				texSizes[i] = math.int2(0, 0);
			}
			frameIndex = 0;
			willReadBackIndex = 0;
			validReadBackIndex = -1;
			lastReadBackStartTime = 0;
			compeleteReadBackCount = 0;
		}

		private void Setup(BXMainCameraRenderBase mainRender)
		{
			if (isViewCamera) return;
			if (!isReadbacking[willReadBackIndex])
			{
				this.cs = mainRender.commonSettings.hizCompute;
				mipCounts[willReadBackIndex] = 0;
				cullingObjectCounts[willReadBackIndex] = 0;

				this.projectionMatrixs[willReadBackIndex] = GL.GetGPUProjectionMatrix(mainRender.camera.projectionMatrix, false) * mainRender.camera.worldToCameraMatrix;
				this.screenSizes[willReadBackIndex] = math.int2(mainRender.width, mainRender.height);
				int texW = ((mainRender.width >> 3) + (mainRender.width >> 4)) + 1;
				int texH = mainRender.height >> 3;
				//texW = Mathf.IsPowerOfTwo(texW) ? texW : Mathf.NextPowerOfTwo(texW);
				//texH = Mathf.IsPowerOfTwo(texH) ? texH : Mathf.NextPowerOfTwo(texH);
				if (!isInitialized || (texW != texSizes[willReadBackIndex].x || texH != texSizes[willReadBackIndex].y))
				{
					GenerateHizBuffers(texW, texH, willReadBackIndex);
					isInitialized = true;
				}
			}
		}

		private void Render(CommandBuffer commandBuffer)
		{
			if (isViewCamera) return;
			commandBuffer.BeginSample(SampleName);
			if (!isReadbacking[willReadBackIndex])
			{
				commandBuffer.SetComputeTextureParam(cs, 0, BXShaderPropertyIDs._EncodeDepthBuffer_ID, BXShaderPropertyIDs._EncodeDepthBuffer_TargetID);
				commandBuffer.SetComputeTextureParam(cs, 0, BaseShaderProperties._HiZMap_ID, hizBuffers[willReadBackIndex]);
				int2 screenSize = screenSizes[willReadBackIndex];
				commandBuffer.DispatchCompute(cs, 0, Mathf.CeilToInt(screenSize.x / 8), Mathf.CeilToInt(screenSize.y / 8), 1);

				int2 texSize = texSizes[willReadBackIndex];
				float2 mipSize = math.float2(screenSize.x >> 3, texSize.y);
				float4 mipOffset = math.float4(0, 0, mipSize.x, mipSize.y);
				ref var mipSizes = ref mipSizeses[willReadBackIndex];
				ref int mipCount = ref mipCounts.UnsafeElementAtMutable(willReadBackIndex);
				mipSizes[mipCount++] = mipOffset;
				while (mipSize.x > 1 && mipSize.y > 1)
				{
					commandBuffer.SetComputeVectorParam(cs, BaseShaderProperties._MipOffset_ID, mipOffset);
					commandBuffer.SetComputeTextureParam(cs, 1, BaseShaderProperties._HiZMap_ID, hizBuffers[willReadBackIndex]);
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

				hizReadbackDatas[willReadBackIndex] = new NativeArray<float>(texSize.x * texSize.y, Allocator.Persistent);
				isDone[willReadBackIndex] = false;
				commandBuffer.RequestAsyncReadbackIntoNativeArray<float>(ref hizReadbackDatas[willReadBackIndex], hizBuffers[willReadBackIndex], readBackActions[willReadBackIndex]);
				isReadbacking[willReadBackIndex] = true;
				readBackStartTime[willReadBackIndex] = Time.frameCount;
				willReadBackIndex = willReadBackIndex == 2 ? 0 : (willReadBackIndex + 1);
			}
			commandBuffer.EndSample(SampleName);
		}

		public override void Register(Renderer renderer, int instanceID)
		{
			if (isViewCamera || isReadbacking[willReadBackIndex]) return;
			rendererDic[instanceID] = renderer;
			CullingObjectData data = new CullingObjectData()
			{
				boundCenter = renderer.bounds.center,
				boundSize = renderer.bounds.size,
				instanceID = instanceID
			};
			ref var cullingObjectCount = ref cullingObjectCounts.UnsafeElementAtMutable(willReadBackIndex);
			ref var cullingObject = ref cullingObjects[willReadBackIndex];
			cullingObject[cullingObjectCount++] = data;
		}

		private void CompleteCull(BXMainCameraRenderBase mainRender)
		{
			isViewCamera = mainRender.camera.cameraType == CameraType.SceneView;
			if (isViewCamera || validReadBackIndex == -1 || isReadbacking[validReadBackIndex] || isDone[validReadBackIndex]) return;
			HizCullJob cullJob = new HizCullJob()
			{
				hizData = hizReadbackDatas[validReadBackIndex],
				cullingObjectDatas = cullingObjects[validReadBackIndex],
				projectionMatrix = projectionMatrixs[validReadBackIndex],
				screenSize = screenSizes[validReadBackIndex],
				texSize = texSizes[validReadBackIndex],
				mipCount = mipCounts[validReadBackIndex],
				mipSizes = mipSizeses[validReadBackIndex]
			};
			int count = cullingObjectCounts[validReadBackIndex];
			JobHandle handle = cullJob.Schedule(count, 8);
			handle.Complete();
			hizReadbackDatas[validReadBackIndex].Dispose();
			for(int i = 0; i < count; ++i)
            {
				ref var data = ref cullingObjects[validReadBackIndex].UnsafeElementAt(i);
				data.renderer.renderingLayerMask = data.renderLayerMask;
            }
			--compeleteReadBackCount;
			isDone[validReadBackIndex] = true;
		}

		public override void Dispose()
		{
			for(int i = 0; i < 3; ++i)
            {
				if (isReadbacking[i])
				{
					AsyncGPUReadback.WaitAllRequests();
					isReadbacking[i] = false;
				}
			}
			if (isInitialized)
            {
				for(int i = 0; i < 3; ++i)
                {
					hizBuffers[i].Release();
					if (hizReadbackDatas[i].IsCreated) hizReadbackDatas[i].Dispose();
					mipSizeses[i].Dispose();
					cullingObjects[i].Dispose();
				}
				screenSizes.Dispose();
				texSizes.Dispose();
				projectionMatrixs.Dispose();
				mipCounts.Dispose();
				cullingObjectCounts.Dispose();
				mipSizeses = null;
				cullingObjects = null;
				hizBuffers = null;
				isInitialized = false;
			}
			rendererDic.Clear();
			rendererDic = null;
			isReadbacking = null;
			isDone = null;
			hizReadbackDatas = null;
			readBackActions = null;
		}

		private void GenerateHizBuffers(int texW, int texH, int index)
        {
			texSizes[index] = math.int2(texW, texH);
			if(hizBuffers[index] != null && hizBuffers[index].IsCreated())
            {
				hizBuffers[index].Release();
            }
			//Vector2 mipSize = new Vector2(texW >> 1, texH);
			//while(mipSize.x > 1)
			RenderTextureDescriptor rd = new RenderTextureDescriptor(texW, texH, UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat, 0);
			rd.sRGB = false;
			hizBuffers[index] = new RenderTexture(rd);
			hizBuffers[index].filterMode = FilterMode.Point;
			hizBuffers[index].enableRandomWrite = true;
			hizBuffers[index].Create();
		}

		private void ReadBack0(AsyncGPUReadbackRequest obj)
		{
			if (obj.done)
			{
				if (obj.hasError)
				{
					hizReadbackDatas[0].Dispose();
					throw new Exception("Hi-Z Readback Error");
				}
				Cull(0);
			}
		}
		private void ReadBack1(AsyncGPUReadbackRequest obj)
		{
			if (obj.done)
			{
				if (obj.hasError)
				{
					hizReadbackDatas[1].Dispose();
					throw new Exception("Hi-Z Readback Error");
				}
				Cull(1);
			}
		}
		private void ReadBack2(AsyncGPUReadbackRequest obj)
		{
			if (obj.done)
			{
				if (obj.hasError)
				{
					hizReadbackDatas[2].Dispose();
					throw new Exception("Hi-Z Readback Error");
				}
				Cull(2);
			}
		}

		//[BurstCompile]
        private struct HizCullJob : IJobParallelFor
        {
			public NativeArray<CullingObjectData> cullingObjectDatas;
			[ReadOnly]
			public NativeArray<float> hizData;
			[ReadOnly]
			public Matrix4x4 projectionMatrix;
			[ReadOnly]
			public int2 screenSize;
			[ReadOnly]
			public int mipCount;
			[ReadOnly]
			public NativeArray<float4> mipSizes;
			[ReadOnly]
			public int2 texSize;

			private float3 GetNDCPos(float3 pos)
			{
				float4 p = projectionMatrix * math.float4(pos.x, pos.y, pos.z, 1f);
				p.xyz /= p.w;
				p.xy = p.xy * 0.5f + 0.5f;
				//p.y = 1.0f - p.y;
				return p.xyz;
			}

			public void Execute(int i)
            {
                unsafe
                {
					ref var data = ref UnsafeUtility.ArrayElementAsRef<CullingObjectData>(cullingObjectDatas.GetUnsafePtr(), i);
					float3 center = data.boundCenter;
					float3 size = data.boundSize;

					float3 p0 = center + size;
					float3 p1 = center - size;
					float3 p2 = math.float3(p0.xy, p1.z);     // + + -
					float3 p3 = math.float3(p0.x, p1.yz);     // + - -
					float3 p4 = math.float3(p1.xy, p0.z);     // - - +
					float3 p5 = math.float3(p1.x, p0.y, p1.z);// - + -
					float3 p6 = math.float3(p0.x, p1.y, p0.z);// + - +
					float3 p7 = math.float3(p1.x, p0.yz);     // - + +

					p0 = GetNDCPos(p0);
					p1 = GetNDCPos(p1);
					p2 = GetNDCPos(p2);
					p3 = GetNDCPos(p3);
					p4 = GetNDCPos(p4);
					p5 = GetNDCPos(p5);
					p6 = GetNDCPos(p6);
					p7 = GetNDCPos(p7);

					float3 aabbMin = math.min(p0, math.min(p1, math.min(p2, math.min(p3, math.min(p4, math.min(p5, math.min(p6, p7)))))));
					float3 aabbMax = math.max(p0, math.max(p1, math.max(p2, math.max(p3, math.max(p4, math.max(p5, math.max(p6, p7)))))));

					if (math.any(aabbMax < math.float3(0f)))
					{
						data.renderLayerMask = 0;
						return;
					}
					if (math.any(aabbMin > math.float3(1f)))
					{
						data.renderLayerMask = 0;
						return;
					}

					float2 ndcSize = (aabbMax.xy - aabbMin.xy) * screenSize;
					float radius = math.max(ndcSize.x, ndcSize.y);
					int mip = (int)math.floor(math.log2(radius));
					mip = math.clamp(mip - 2, 0, mipCount - 1);
					float4 mipSize = mipSizes[mip];

					int2 minPx = (int2)math.ceil(aabbMin.xy * mipSize.zw + mipSize.xy);
					int2 maxPx = (int2)math.ceil(aabbMax.xy * mipSize.zw + mipSize.xy);

					int maxIndex = texSize.x * texSize.y - 1;
					int index0 = math.clamp(minPx.x + minPx.y * texSize.x, 0, maxIndex);
					int index1 = math.clamp(minPx.x + maxPx.y * texSize.x, 0, maxIndex);
					int index2 = math.clamp(maxPx.x + maxPx.y * texSize.x, 0, maxIndex);
					int index3 = math.clamp(maxPx.x + minPx.y * texSize.x, 0, maxIndex);


					float d0 = hizData[index0];
					float d1 = hizData[index1];
					float d2 = hizData[index2];
					float d3 = hizData[index3];
					float minD = math.min(d0, math.min(d1, math.min(d2, d3)));
					if (minD > aabbMax.z)
					{
						data.renderLayerMask = 0;
					}
					else
					{
						data.renderLayerMask = 1;
					}
				}
			}
        }

        private void Cull(int index)
        {
			isReadbacking[index] = false;
			int startTime = readBackStartTime[index];
			// 有更晚开启的回读任务提前完成了，那就丢弃这个更早开始的任务
			// 有多个回读任务同时完成了，则使用最早被调用的那个任务
			if(startTime < lastReadBackStartTime || compeleteReadBackCount > 0)
            {
				hizReadbackDatas[index].Dispose();
				return;
            }
			++compeleteReadBackCount;
			validReadBackIndex = index;
			lastReadBackStartTime = startTime;
		}

        public override void BeforeSRPCull(BXMainCameraRenderBase mainRender)
        {
			CompleteCull(mainRender);
			Setup(mainRender);
        }

        public override void AfterSRPCull()
        {
            
        }

        public override void AfterSRPRender(CommandBuffer commandBuffer)
        {
			Render(commandBuffer);
        }
    }
}
