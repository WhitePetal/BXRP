using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;

namespace BXGeometryGraph
{
    struct ShaderStringMapping
    {
        public AbstractGeometryNode node { get; set; }
        //        public List<AbstractMaterialNode> nodes { get; set; }
        public int startIndex { get; set; }
        public int count { get; set; }
    }

    class ShaderStringBuilder : IDisposable
    {
        enum ScopeType
        {
            Indent,
            Block,
            BlockSemicolon
        }

        private StringBuilder m_StringBuilder;
        private Stack<ScopeType> m_ScopeStack;
        private int m_IndentationLevel;
        private ShaderStringMapping m_CurrentMapping;
        private List<ShaderStringMapping> m_Mappings;
        private bool m_HumanReadable;

        private const string k_IndentationString = "    ";
        private const string k_NewLineString = "\n";

        internal AbstractGeometryNode currentNode
        {
            get { return m_CurrentMapping.node; }
            set
            {
                m_CurrentMapping.count = m_StringBuilder.Length - m_CurrentMapping.startIndex;
                if (m_CurrentMapping.count > 0)
                    m_Mappings.Add(m_CurrentMapping);
                m_CurrentMapping.node = value;
                m_CurrentMapping.startIndex = m_StringBuilder.Length;
                m_CurrentMapping.count = 0;
            }
        }

        internal List<ShaderStringMapping> mappings
        {
            get { return m_Mappings; }
        }

        public ShaderStringBuilder(int indentationLevel = 0, int stringBuilderSize = 8192, bool humanReadable = false)
        {
            IncreaseIndent(indentationLevel);
            m_StringBuilder = new StringBuilder(stringBuilderSize);
            m_ScopeStack = new Stack<ScopeType>();
            m_Mappings = new List<ShaderStringMapping>();
            m_CurrentMapping = new ShaderStringMapping();
            m_HumanReadable = humanReadable;
        }

        public void AppendNewLine()
        {
            m_StringBuilder.Append(k_NewLineString);
        }

        private void AppendLine(string value, int startIndex, int count)
        {
            if (value.Length > 0)
            {
                TryAppendIndentation();
                m_StringBuilder.Append(value, startIndex, count);
            }
            AppendNewLine();
        }

        public void AppendLine(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                TryAppendIndentation();
                m_StringBuilder.Append(value);
            }
            AppendNewLine();
        }

        public void Append(string value)
        {
            m_StringBuilder.Append(value);
        }

        public void Append(string value, int start, int count)
        {
            m_StringBuilder.Append(value, start, count);
        }

        [StringFormatMethod("formatString")]
        public void Append(string formatString, params object[] args)
        {
            m_StringBuilder.AppendFormat(formatString, args);
        }

        public void AddLine(string v0) { TryAppendIndentation(); Append(v0); AppendNewLine(); }
        public void AddLine(string v0, string v1) { TryAppendIndentation(); Append(v0); Append(v1); AppendNewLine(); }
        public void AddLine(string v0, string v1, string v2) { TryAppendIndentation(); Append(v0); Append(v1); Append(v2); AppendNewLine(); }
        public void AddLine(string v0, string v1, string v2, string v3) { TryAppendIndentation(); Append(v0); Append(v1); Append(v2); Append(v3); AppendNewLine(); }
        public void AddLine(string v0, string v1, string v2, string v3, string v4) { TryAppendIndentation(); Append(v0); Append(v1); Append(v2); Append(v3); Append(v4); AppendNewLine(); }
        public void AddLine(string v0, string v1, string v2, string v3, string v4, string v5) { TryAppendIndentation(); Append(v0); Append(v1); Append(v2); Append(v3); Append(v4); Append(v5); AppendNewLine(); }
        public void AddLine(string v0, string v1, string v2, string v3, string v4, string v5, string v6) { TryAppendIndentation(); Append(v0); Append(v1); Append(v2); Append(v3); Append(v4); Append(v5); Append(v6); AppendNewLine(); }
        public void AddLine(string v0, string v1, string v2, string v3, string v4, string v5, string v6, string v7) { TryAppendIndentation(); Append(v0); Append(v1); Append(v2); Append(v3); Append(v4); Append(v5); Append(v6); Append(v7); AppendNewLine(); }

        [StringFormatMethod("formatString")]
        public void AppendLine(string formatString, params object[] args)
        {
            TryAppendIndentation();
            m_StringBuilder.AppendFormat(CultureInfo.InvariantCulture, formatString, args);
            AppendNewLine();
        }

        static readonly char[] LineSeparators = new[] { '\n', '\r' };
        public void AppendLines(string lines)
        {
            if (string.IsNullOrEmpty(lines))
                return;

            int startSearchIndex = 0;
            int indexOfNextBreak = lines.IndexOfAny(LineSeparators);
            while (indexOfNextBreak >= 0)
            {
                AppendLine(lines, startSearchIndex, indexOfNextBreak - startSearchIndex);
                startSearchIndex = indexOfNextBreak + 1;
                indexOfNextBreak = lines.IndexOfAny(LineSeparators, startSearchIndex);
            }

            if (startSearchIndex < lines.Length)
            {
                AppendLine(lines, startSearchIndex, lines.Length - startSearchIndex);
            }
        }

        public void TryAppendIndentation()
        {
            if (m_HumanReadable)
            {
                for (var i = 0; i < m_IndentationLevel; i++)
                    m_StringBuilder.Append(k_IndentationString);
            }
        }

        public IDisposable IndentScope()
        {
            m_ScopeStack.Push(ScopeType.Indent);
            IncreaseIndent();
            return this;
        }

        public void IncreaseIndent()
        {
            m_IndentationLevel++;
        }

        public void IncreaseIndent(int level)
        {
            for (var i = 0; i < level; i++)
                IncreaseIndent();
        }

        public void DecreaseIndent()
        {
            m_IndentationLevel--;
        }

        public void DecreaseIndent(int level)
        {
            for (var i = 0; i < level; i++)
                DecreaseIndent();
        }

        public string ToCodeBlock()
        {
            // Remove new line
            if (m_StringBuilder.Length > 0)
                m_StringBuilder.Length = m_StringBuilder.Length - 1;

            if (m_HumanReadable)
            {
                // Set indentations
                m_StringBuilder.Replace(Environment.NewLine, Environment.NewLine + k_IndentationString);
            }

            return m_StringBuilder.ToString();
        }

        public void Dispose()
        {
            if (m_ScopeStack.Count == 0)
                return;

            switch (m_ScopeStack.Pop())
            {
                case ScopeType.Indent:
                    DecreaseIndent();
                    break;
                case ScopeType.Block:
                    DecreaseIndent();
                    AppendLine("}");
                    break;
                case ScopeType.BlockSemicolon:
                    DecreaseIndent();
                    AppendLine("};");
                    break;
            }
        }
    }
}
