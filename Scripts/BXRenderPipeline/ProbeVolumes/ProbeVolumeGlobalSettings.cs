using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace BXRenderPipeline
{
	[Serializable]
	public class ProbeVolumeRuntimeResources : IRenderPipelineResources
	{
		[Header("Runtime")]


		[ResourcePath("Assets/Shaders/ShaderLibrarys/ProbeVolume/ProbeVolumeBlendStates.compute")]
		public ComputeShader probeVolumeBlendStateCS;
		[ResourcePath("Assets/Shaders/ShaderLibrarys/ProbeVolume/ProbeVolumeUploadData.compute")]
		public ComputeShader probeVolumeUploadDataCS;
		[ResourcePath("Assets/Shaders/ShaderLibrarys/ProbeVolume/ProbeVolumeUploadDataL2.compute")]
		public ComputeShader probeVolumeUploadDataL2CS;
	}

	[Serializable]
	public class ProbeVolumeDebugResources : IRenderPipelineResources
    {
		[Header("Debug")]


		[ResourcePath("Assets/Shaders/ShaderLibrarys/ProbeVolume/Debug/ProbeVolumeDebug.shader")]
		public Shader probeVolumeDebugShader;
		[ResourcePath("Assets/Shaders/ShaderLibrarys/ProbeVolume/Debug/ProbeVolumeFragmentationDebug.shader")]
		public Shader probeVolumeFragmentationDebugShader;
		[ResourcePath("Assets/Shaders/ShaderLibrarys/ProbeVolume/Debug/ProbeVolumeSamplingDebug.shader")]
		public Shader probeVolumeSamplingDebugShader;
		[ResourcePath("Assets/Shaders/ShaderLibrarys/ProbeVolume/Debug/ProbeVolumeOffsetDebug.shader")]
		public Shader probeVolumeOffsetDebugShader;
		[ResourcePath("Assets/Scripts/BXRenderPipeline/ProbeVolumes/Debug/ProbeSamplingDebugMesh.fbx")]
		public Mesh probeSamplingDebugMesh;
		[ResourcePath("Assets/Scripts/BXRenderPipeline/ProbeVolumes/Debug/ProbeVolumeNumbersDisplayTex.fbx")]
		public Texture2D numbersDisplayTex;
	}

	[Serializable]
	public class ProbeVolumeBakingResources : IRenderPipelineResources
    {
		[Header("Baking")]

		// TODO: NEED IMPLEMENT SHADERs

		[ResourcePath("Assets/Shaders/ShaderLibrarys/ProbeVolume/Editor/ProbeVolumeCellDilation.compute")]
		public ComputeShader dilationShader;
		[ResourcePath("Assets/Shaders/ShaderLibrarys/ProbeVolume/Editor/ProbeVolumeSubdivide.compute")]
		public ComputeShader subdivideSceneCS;
		[ResourcePath("Assets/Shaders/ShaderLibrarys/ProbeVolume/Editor/VoxelizeScene.shader")]
		public Shader voxelizeSceneShader;

		[ResourcePath("Assets/Shaders/ShaderLibrarys/ProbeVolume/Editor/TraceVirtualOffset.urtshader")]
		public ComputeShader traceVirtualOffsetCS;
		[ResourcePath("Assets/Shaders/ShaderLibrarys/ProbeVolume/Editor/TraceVirtualOffset.urtshader")]
		public RayTracingShader traceVirtualOffsetRT;

		[ResourcePath("Assets/Shaders/ShaderLibrarys/ProbeVolume/Editor/DynamicGISkyOcclusion.urtshader")]
		public ComputeShader skyOcclusionCS;
		[ResourcePath("Assets/Shaders/ShaderLibrarys/ProbeVolume/Editor/DynamicGISkyOcclusion.urtshader")]
		public RayTracingShader skyOcclusionRT;

		[ResourcePath("Assets/Shaders/ShaderLibrarys/ProbeVolume/Editor/TraceRenderingLayerMask.urtshader")]
		public ComputeShader renderingLayerCS;
		[ResourcePath("Assets/Shaders/ShaderLibrarys/ProbeVolume/Editor/TraceRenderingLayerMask.urtshader")]
		public RayTracingShader renderingLayerRT;

    }

	[Serializable]
	public class ProbeVolumeGlobalSettings : IRenderPipelineResources
	{
		[SerializeField, Tooltip("Enabling this will make APV baked data assets compatible with Addressables and Asset Bundles.This will also make Disk Streaming unavailable. After changing this setting, a clean rebuild may be required for data assets to be included in Adressables and Asset Bundles.")]
		private bool m_ProbeVolumeDisableStreamingAssets;

		public bool probeVolumeDisableStreamingAssets
        {
			get => m_ProbeVolumeDisableStreamingAssets;
			//set => this.SetValueAndNotify(ref m_ProbeVolumeDisableStreamingAssets, value, nameof(m_ProbeVolumeDisableStreamingAssets));
			set => m_ProbeVolumeDisableStreamingAssets = value;

		}
	}
}
