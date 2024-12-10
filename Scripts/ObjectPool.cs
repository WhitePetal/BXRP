using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace BXCommon
{
    /// <summary>
    /// Generic object pool.
    /// </summary>
    /// <typeparam name="T">Type of the object pool.</typeparam>
    public class ObjectPool<T> where T : new()
    {
        private readonly Stack<T> m_Stack = new Stack<T>();
        private readonly UnityAction<T> m_ActionOnGet;
        private readonly UnityAction<T> m_ActionOnRelease;
        private readonly bool m_CollectionCheck = true;

        /// <summary>
        /// Number of objects in the pool.
        /// </summary>
        public int countAll { get; private set; }
        /// <summary>
        /// Number of inactive objects in the pool.
        /// </summary>
        public int countInactive { get { return m_Stack.Count; } }
        /// <summary>
        /// Number of active objects in the pool.
        /// </summary>
        public int countActive { get { return countAll - countInactive; } }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="actionOnGet">Action on get.</param>
        /// <param name="actionOnRelease">Action on release.</param>
        /// <param name="collectionCheck">True if collection integrity should be checked.</param>
        public ObjectPool(UnityAction<T> actionOnGet, UnityAction<T> actionOnRelease, bool collectionCheck = true)
        {
            m_ActionOnGet = actionOnGet;
            m_ActionOnRelease = actionOnRelease;
            m_CollectionCheck = collectionCheck;
        }

        /// <summary>
        /// Get an object from the pool.
        /// </summary>
        /// <returns>A new object from the pool.</returns>
        public T Get()
        {
            T element;
            if(m_Stack.Count == 0)
            {
                element = new T();
                ++countAll;
            }
            else
            {
                element = m_Stack.Pop();
            }
            if (m_ActionOnGet != null)
                m_ActionOnGet(element);

            return element;
        }

        public struct PooledObject : IDisposable
        {
            private readonly T m_ToReturn;
            private readonly ObjectPool<T> m_Pool;

            internal PooledObject(T value, ObjectPool<T> pool)
            {
                m_ToReturn = value;
                m_Pool = pool;
            }

            /// <summary>
            /// Disposable pattern implementation.
            /// </summary>
            void IDisposable.Dispose() => m_Pool.Release(m_ToReturn);
        }

        /// <summary>
        /// Get et new PooledObject.
        /// </summary>
        /// <param name="v">Output new typed object.</param>
        /// <returns>New PooledObject</returns>
        public PooledObject Get(out T v) => new PooledObject(v = Get(), this);

        /// <summary>
        /// Release an object to the pool.
        /// </summary>
        /// <param name="element">Object to release.</param>
        public void Release(T element)
        {
#if UNITY_EDITOR // keep heavy checks in editor
            if (m_CollectionCheck && m_Stack.Count > 0)
            {
                if (m_Stack.Contains(element))
                    Debug.LogError("Internal error. Trying to destroy object that is already released to pool.");
            }
#endif
            if (m_ActionOnRelease != null)
                m_ActionOnRelease(element);
            m_Stack.Push(element);
        }
    }

    /// <summary>
    /// List Pool.
    /// </summary>
    /// <typeparam name="T">Type of the objects in the pooled lists.</typeparam>
    public static class ListPool<T>
    {
        // Object pool to avoid allocations.
        private static readonly ObjectPool<List<T>> s_Pool = new ObjectPool<List<T>>(null, l => l.Clear());

        /// <summary>
        /// Get a new List
        /// </summary>
        /// <returns>A new List</returns>
        public static List<T> Get() => s_Pool.Get();

        /// <summary>
        /// Get a new list PooledObject.
        /// </summary>
        /// <param name="value">Output typed List.</param>
        /// <returns>A new List PooledObject.</returns>
        public static ObjectPool<List<T>>.PooledObject Get(out List<T> value) => s_Pool.Get(out value);

        /// <summary>
        /// Release an object to the pool.
        /// </summary>
        /// <param name="toRelease">List to release.</param>
        public static void Release(List<T> toRelease) => s_Pool.Release(toRelease);
    }
}
