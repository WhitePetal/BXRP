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
		/// <see cref="BXRenderCommonSettings"/>
        /// </summary>
		public BXRenderCommonSettings commonSettings;

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
        public BXRenderPipeline(bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatching, BXRenderCommonSettings commonSettings)
		{
			this.useDynamicBatching = useDynamicBatching;
			this.useGPUInstancing = useGPUInstancing;
			this.commonSettings = commonSettings;

			commonSettings.Init();

			GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatching;
			GraphicsSettings.lightsUseLinearIntensity = false;
			QualitySettings.antiAliasing = 1;
			//QualitySettings.realtimeReflectionProbes = false;

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
					mainCameraRender.Render(context, camera, useDynamicBatching, useGPUInstancing);
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

			base.Dispose(disposing);

			BXVolumeManager.instance.Deinitialize();
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
