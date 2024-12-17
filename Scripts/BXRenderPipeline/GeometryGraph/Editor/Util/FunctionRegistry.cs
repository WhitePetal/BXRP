using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    class FunctionSource
    {
        public string code;
        public HashSet<AbstractGeometryNode> nodes;
        public bool isGeneric;
        public int graphPrecisionFlags;     // Flags<GraphPrecision>
        public int concretePrecisionFlags;  // Flags<ConcretePrecision>
    }

    class FunctionRegistry
    {
        private Dictionary<string, FunctionSource> m_Sources = new Dictionary<string, FunctionSource>();
        private bool m_Validate = false;
        private ShaderStringBuilder m_Builder;
        private IncludeCollection m_Includes;

        public FunctionRegistry(ShaderStringBuilder builder, IncludeCollection includes, bool validate = false)
        {
            m_Builder = builder;
            m_Includes = includes;
            m_Validate = validate;
        }

        internal ShaderStringBuilder builder => m_Builder;

        public Dictionary<string, FunctionSource> sources => m_Sources;

        public void RequiresIncludes(IncludeCollection includes)
        {
            // TODO
            //m_Includes.Add(includes);
        }

        public void RequiresIncludePath(string includePath, bool shouldIncludeWithPragmas = false)
        {
            // TODO
            //m_Includes.Add(includePath, IncludeLocation.Graph, shouldIncludeWithPragmas);
        }

        // this list is somewhat redundant, but it preserves function declaration ordering
        // (i.e. when nodes add multiple functions, they require being defined in a certain order)
        public List<string> names { get; } = new List<string>();

        public void ProvideFunction(string name, GraphPrecision graphPrecision, ConcretePrecision concretePrecision, Action<ShaderStringBuilder> generator)
        {
            // TODO
            // appends code, construct the standalone code string
            //var originalIndex = builder.length;
            //builder.AppendNewLine();
        }
    }
}
