using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;

namespace BXGeometryGraph.Runtime
{
	public class CubeMeshJobManager : AbstractGeometryJob
	{
		//[BurstCompile]
		public struct CubeMeshJob : IJobParallelFor
		{
			public void Execute(int index)
			{
				throw new System.NotImplementedException();
			}
		}

		public override JobHandle Schedule(ref GeometryData geoData, JobHandle dependsOn = default)
		{
			throw new System.NotImplementedException();
		}

		public override void WriteResultToGeoData(ref GeometryData geoData)
		{
			throw new System.NotImplementedException();
		}
	}
}
