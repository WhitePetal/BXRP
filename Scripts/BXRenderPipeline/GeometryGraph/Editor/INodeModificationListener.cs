using System.Collections;
using System.Collections.Generic;
using BXGraphing;
using UnityEngine;

namespace BXGeometryGraph
{
    public interface INodeModificationListener
    {
        void OnNodeModified(ModificationScope scope);
    }
}
