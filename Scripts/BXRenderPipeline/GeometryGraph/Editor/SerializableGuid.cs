using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace BXGeometryGraph
{
    [Serializable]
    public class SerializableGuid : ISerializationCallbackReceiver
    {
        [NonSerialized]
        private Guid m_Guid;

        [SerializeField]
        private string m_GuidSerialized;

        public SerializableGuid()
        {
            m_Guid = Guid.NewGuid();
        }

        public SerializableGuid(Guid guid)
        {
            m_Guid = guid;
        }

        public Guid guid
        {
            get { return m_Guid; }
            internal set { m_Guid = value; } // allow id to be overwritten when necessary
        }

        public void OnBeforeSerialize()
        {
            m_GuidSerialized = m_Guid.ToString();
        }

        public void OnAfterDeserialize()
        {
            if (string.IsNullOrEmpty(m_GuidSerialized))
                m_Guid = Guid.NewGuid();
            else
                m_Guid = new Guid(m_GuidSerialized);
        }

    }
}
