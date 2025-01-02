using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BXGeometryGraph.Runtime;

namespace BXGeometryGraph
{
    interface IGenerateGeometryJob
    {
        AbstractGeometryJob BuildGeometryJob();
    }
}
