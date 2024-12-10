using System.Collections;
using System.Collections.Generic;
using BXGraphing;
using UnityEngine;

namespace BXGeometryGraph
{
    public interface IGeometryInputObserver
    {
        /// <summary>
        /// This interface is implemented by any entity that wants to be made aware of updates to a shader input
        /// </summary>
        interface IShaderInputObserver
        {
            void OnShaderInputUpdated(ModificationScope modificationScope);
        }
    }
}
