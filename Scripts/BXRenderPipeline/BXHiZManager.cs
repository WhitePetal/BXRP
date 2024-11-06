using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace BXRenderPipeline
{
	public class BXHiZManager : IDisposable
	{
		private struct CullingObjectData
        {
			public Vector4 boundCenter;
			public Vector4 boundSize;
			//public ObjectRenderTest component;
        };

		private static readonly Lazy<BXHiZManager> s_Instance = new Lazy<BXHiZManager>(() => new BXHiZManager());

		public static BXHiZManager instance = s_Instance.Value;

		public RenderTexture[] hizBuffers;

		public bool isInitialized { get; private set; }

		public int frameIndex;

		private Vector2Int screenSize;
		private Vector2Int texSize;
		private Vector4[] mipSizes; // x,y offset, z,w mip size

		private Matrix4x4 projectionMatrix;

		private int mipCount;
		private int cullingObjectCount;

		private ComputeShader cs;
		private NativeArray<float> hizReadbackData;

		private NativeArray<CullingObjectData> cullingObjects = new NativeArray<CullingObjectData>(1024, Allocator.Persistent);

		private bool isReadbacking;

		private bool isViewCamera;

		public void Initialize()
		{
			hizBuffers = new RenderTexture[3];
			mipSizes = new Vector4[16];
			frameIndex = 0;
		}

		public void Setup(BXMainCameraRenderBase mainRender)
        {
			isViewCamera = mainRender.camera.cameraType == CameraType.SceneView;

			if (isViewCamera) return;
			this.cs = mainRender.commonSettings.hizCompute;
			this.screenSize = new Vector2Int(mainRender.width, mainRender.height);
			this.projectionMatrix = GL.GetGPUProjectionMatrix(mainRender.camera.projectionMatrix, false) * mainRender.camera.worldToCameraMatrix;
			int texW = (mainRender.width >> 3) + (mainRender.width >> 4);
			int texH = mainRender.height >> 3;
			//texW = Mathf.IsPowerOfTwo(texW) ? texW : Mathf.NextPowerOfTwo(texW);
			//texH = Mathf.IsPowerOfTwo(texH) ? texH : Mathf.NextPowerOfTwo(texH);
            if (!isInitialized || (texW != texSize.x || texH != texSize.y))
            {
				GenerateHizBuffers(texW, texH);
				isInitialized = true;
			}
			mipCount = 0;
			cullingObjectCount = 0;
		}

		public void Render(CommandBuffer commandBuffer)
        {
			if (isViewCamera) return;
			commandBuffer.BeginSample("Hi-Z");
			commandBuffer.SetComputeTextureParam(cs, 0, "_DepthMap", BXShaderPropertyIDs._EncodeDepthBuffer_TargetID);
			commandBuffer.SetComputeTextureParam(cs, 0, "_HizMap", hizBuffers[0]);
			commandBuffer.DispatchCompute(cs, 0, Mathf.CeilToInt(screenSize.x / 8), Mathf.CeilToInt(screenSize.y / 8), 1);

			Vector2 mipSize = new Vector2(screenSize.x >> 3, texSize.y);
			Vector4 mipOffset = new Vector4(0, 0, mipSize.x, mipSize.y);
			mipSizes[mipCount++] = mipOffset;
			while (mipSize.x > 1 && mipSize.y > 1)
            {
				commandBuffer.SetComputeVectorParam(cs, "_MipOffset", mipOffset);
				commandBuffer.SetComputeTextureParam(cs, 1, "_HizMap", hizBuffers[0]);
				commandBuffer.DispatchCompute(cs, 1, Mathf.CeilToInt(mipSize.x / 8f), Mathf.CeilToInt(mipSize.y / 8f), 1);
				mipOffset += new Vector4(mipSize.x, 0, 0, 0);
				mipSize *= 0.5f;
				mipSizes[mipCount++] = new Vector4(mipOffset.x, mipOffset.y, mipSize.x, mipSize.y);
				if (mipSize.x <= 1 || mipSize.y <= 1) break;
				mipOffset += new Vector4(0, mipSize.y, 0, 0);
				mipSize *= 0.5f;
				mipSizes[mipCount++] = new Vector4(mipOffset.x, mipOffset.y, mipSize.x, mipSize.y);
				if (mipSize.x <= 1 || mipSize.y <= 1) break;
				mipOffset += new Vector4(mipSize.x, 0, 0, 0);
				mipSize *= 0.5f;
				mipOffset.z = mipSize.x;
				mipOffset.w = mipSize.y;
				mipSizes[mipCount++] = new Vector4(mipOffset.x, mipOffset.y, mipSize.x, mipSize.y);
			}

            if (!isReadbacking)
            {
				hizReadbackData = new NativeArray<float>(texSize.x * texSize.y, Allocator.Persistent);
				commandBuffer.RequestAsyncReadbackIntoNativeArray<float>(ref hizReadbackData, hizBuffers[0], ReadBack);
				isReadbacking = true;
			}
			commandBuffer.EndSample("Hi-Z");
        }

		public void Register(ObjectRenderTest obj)
        {
			if (isViewCamera) return;
			MeshRenderer mr = obj.GetComponent<MeshRenderer>();
			CullingObjectData data = new CullingObjectData()
			{
				boundCenter = mr.bounds.center,
				boundSize = mr.bounds.size,
				//component = obj
			};
			cullingObjects[cullingObjectCount++] = data;
		}

		public void Dispose()
		{
			if (isReadbacking)
			{
				AsyncGPUReadback.WaitAllRequests();
				isReadbacking = false;
			}
			if (isInitialized)
            {
				for(int i = 0; i < 3; ++i)
                {
					hizBuffers[i].Release();
                }
				hizBuffers = null;
				isInitialized = false;
			}
			cullingObjects.Dispose();
            mipSizes = null;
		}

		private void GenerateHizBuffers(int texW, int texH)
        {
			texSize = new Vector2Int(texW, texH);
			//Vector2 mipSize = new Vector2(texW >> 1, texH);
			//while(mipSize.x > 1)
			RenderTextureDescriptor rd = new RenderTextureDescriptor(texW, texH, UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat, 0);
			rd.sRGB = false;
			for (int i = 0; i < 3; ++i)
			{
				hizBuffers[i] = new RenderTexture(rd);
				hizBuffers[i].filterMode = FilterMode.Point;
				hizBuffers[i].enableRandomWrite = true;
				hizBuffers[i].Create();
			}
		}

		private void ReadBack(AsyncGPUReadbackRequest obj)
		{
			if (obj.done)
			{
				if (obj.hasError)
				{
					throw new Exception("Hi-Z Readback Error");
				}
				for (int i = 0; i < cullingObjectCount; ++i)
				{
					var data = cullingObjects[i];
					Vector3 center = data.boundCenter;
					Vector3 size = data.boundSize;
					Vector3 p0 = center + size;
					Vector3 p1 = center - size;
					Vector3 p2 = center + new Vector3(size.x, size.y, -size.z);
					Vector3 p3 = center + new Vector3(size.x, -size.y, -size.z);
					Vector3 p4 = center + new Vector3(-size.x, -size.y, size.z);
					Vector3 p5 = center + new Vector3(-size.x, size.y, -size.z);
					Vector3 p6 = center + new Vector3(size.x, -size.y, size.z);
					Vector3 p7 = center + new Vector3(-size.x, size.y, size.z);
					Debug.Log("p0: " + p0);

					p0 = GetNDCPos(p0);
					Debug.Log("p0 ndc: " + p0);
					p1 = GetNDCPos(p1);
					p2 = GetNDCPos(p2);
					p3 = GetNDCPos(p3);
					p4 = GetNDCPos(p4);
					p5 = GetNDCPos(p5);
					p6 = GetNDCPos(p6);
					p7 = GetNDCPos(p7);

					Vector3 aabbMin = Vector3.Min(p0, Vector3.Min(p1, Vector3.Min(p2, Vector3.Min(p3, Vector3.Min(p4, Vector3.Min(p5, Vector3.Min(p6, p7)))))));
					Vector3 aabbMax = Vector3.Max(p0, Vector3.Max(p1, Vector3.Max(p2, Vector3.Max(p3, Vector3.Max(p4, Vector3.Max(p5, Vector3.Max(p6, p7)))))));
					Debug.Log("aabb: " + aabbMin + " === " + aabbMax);

					Vector2 ndcSize = new Vector2(aabbMax.x - aabbMin.x, aabbMax.y - aabbMin.y) * screenSize;
					float radius = Mathf.Max(ndcSize.x, ndcSize.y);
					Debug.Log("radius: " + radius + " === " + ndcSize + " === " + screenSize);
					int mip = Mathf.CeilToInt(Mathf.Log(radius, 2));
					mip = Mathf.Clamp(mip, 0, mipCount - 1);
					Vector4 mipSize = mipSizes[mip];
					Debug.Log("mip: " + mip + " === " + mipSize + " === " + mipCount);

					Vector2 minPx = new Vector2(aabbMin.x * mipSize.z, aabbMin.y * mipSize.w) + new Vector2(mipSize.x, mipSize.y);
					Vector2 maxPx = new Vector2(aabbMax.x * mipSize.z, aabbMax.y * mipSize.w) + new Vector2(mipSize.x, mipSize.y);

					Debug.Log("minPx: " + minPx);
					Debug.Log("mipStart: " + new Vector2(mipSize.x, mipSize.y) + "mipEnd: " + new Vector2(mipSize.x + mipSize.z, mipSize.y + mipSize.w));
					int index0 = Mathf.CeilToInt(minPx.x + minPx.y * texSize.x);
					Debug.Log("index0: " + index0);
					int index1 = Mathf.CeilToInt(minPx.x + maxPx.y * texSize.x);
					int index2 = Mathf.CeilToInt(maxPx.x + maxPx.y * texSize.x);
					int index3 = Mathf.CeilToInt(maxPx.x + minPx.y * texSize.x);

					float d0 = hizReadbackData[index0];
					float d1 = hizReadbackData[index1];
					float d2 = hizReadbackData[index2];
					float d3 = hizReadbackData[index3];
					Debug.Log("d0: " + d0 + " === " + d1 + " === " + d2 + " === " + d3);
					float minD = Mathf.Min(d0, Mathf.Min(d1, Mathf.Min(d2, d3)));
					if (minD > aabbMin.z)
					{
						Debug.Log("Be Culled: " + minD + " === " + aabbMin.z);
					}
					else
					{
						Debug.Log("Not Culled: " + minD + " === " + aabbMin.z);
					}
				}

				isReadbacking = false;
				hizReadbackData.Dispose();
			}
		}

		private Vector3 GetNDCPos(Vector3 pos)
		{
			Vector4 p = projectionMatrix * new Vector4(pos.x, pos.y, pos.z, 1f);
			p = new Vector4(p.x / p.w, p.y / p.w, p.z / p.w, 1f);
			p = new Vector4(p.x * 0.5f + 0.5f, 1f - (p.y * 0.5f + 0.5f), p.z, 1f);
			return p;
		}
	}
}
