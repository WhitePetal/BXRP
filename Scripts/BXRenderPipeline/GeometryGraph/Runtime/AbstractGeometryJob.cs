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
		public int pathID;

		public abstract JobHandle Schedule(ref GeometryData geoData, JobHandle dependsOn = new JobHandle());

		public abstract void WriteResultToGeoData(ref GeometryData geoData);
	}
}
