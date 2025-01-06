using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace BXGeometryGraph.Runtime
{
	[Serializable]
	public unsafe abstract class AbstractGeometryJob : IDisposable
	{
		[SerializeField]
		public AbstractGeometryJob[] depenedJobs;

		// May be in Unity_Editor nodeGuid is unused
		//#if UNITY_EDITOR
		[NonSerialized]
		public string nodeGuid;
		//#endif

		public abstract JobHandle Schedule(JobHandle dependsOn = new JobHandle());

		public abstract JobHandle WriteResultToGeoData(GeometryData* geoData, JobHandle dependsOn = default);

		public virtual float3 GetFloat3(int outputId) { return float3.zero; }
		public virtual int GetInt(int outputId) { return 0; }
		public virtual GeometryData GetGeometry(int outputId)
        {
			GeometryData data = new GeometryData();
			data.CreateEmpty(Allocator.TempJob);
			return data;

		}

		public abstract void Dispose();
    }
}
