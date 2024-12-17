using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    [Serializable]
    public class FakeJsonObject
    {
        [SerializeField]
        private string m_Type;

        [SerializeField]
        private string m_ObjectId;

        public string id
        {
            get
            {
                return m_ObjectId;
            }
            set
            {
                m_ObjectId = value;
            }
        }

        public string type
        {
            get { return m_Type; }
            set { m_Type = value; }
        }

        public void Reset()
        {
            m_ObjectId = null;
            m_Type = null;
        }
    }
}
