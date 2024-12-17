using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace BXGeometryGraph
{
    public class PropertyCollector
    {
        public struct TextureInfo
        {
            public string name;
            public int textureId;
            public TextureDimension dimension;
            public bool modifiable;
        }

        private bool m_ReadOnly;
        private List<HLSLProperty> m_HLSLProperties = null;

        // reference name ==> property index in list
        private Dictionary<string, int> m_ReferenceNames = new Dictionary<string, int>();

        // list of properties (kept in a list to maintain deterministic declaration order)
        private List<AbstractGeometryProperty> m_Properties = new List<AbstractGeometryProperty>();

        public int propertyCount => m_Properties.Count;
        public IEnumerable<AbstractGeometryProperty> properties => m_Properties;
        public AbstractGeometryProperty GetProperty(int index) { return m_Properties[index]; }

        public void Sort()
        {
            if (m_ReadOnly)
            {
                Debug.LogError("Cannot sort the properties when the PropertyCollector is already marked ReadOnly");
                return;
            }

            m_Properties.Sort((a, b) => String.CompareOrdinal(a.referenceName, b.referenceName));

            // reference name indices are now messed up, rebuild them
            m_ReferenceNames.Clear();
            for (int i = 0; i < m_Properties.Count; i++)
                m_ReferenceNames.Add(m_Properties[i].referenceName, i);
        }

        public void SetReadOnly()
        {
            m_ReadOnly = true;
        }

        private static bool EquivalentHLSLProperties(AbstractGeometryProperty a, AbstractGeometryProperty b)
        {
            bool equivalent = true;
            var bHLSLProps = new List<HLSLProperty>();
            b.ForeachHLSLProperty(bh => bHLSLProps.Add(bh));
            a.ForeachHLSLProperty(ah =>
            {
                var i = bHLSLProps.FindIndex(bh => bh.name == ah.name);
                if (i < 0)
                    equivalent = false;
                else
                {
                    var bh = bHLSLProps[i];
                    if (!ah.ValueEquals(bh))
                        equivalent = false;
                    bHLSLProps.RemoveAt(i);
                }
            });
            return equivalent && (bHLSLProps.Count == 0);
        }

        public void AddGeometryProperty(AbstractGeometryProperty prop)
        {
            if (m_ReadOnly)
            {
                Debug.LogError("ERROR attempting to add property to readonly collection");
                return;
            }

            int propIndex = -1;
            if(m_ReferenceNames.TryGetValue(prop.referenceName, out propIndex))
            {
                // existing referenceName
                var existingProp = m_Properties[propIndex];
                if(existingProp != prop)
                {
                    // duplicate reference name, but different property instances
                    if(existingProp.GetType() != prop.GetType())
                    {
                        Debug.LogError("Two properties with the same reference name (" + prop.referenceName + ") using different types");
                    }
                    else
                    {
                        if(!EquivalentHLSLProperties(existingProp, prop))
                            Debug.LogError("Two properties with the same reference name (" + prop.referenceName + ") produce different HLSL properties");
                    }
                }
            }
            else
            {
                // new referenceName, new property
                propIndex = m_Properties.Count;
                m_Properties.Add(prop);
                m_ReferenceNames.Add(prop.referenceName, propIndex);
            }
        }
    }
}
