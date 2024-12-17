using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    [GenerationAPI]
    [Serializable]
    internal class IncludeCollection : IEnumerable<IncludeDescriptor>
    {
        [SerializeField]
        private List<IncludeDescriptor> includes;

        public IncludeCollection()
        {
            includes = new List<IncludeDescriptor>();
        }

        public IEnumerator<IncludeDescriptor> GetEnumerator()
        {
            return includes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
