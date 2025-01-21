using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXRenderPipeline
{
	[Serializable]
	internal class ProbeVolumeRuntimeResources
	{
		[Header("Runtime")]

		[ResourcePath("Assets/Shaders/ShaderLibrarys/ProbeVolume/ProbeVolumeBlendStates.compute")]
		public ComputeShader probeVolumeBlendStateCS;
		public ComputeShader probeVolumeUploadDataCS;
		public ComputeShader probeVolumeUploadDataL2CS;
	}

	[Serializable]
	internal class ProbeVolumeGlobalSettings
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
