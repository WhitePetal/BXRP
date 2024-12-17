using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    internal interface IGeneratesShaderBodyCode
    {
        void GenerateNodeShaderCode(ShaderStringBuilder visitor, GenerationMode generationMode);
    }
}