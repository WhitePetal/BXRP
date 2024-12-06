using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    public interface IGenerateProperties
    {
        void CollectGeometryProperties(PropertyCollector collector, GenerationMode generationMode);
    }
}
