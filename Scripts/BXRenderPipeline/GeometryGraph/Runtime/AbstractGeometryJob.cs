using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace BXGeometryGraph.Runtime
{
	[Serializable]
	public abstract class AbstractGeometryJob
	{
		[SerializeField]
		public AbstractGeometryJob[] depenedJobs;

		// May be in Unity_Editor nodeGuid is unused
		//#if UNITY_EDITOR
		[NonSerialized]
		public string nodeGuid;
		//#endif

		public abstract JobHandle Schedule(ref GeometryData geoData, JobHandle dependsOn = new JobHandle());

		public abstract void WriteResultToGeoData(ref GeometryData geoData);

		public virtual float3 GetFloat3(int outputId) { return float3.zero; }
		public virtual int GetInt(int outputId) { return 0; }
	}
}
