using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    [Serializable]
    public struct GeometryGraphRequirements
    {
        [SerializeField] bool m_RequiresGeometry;

        internal static GeometryGraphRequirements none
        {
            get
            {
                return new GeometryGraphRequirements();
            }
        }

        public bool requiresGeometry
        {
            get { return m_RequiresGeometry; }
            internal set { m_RequiresGeometry = value; }
        }
    }
}
