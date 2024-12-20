using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace BXGeometryGraph
{
    public interface INodeModificationListener
    {
        void OnNodeModified(ModificationScope scope);
    }
}
