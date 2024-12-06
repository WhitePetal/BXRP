using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGraphing
{
    public interface IGraphObject
    {
        IGraph graph { get; set; }
        void RegisterCompleteObjectUndo(string name);
    }
}
