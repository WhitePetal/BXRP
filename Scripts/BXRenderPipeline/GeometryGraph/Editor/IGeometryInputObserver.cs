using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace BXGeometryGraph
{
    /// <summary>
    /// This interface is implemented by any entity that wants to be made aware of updates to a shader input
    /// </summary>
    interface IGeometryInputObserver
    {
        void OnGeometryInputUpdated(ModificationScope modificationScope);
    }
}
