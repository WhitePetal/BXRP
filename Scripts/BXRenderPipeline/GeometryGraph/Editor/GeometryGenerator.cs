using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BXGeometryGraph
{
    public class GeometryGenerator
    {
        private struct GeometryChunk
        {
            public GeometryChunk(int indentLevel, string geometryChunkString)
            {
                m_IndentLevel = indentLevel;
                m_GeometryChunkString = geometryChunkString;
            }

            private readonly int m_IndentLevel;
            private readonly string m_GeometryChunkString;

            public int chunkIndentLevel
            {
                get { return m_IndentLevel; }
            }

            public string chunkString
            {
                get { return m_GeometryChunkString; }
            }
        }

        private readonly List<GeometryChunk> m_GeometryChunks = new List<GeometryChunk>();
        private int m_IndentLevel;
        private string m_Pragma = string.Empty;

        public void AddGeometryChunk(string s, bool unique)
        {
            if (string.IsNullOrEmpty(s))
                return;

            if (unique && m_GeometryChunks.Any(x => x.chunkString == s))
                return;

            m_GeometryChunks.Add(new GeometryChunk(m_IndentLevel, s));
        }


        private const string kErrorString = @"ERROR!";

        public static string AdaptNodeOutput(AbstractGeometryNode node, int outputSlotId, ConcreteSlotValueType convertToType)
        {
            var outputSlot = node.FindOutputSlot<GeometrySlot>(outputSlotId);

            if (outputSlot == null)
                return kErrorString;

            var convertFromType = outputSlot.concreteValueType;
            var rawOutput = node.GetVariableNameForSlot(outputSlotId);
            if (convertFromType == convertToType)
                return rawOutput;

            switch (convertToType)
            {
                case ConcreteSlotValueType.Vector1:
                    return string.Format("({0}).x", rawOutput);
                case ConcreteSlotValueType.Vector2:
                    switch (convertFromType)
                    {
                        case ConcreteSlotValueType.Vector1:
                            return string.Format("({0}.xx)", rawOutput);
                        case ConcreteSlotValueType.Vector3:
                        case ConcreteSlotValueType.Vector4:
                            return string.Format("({0}.xy)", rawOutput);
                        default:
                            return kErrorString;
                    }
                case ConcreteSlotValueType.Vector3:
                    switch (convertFromType)
                    {
                        case ConcreteSlotValueType.Vector1:
                            return string.Format("({0}.xxx)", rawOutput);
                        case ConcreteSlotValueType.Vector2:
                            return string.Format("({0}3({1}, 0.0))", node.precision, rawOutput);
                        case ConcreteSlotValueType.Vector4:
                            return string.Format("({0}.xyz)", rawOutput);
                        default:
                            return kErrorString;
                    }
                case ConcreteSlotValueType.Vector4:
                    switch (convertFromType)
                    {
                        case ConcreteSlotValueType.Vector1:
                            return string.Format("({0}.xxxx)", rawOutput);
                        case ConcreteSlotValueType.Vector2:
                            return string.Format("({0}4({1}, 0.0, 1.0))", node.precision, rawOutput);
                        case ConcreteSlotValueType.Vector3:
                            return string.Format("({0}4({1}, 1.0))", node.precision, rawOutput);
                        default:
                            return kErrorString;
                    }
                case ConcreteSlotValueType.Matrix3:
                    return rawOutput;
                case ConcreteSlotValueType.Matrix2:
                    return rawOutput;
                default:
                    return kErrorString;
            }
        }
    }

}