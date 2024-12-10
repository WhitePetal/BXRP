using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    [SerializeField]
    public class ContextData
    {
        [SerializeField]
        private Vector2 m_Position;

        [SerializeField]
        private List<JsonRef<BlockNode>> m_Blocks = new List<JsonRef<BlockNode>>();
    }
}
