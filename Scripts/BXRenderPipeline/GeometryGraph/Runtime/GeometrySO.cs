using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;

namespace BXGeometryGraph.Runtime
{
	[Serializable]
	public class GeometrySO : ScriptableObject, ISerializationCallbackReceiver
	{
		[SerializeField]
		public GeometryData geometryData;

		[SerializeField]
		public AbstractGeometryJob[] geometryJobs;

		[SerializeField]
		private int pathCount;

		private JobHandle[] jobHandles;

		

		private JobHandle jobHandle;

		public void Schedule()
		{
			jobHandle = default;

			for(int i = 0; i < geometryJobs.Length; ++i)
			{
				geometryJobs[i].Schedule(ref geometryData, jobHandle);
			}
		}

		public void Compelete()
		{
			jobHandle.Complete();
			for (int i = 0; i < geometryJobs.Length; ++i)
			{
				geometryJobs[i].WriteResultToGeoData(ref geometryData);
			}
		}

		public void OnBeforeSerialize()
		{
			return;
		}

		public void OnAfterDeserialize()
		{
			jobHandles = new JobHandle[pathCount];
		}
	}
}
