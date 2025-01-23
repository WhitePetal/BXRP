using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
		public ComputeShader probeVolumeUploadDataL2CS;
	}

	[Serializable]
	public class ProbeVolumeGlobalSettings : IRenderPipelineResources
	{
		[Header("Debug")]

		public Shader probeVolumeDebugShader;
		public Shader probeVolumeFragmentationDebugShader;
		public Shader probeVolumeSamplingDebugShader;
		public Shader probeVolumeOffsetDebugShader;
		public Mesh probeSamplingDebugMesh;
		public Texture2D numbersDisplayTex;
	}
}
