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
		[NonSerialized]
		protected GeometryRenderer owner;

		public virtual void Init(GeometryRenderer owner)
		{
			this.owner = owner;

			if (depenedJobs == null) return;
			for(int i = 0; i < depenedJobs.Length; ++i)
			{
				if (depenedJobs[i] == null)
					continue;
				depenedJobs[i].Init(owner);
			}
		}

		public abstract JobHandle Schedule(JobHandle dependsOn = new JobHandle());

		public abstract JobHandle WriteResultToGeoData(GeometryData* geoData, JobHandle dependsOn = default);

		public virtual float3 GetFloat3(int outputId) { return float3.zero; }
		public virtual int GetInt(int outputId) { return 0; }
		public virtual GeometryData GetGeometry(int outputId)
        {
			GeometryData data = new GeometryData();
			return data;

		}

		public virtual void Dispose()
        {
			if (depenedJobs != null && depenedJobs.Length > 0)
			{
				for (int i = 0; i < depenedJobs.Length; ++i)
				{
					if (depenedJobs[i] == null)
						continue;
					depenedJobs[i].Dispose();
				}
			}
		}
    }
}
