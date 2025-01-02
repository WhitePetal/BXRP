using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;

namespace BXGeometryGraph.Runtime
{
	[Serializable]
	public abstract class AbstractGeometryJob
	{
		[SerializeField]
		public AbstractGeometryJob[] depenedJobs;

		[SerializeField]
		public int testSerialize;

		[NonSerialized]
		protected GeometryData geometryData;

		// May be in Unity_Editor nodeGuid is unused
//#if UNITY_EDITOR
		[NonSerialized]
		public string nodeGuid;
		//#endif

		public abstract JobHandle Schedule(ref GeometryData geoData, JobHandle dependsOn = new JobHandle());

		public abstract void WriteResultToGeoData(ref GeometryData geoData);

		public abstract T GetOutput<T>(int outputId) where T : struct;
	}
}
