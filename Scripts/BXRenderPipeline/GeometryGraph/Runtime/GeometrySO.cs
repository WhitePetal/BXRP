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
	public class GeometrySO : ScriptableObject
	{
		[SerializeField]
		public string json;

		[Serializable]
		public class InnerData
        {
			[SerializeField]
			public AbstractGeometryJob ouputJob;
		}

        [NonSerialized]
		public InnerData data;

		public void ClearStates()
        {
			//isScheduling = false;
        }

        public void OnBeforeSerialize()
        {

        }

		public void Init(GeometryRenderer owner)
        {
			data.ouputJob.Init(owner);
        }

		public void Deserialize()
        {
			if (string.IsNullOrEmpty(json) || data != null)
				return;
			data = JsonSerialization.FromJson<InnerData>(json);
		}
	}
}
