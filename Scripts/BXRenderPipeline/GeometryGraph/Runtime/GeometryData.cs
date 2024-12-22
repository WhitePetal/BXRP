using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace BXGeometryGraph.Runtime
{
	[Serializable]
	public struct GeometryData
	{
		[SerializeField]
		public NativeArray<Vector3> points;

	}
}
