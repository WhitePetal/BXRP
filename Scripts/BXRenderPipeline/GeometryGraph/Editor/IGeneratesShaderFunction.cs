using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    interface IGeneratesShaderFunction
    {
        void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode);
    }
}
