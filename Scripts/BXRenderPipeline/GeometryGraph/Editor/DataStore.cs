using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    class DataStore<T>
    {
        Action<T, IGraphDataAction> m_Reducer;
        internal T State { get; private set; }

        public Action<T, IGraphDataAction> Subscribe;

        internal DataStore(Action<T, IGraphDataAction> reducer, T initialState)
        {
            m_Reducer = reducer;
            State = initialState;
        }

        public void Dispatch(IGraphDataAction action)
        {
            m_Reducer(State, action);
            // Note: This would only work with reference types, as value types would require creating a new copy, this works given that we use GraphData which is a heap object
            // Notifies any listeners about change in state
            Subscribe?.Invoke(State, action);
        }
    }
}
