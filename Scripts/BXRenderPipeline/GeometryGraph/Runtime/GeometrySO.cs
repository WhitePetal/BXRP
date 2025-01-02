using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Serialization.Json;
using UnityEngine;

namespace BXGeometryGraph.Runtime
{
	[Serializable]
	public class GeometrySO : ScriptableObject, ISerializationCallbackReceiver
	{
		[SerializeField]
		public string json;

		[Serializable]
		public class InnerData
        {
			[SerializeField]
			public GeometryData geometryData;

			[SerializeField]
			public AbstractGeometryJob ouputJob;

			[SerializeField]
			public int pathCount;

			[NonSerialized]
			private JobHandle[] jobHandles;

			[NonSerialized]
			private bool isScheduling;
		}

        [NonSerialized]
		public InnerData innerData;

		private JobHandle jobHandle;

		public void Schedule()
		{
			//if (isScheduling)
			//	return;
			//isScheduling = true;
			//jobHandle = default;

			//for(int i = 0; i < geometryJobs.Length; ++i)
			//{
			//	geometryJobs[i].Schedule(ref geometryData, jobHandle);
			//}
		}

		public void Compelete()
		{
			//if (!isScheduling)
			//	return;
			//jobHandle.Complete();
			//for (int i = 0; i < geometryJobs.Length; ++i)
			//{
			//	geometryJobs[i].WriteResultToGeoData(ref geometryData);
			//}
			//isScheduling = false;
		}

		public void ClearStates()
        {
			//isScheduling = false;
        }

        public void OnBeforeSerialize()
        {

        }

		public void Deserialize()
        {
			if (string.IsNullOrEmpty(json) || innerData != null)
				return;
			innerData = JsonSerialization.FromJson<InnerData>(json);
		}

        public unsafe void OnAfterDeserialize()
        {
			// will crash invoke JsonSerialization.FromJson in OnAfterDeserialize
			// x_x
			//Deserialize();
		}
	}
}
