using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    [System.Serializable]
    struct SlotReference : IEquatable<SlotReference>, IComparable<SlotReference>
    {
        [SerializeField]
        private JsonRef<AbstractGeometryNode> m_Node;

        [SerializeField]
        private int m_SlotId;


        public SlotReference(AbstractGeometryNode node, int slotId)
        {
            m_Node = node;
            m_SlotId = slotId;
        }

        public AbstractGeometryNode node
        {
            get { return m_Node; }
        }

        public int slotId
        {
            get { return m_SlotId; }
        }

        public GeometrySlot slot => m_Node.value?.FindSlot<GeometrySlot>(m_SlotId);

        public bool Equals(SlotReference other)
        {
            return m_SlotId == other.m_SlotId && m_Node.value == other.m_Node.value;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj.GetType() == GetType() && Equals((SlotReference)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (m_SlotId * 397) ^ m_Node.GetHashCode();
            }
        }

        public int CompareTo(SlotReference other)
        {
            var nodeIdComparison = m_Node.value.objectId.CompareTo(other.m_Node.value.objectId);
            if (nodeIdComparison != 0)
            {
                return nodeIdComparison;
            }

            return m_SlotId.CompareTo(other.m_SlotId);
        }
    }
}
