using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXRenderPipeline
{
	internal class ProbeGlobalIndirection
	{
		private const int kUintPerEntry = 3;
		internal int estimatedVMemCost { get; private set; }

		// IMPORTANT! IF THIS VALUE CHANGES DATA NEEDS TO BE REBAKED.
		internal const int kEntryMaxSubdivLevel = 3;

		internal struct IndexMetaData
		{
			private static uint[] s_PackedValues = new uint[kUintPerEntry];

		}
	}
}
