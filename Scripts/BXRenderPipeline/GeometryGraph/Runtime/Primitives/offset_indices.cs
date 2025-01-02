using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;

namespace BXGeometryGraph.Runtime
{
    internal struct offset_indices
    {
        [BurstCompile]
        internal static void fill_constant_grouo_size(int size, int start_offset, ref NativeArray<int> offsets)
        {
            for(int i = 0; i < offsets.Length; ++i)
            {
                offsets[i] = size * i + start_offset;
            }
        }
    }
}
