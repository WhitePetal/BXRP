using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace BXGeometryGraph.Runtime
{
	[Serializable]
	public struct GeometryData
	{
		[SerializeField]
		public NativeArray<float3> points;
	}
}
