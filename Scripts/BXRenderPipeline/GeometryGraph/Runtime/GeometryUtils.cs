using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using UnityEngine;

namespace BXGeometryGraph.Runtime
{
    internal struct utils
    {
        /// <summary>
        /// x == y || x == z
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        [BurstCompile]
        internal static bool ELEM(int x, int y, int z)
        {
            return x == y || x == z;
        }
    }
}
