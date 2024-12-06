using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGraphing
{
    public static class DictionaryPool<TKey, TValue>
    {
        // Object pool to avoid allocations.
        private static readonly ObjectPool<Dictionary<TKey, TValue>> k_Pool = new ObjectPool<Dictionary<TKey, TValue>>(null, l => l.Clear());

        public static Dictionary<TKey, TValue> Get()
        {
            return k_Pool.Get();
        }

        public static PooledObject<Dictionary<TKey, TValue>> GetDisposable()
        {
            return k_Pool.GetDisposable();
        }

        public static void Release(Dictionary<TKey, TValue> toRelease)
        {
            k_Pool.Release(toRelease);
        }
    }
}
