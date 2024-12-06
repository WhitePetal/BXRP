using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    public interface IGeneratesBodyCode
    {
        void GenerateNodeCode(GeometryGenerator visitor, GenerationMode generationMode);
    }
}