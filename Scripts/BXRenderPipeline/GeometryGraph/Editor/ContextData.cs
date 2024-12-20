using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    [Serializable]
    sealed class ContextData
    {
        [SerializeField]
        private Vector2 m_Position;

        [SerializeField]
        private List<JsonRef<BlockNode>> m_Blocks = new List<JsonRef<BlockNode>>();

        [NonSerialized]
        private GeometryStage m_GeometryStage;

        public ContextData()
        {
        }

        public List<JsonRef<BlockNode>> blocks => m_Blocks;

        public Vector2 position
        {
            get => m_Position;
            set => m_Position = value;
        }

        public GeometryStage geometryStage
        {
            get => m_GeometryStage;
            set => m_GeometryStage = value;
        }
    }
}
