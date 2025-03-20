using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using BXRenderPipelineDeferred;
//using BXRenderPipelineForward;

namespace BXRenderPipeline
{
	/// <summary>
    /// BXRP渲染管线实例类
    /// 即渲染的入口
    /// </summary>
    public class BXRenderPipeline : RenderPipeline
    {
		/// <summary>
        /// 前向渲染所支持的所有LightMode
        /// </summary>
		public static ShaderTagId[] forwardShaderTagIds = new ShaderTagId[]
		{
			new ShaderTagId("SRPDefaultUnlit"),
			new ShaderTagId("BXForwardBase"),
			new ShaderTagId("BXForwardBaseAlphaTest")
		};
		/// <summary>
        /// 延迟渲染所支持的所有LightMode
        /// </summary>
		public static ShaderTagId[] deferredShaderTagIds = new ShaderTagId[]
		{
			new ShaderTagId("SRPDefaultUnlit"),
			new ShaderTagId("BXDeferredBase"),
			new ShaderTagId("BXDeferredBaseAlphaTest"),
			new ShaderTagId("BXDeferredBaseAlpha"),
			new ShaderTagId("BXDeferredAddAlpha")
		};
		/// <summary>
        /// Unity自带的，但是BXRP已经不支持的LightMode
        /// </summary>
		public static ShaderTagId[] legacyShaderTagIds = new ShaderTagId[]
		{
			new ShaderTagId("Always"),
			new ShaderTagId("ForwardBase"),
			new ShaderTagId("PrepassBase"),
			new ShaderTagId("Vertex"),
			new ShaderTagId("VertexLMRGBM"),
			new ShaderTagId("VertexLM")
		};

		/// <summary>
        /// 是否使用动态和批
        /// </summary>
		private bool useDynamicBatching;
		/// <summary>
        /// 是否使用 GPU Instancing
        /// </summary>
		private bool useGPUInstancing;

		/// <summary>
        /// 主相机渲染器
        /// 这里默认使用延迟渲染方式
        /// 如果要使用前向渲染可以把这里改为 BXMainCameraRender <see cref="BXRenderPipelineForward.BXMainCameraRender"/>
        /// </summary>
		private BXMainCameraRenderDeferred mainCameraRender = new BXMainCameraRenderDeferred();
		// for reflection probe bake、preview window each other render
		/// <summary>
		/// 其它相机渲染器
        /// 主要用于 reflection probe bake、preview window 和 所有除MainCamera外其他场景内的相机
		/// 这里默认使用延迟渲染方式
		/// 如果要使用前向渲染可以把这里改为 BXMainCameraRender <see cref="BXRenderPipelineForward.BXOtherCameraRender"/>
		/// </summary>
		private BXOtherCameraRenderDeferred otherCameraRender = new BXOtherCameraRenderDeferred();
		/// <summary>
        /// 管线基础设置
        /// </summary>
		public BXRenderCommonSettings commonSettings;

		/// <summary>
		/// 在所有渲染之前执行的RenderFeatures
		/// <see cref="BXRenderFeature"/>
		/// </summary>
		private List<BXRenderFeature> beforeRenderFeatures;
		/// <summary>
		/// 在方向光阴影渲染之前执行的RenderFeatures
		/// <see cref="BXRenderFeature"/>
		/// </summary>
		private List<BXRenderFeature> onDirShadowRenderFeatures;
		/// <summary>
		/// 在不透明物体渲染之前执行的RenderFeatures
		/// <see cref="BXRenderFeature"/>
		/// </summary>
		private List<BXRenderFeature> beforeOpaqueRenderFeatures;
		/// <summary>
		/// 在不透明物体渲染之后执行的RenderFeatures
		/// <see cref="BXRenderFeature"/>
		/// </summary>
		private List<BXRenderFeature> afterOpaqueRenderFeatures;
		/// <summary>
		/// 在透明物体渲染之前执行的RenderFeatures
		/// <see cref="BXRenderFeature"/>
		/// </summary>
		private List<BXRenderFeature> beforeTransparentFeatures;
		/// <summary>
		/// 在透明物体渲染之后执行的RenderFeatures
		/// <see cref="BXRenderFeature"/>
		/// </summary>
		private List<BXRenderFeature> afterTransparentFeatures;
		/// <summary>
		/// 在后处理之前执行的RenderFeatures
		/// <see cref="BXRenderFeature"/>
		/// </summary>
		private List<BXRenderFeature> onPostProcessRenderFeatures;

		/// <summary>
        /// 构建BXRP实例
        /// </summary>
        /// <param name="useDynamicBatching"></param>
        /// <param name="useGPUInstancing"></param>
        /// <param name="useSRPBatching"></param>
        /// <param name="commonSettings"></param>
        /// <param name="beforeRenderFeatures"></param>
        /// <param name="onDirShadowRenderFeatures"></param>
        /// <param name="beforeOpaqueRenderFeatures"></param>
        /// <param name="afterOpaqueRenderFeatures"></param>
        /// <param name="beforeTransparentFeatures"></param>
        /// <param name="afterTransparentFeatures"></param>
        /// <param name="onPostProcessRenderFeatures"></param>
        public BXRenderPipeline(bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatching, BXRenderCommonSettings commonSettings,
			List<BXRenderFeature> beforeRenderFeatures, List<BXRenderFeature> onDirShadowRenderFeatures, 
			List<BXRenderFeature> beforeOpaqueRenderFeatures, List<BXRenderFeature> afterOpaqueRenderFeatures,
			List<BXRenderFeature> beforeTransparentFeatures, List<BXRenderFeature> afterTransparentFeatures,
			List<BXRenderFeature> onPostProcessRenderFeatures)
		{
			this.useDynamicBatching = useDynamicBatching;
			this.useGPUInstancing = useGPUInstancing;
			this.commonSettings = commonSettings;

			commonSettings.Init();

			GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatching;
			GraphicsSettings.lightsUseLinearIntensity = false;
			QualitySettings.antiAliasing = 1;
			//QualitySettings.realtimeReflectionProbes = false;

			this.beforeRenderFeatures = beforeRenderFeatures;
			this.onDirShadowRenderFeatures = onDirShadowRenderFeatures;
			this.beforeOpaqueRenderFeatures = beforeOpaqueRenderFeatures;
			this.afterOpaqueRenderFeatures = afterOpaqueRenderFeatures;
			this.beforeTransparentFeatures = beforeTransparentFeatures;
			this.afterTransparentFeatures = afterTransparentFeatures;
			this.onPostProcessRenderFeatures = onPostProcessRenderFeatures;
			InitRenderFeatures(beforeRenderFeatures);
			InitRenderFeatures(onDirShadowRenderFeatures);
			InitRenderFeatures(beforeOpaqueRenderFeatures);
			InitRenderFeatures(afterOpaqueRenderFeatures);
			InitRenderFeatures(beforeTransparentFeatures);
			InitRenderFeatures(afterTransparentFeatures);
			InitRenderFeatures(onPostProcessRenderFeatures);

			mainCameraRender.Init(commonSettings);
            otherCameraRender.Init(commonSettings);

			Assert.IsTrue(commonSettings.coreBlitPS != null, "CommonSettings CoreBlitPS Shader is null");
			Assert.IsTrue(commonSettings.coreBlitColorAndDepthPS != null, "CommonSettings CoreBlitColorAndDepth Shader is null");
			Blitter.Initialize(commonSettings.coreBlitPS, commonSettings.coreBlitColorAndDepthPS);
			BXVolumeManager.instance.Initialize();
			BXHiZManager.instance.Initialize();
		}

		/// <summary>
        /// BXRP管线渲染执行入口
        /// 每个渲染帧调用一次
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cameras"></param>
		protected override void Render(ScriptableRenderContext context, Camera[] cameras)
		{
			for(int i = 0; i < cameras.Length; ++i)
			{
				var camera = cameras[i];
				if(camera.CompareTag("MainCamera") || camera.cameraType == CameraType.SceneView)
				{
					mainCameraRender.Render(context, camera, useDynamicBatching, useGPUInstancing,
						beforeRenderFeatures, onDirShadowRenderFeatures,
						beforeOpaqueRenderFeatures, afterOpaqueRenderFeatures,
						beforeTransparentFeatures, afterTransparentFeatures,
						onPostProcessRenderFeatures);
				}
				else
				{
					// other camera use forward path render always
					// and only used in editor
					// in runtime we always only use main camera
#if UNITY_EDITOR
					otherCameraRender.Render(context, camera, useDynamicBatching, useGPUInstancing);
#endif
				}
			}
		}

		/// <summary>
        /// BXRP管线实例销毁方法
        /// </summary>
        /// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			BXHiZManager.instance.Dispose();
			mainCameraRender.Dispose();
			mainCameraRender = null;
			otherCameraRender.Dispose();
			otherCameraRender = null;
			commonSettings = null;
			Blitter.Cleanup();
			DisposeRenderFeatures(ref beforeRenderFeatures);
			DisposeRenderFeatures(ref onDirShadowRenderFeatures);
			DisposeRenderFeatures(ref beforeOpaqueRenderFeatures);
			DisposeRenderFeatures(ref afterOpaqueRenderFeatures);
			DisposeRenderFeatures(ref beforeTransparentFeatures);
			DisposeRenderFeatures(ref afterTransparentFeatures);
			DisposeRenderFeatures(ref onPostProcessRenderFeatures);
			base.Dispose(disposing);

			BXVolumeManager.instance.Deinitialize();
		}

		/// <summary>
        /// 初始化所有RenderFeautre
        /// <see cref="BXRenderFeature"/>
        /// </summary>
        /// <param name="renderFeatures"></param>
		private void InitRenderFeatures(List<BXRenderFeature> renderFeatures)
		{
			if (renderFeatures == null || renderFeatures.Count == 0) return;
			for (int i = 0; i < renderFeatures.Count; ++i)
			{
				renderFeatures[i].Init(commonSettings);
			}
		}

		/// <summary>
        /// 销毁所有RenderFeature
        /// <see cref="BXRenderFeature"/>
        /// </summary>
        /// <param name="renderFeatures"></param>
		private void DisposeRenderFeatures(ref List<BXRenderFeature> renderFeatures)
		{
			if (renderFeatures == null) return;
			if(renderFeatures.Count == 0)
			{
				renderFeatures = null;
				return;
			}

			foreach(var feature in renderFeatures)
			{
				feature.Dispose();
			}
#if UNITY_EDITOR
			renderFeatures.RemoveAll(feature => feature.isDynamic);
#else
			renderFeatures.Clear();
#endif
			renderFeatures = null;
		}

		/// <summary>
        /// 向BXRP管线实例添加(注册)RenderFeature
        /// </summary>
        /// <param name="renderFeature"></param>
        /// <param name="step"></param>
		public void AddRenderFeature(BXRenderFeature renderFeature, RenderFeatureStep step)
		{
			switch (step)
			{
				case RenderFeatureStep.BeforeRender:
					beforeRenderFeatures.Add(renderFeature);
					break;
				case RenderFeatureStep.OnDirShadows:
					onDirShadowRenderFeatures.Add(renderFeature);
					break;
				case RenderFeatureStep.BeforeOpaque:
					beforeOpaqueRenderFeatures.Add(renderFeature);
					break;
				case RenderFeatureStep.AfterOpaque:
					afterOpaqueRenderFeatures.Add(renderFeature);
					break;
				case RenderFeatureStep.BeforeTransparent:
					beforeTransparentFeatures.Add(renderFeature);
					break;
				case RenderFeatureStep.AfterTransparent:
					afterTransparentFeatures.Add(renderFeature);
					break;
				case RenderFeatureStep.OnPostProcess:
					onPostProcessRenderFeatures.Add(renderFeature);
					break;
			}
			renderFeature.isDynamic = true;
			renderFeature.step = step;
			renderFeature.Init(commonSettings);
		}

		/// <summary>
        /// 从BXRP管线实例移除(销毁)RenderFeature
        /// </summary>
        /// <param name="renderFeature"></param>
		public void RemoveRenderFeature(BXRenderFeature renderFeature)
		{
			switch (renderFeature.step)
			{
				case RenderFeatureStep.BeforeRender:
					beforeRenderFeatures.Remove(renderFeature);
					break;
				case RenderFeatureStep.OnDirShadows:
					onDirShadowRenderFeatures.Remove(renderFeature);
					break;
				case RenderFeatureStep.BeforeOpaque:
					beforeOpaqueRenderFeatures.Remove(renderFeature);
					break;
				case RenderFeatureStep.AfterOpaque:
					afterOpaqueRenderFeatures.Remove(renderFeature);
					break;
				case RenderFeatureStep.BeforeTransparent:
					beforeTransparentFeatures.Remove(renderFeature);
					break;
				case RenderFeatureStep.AfterTransparent:
					afterTransparentFeatures.Remove(renderFeature);
					break;
				case RenderFeatureStep.OnPostProcess:
					onPostProcessRenderFeatures.Remove(renderFeature);
					break;
			}
			renderFeature.Dispose();
		}

		/// <summary>
        /// 访问指定渲染阶段执行的所有RenderFeature
        /// </summary>
        /// <param name="step"></param>
        /// <returns></returns>
		public List<BXRenderFeature> GetRenderFeatures(RenderFeatureStep step)
		{
			switch (step)
			{
				case RenderFeatureStep.BeforeRender:
					return beforeRenderFeatures;
				case RenderFeatureStep.OnDirShadows:
					return onDirShadowRenderFeatures;
				case RenderFeatureStep.BeforeOpaque:
					return beforeOpaqueRenderFeatures;
				case RenderFeatureStep.AfterOpaque:
					return afterOpaqueRenderFeatures;
				case RenderFeatureStep.BeforeTransparent:
					return beforeTransparentFeatures;
				case RenderFeatureStep.AfterTransparent:
					return afterTransparentFeatures;
				case RenderFeatureStep.OnPostProcess:
					return onPostProcessRenderFeatures;
			}
			return null;
		}

		/// <summary>
        /// 外部快速访问管线基础设置的方法
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
		public static bool TryGetRenderCommonSettings(out BXRenderCommonSettings settings)
		{
			settings = null;
			var pipelineAsset = GraphicsSettings.currentRenderPipeline;
			if (pipelineAsset == null)
				return false;

			var bxPipelineAsset = pipelineAsset as BXRenderPipelineAsset;
			if (bxPipelineAsset == null)
				return false;

			settings = bxPipelineAsset.commonSettings;

			return settings != null;
		}
	}
}
