using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace BXRenderPipeline
{
    internal static class BXNativeArrayExtensions
    {
        public static unsafe ref T UnsafeElementAt<T>(this NativeArray<T> array, int index) where T : struct
		{
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafeReadOnlyPtr(), index);
		}

        public static unsafe ref T UnsafeElementAtMutable<T>(this NativeArray<T> array, int index) where T : struct
		{
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafePtr(), index);
		}
    }
}
