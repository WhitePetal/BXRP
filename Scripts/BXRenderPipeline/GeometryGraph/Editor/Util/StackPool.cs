using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace BXGeometryGraph
{
    public class StackPool<T>
    {
        // Object pool to avoid allocations.
        static readonly ObjectPool<Stack<T>> k_StackPool = new ObjectPool<Stack<T>>(() => new Stack<T>(), null, l => l.Clear());

        public static Stack<T> Get()
        {
            return k_StackPool.Get();
        }

        public static void Release(Stack<T> toRelease)
        {
            k_StackPool.Release(toRelease);
        }
    }
}
