using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using BXRenderPipelineDeferred;
//using BXRenderPipelineForward;

namespace BXRenderPipeline
{
    public class BXRenderPipeline : RenderPipeline
    {
		public static ShaderTagId[] forwardShaderTagIds = new ShaderTagId[]
		{
			new ShaderTagId("SRPDefaultUnlit"),
			new ShaderTagId("BXForwardBase"),
			new ShaderTagId("BXForwardBaseAlphaTest")
		};
		public static ShaderTagId[] deferredShaderTagIds = new ShaderTagId[]
		{
			new ShaderTagId("SRPDefaultUnlit"),
			new ShaderTagId("BXDeferredBase"),
			new ShaderTagId("BXDeferredBaseAlphaTest"),
			new ShaderTagId("BXDeferredBaseAlpha"),
			new ShaderTagId("BXDeferredAddAlpha")
		};

		public static ShaderTagId[] legacyShaderTagIds = new ShaderTagId[]
		{
			new ShaderTagId("Always"),
			new ShaderTagId("ForwardBase"),
			new ShaderTagId("PrepassBase"),
			new ShaderTagId("Vertex"),
			new ShaderTagId("VertexLMRGBM"),
			new ShaderTagId("VertexLM")
		};

		private bool useDynamicBatching, useGPUInstancing;
		private BXMainCameraRenderDeferred mainCameraRender = new BXMainCameraRenderDeferred();
		// for reflection probe bake、preview window each other render
		private BXOtherCameraRender otherCameraRender = new BXOtherCameraRender();
		public BXRenderCommonSettings commonSettings;

		private List<BXRenderFeature> beforeRenderFeatures;
		private List<BXRenderFeature> onDirShadowRenderFeatures;
		private List<BXRenderFeature> beforeOpaqueRenderFeatures;
		private List<BXRenderFeature> afterOpaqueRenderFeatures;
		private List<BXRenderFeature> beforeTransparentFeatures;
		private List<BXRenderFeature> afterTransparentFeatures;
		private List<BXRenderFeature> onPostProcessRenderFeatures;

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
			QualitySettings.realtimeReflectionProbes = true;

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

			Assert.IsTrue(commonSettings.coreBlitPS != null, "CommonSettings CoreBlitPS Shader is null");
			Assert.IsTrue(commonSettings.coreBlitColorAndDepthPS != null, "CommonSettings CoreBlitColorAndDepth Shader is null");
			Blitter.Initialize(commonSettings.coreBlitPS, commonSettings.coreBlitColorAndDepthPS);
			BXVolumeManager.instance.Initialize();
			BXHiZManager.instance.Initialize();
		}

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
					otherCameraRender.Render(context, camera, useDynamicBatching, useGPUInstancing, commonSettings);
#endif
				}
			}
		}

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

		private void InitRenderFeatures(List<BXRenderFeature> renderFeatures)
		{
			if (renderFeatures == null || renderFeatures.Count == 0) return;
			for (int i = 0; i < renderFeatures.Count; ++i)
			{
				renderFeatures[i].Init(commonSettings);
			}
		}

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
	}
}
