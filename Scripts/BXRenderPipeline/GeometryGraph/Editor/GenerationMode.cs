using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    public enum GenerationMode
    {
        Preview,
        ForReals,
        VFX
    }

    public static class GenerationModeExtensions
    {
        public static bool IsPreview(this GenerationMode mode) { return mode == GenerationMode.Preview; }
    }
}
