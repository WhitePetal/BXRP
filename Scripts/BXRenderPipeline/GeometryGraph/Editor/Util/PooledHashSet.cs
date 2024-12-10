using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace BXGeometryGraph
{
    class PooledHashSet<T> : HashSet<T>, IDisposable
    {
        private static Stack<PooledHashSet<T>> s_Pool = new Stack<PooledHashSet<T>>();
        private bool m_Active;

        PooledHashSet()
        {

        }

        public static PooledHashSet<T> Get()
        {
            if(s_Pool.Count == 0)
            {
                return new PooledHashSet<T> { m_Active = true };
            }

            var list = s_Pool.Pop();
            list.m_Active = true;
#if DEBUG
            GC.ReRegisterForFinalize(list);
#endif
            return list;
        }

        public void Dispose()
        {
            Assert.IsTrue(m_Active);
            m_Active = false;
            Clear();
            s_Pool.Push(this);
#if DEBUG
            GC.SuppressFinalize(this);
#endif
        }

        // Destructor causes some GC alloc so only do this sanity check in debug build
#if DEBUG
        ~PooledHashSet()
        {
            throw new InvalidOperationException($"{nameof(PooledHashSet<T>)} must be disposed manually.");
        }

#endif
    }
}
