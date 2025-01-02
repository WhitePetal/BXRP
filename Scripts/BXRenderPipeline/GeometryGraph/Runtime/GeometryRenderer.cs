using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph.Runtime
{
    public class GeometryRenderer : MonoBehaviour
    {
        [SerializeField]
        private GeometrySO sharedGeometrySO;

        internal GeometrySO m_GeometrySO;

        public GeometrySO geometrySO
        {
            get
            {
                if (m_GeometrySO == sharedGeometrySO || m_GeometrySO == null)
                {
                    m_GeometrySO = Instantiate<GeometrySO>(sharedGeometrySO);
                    m_GeometrySO.ClearStates();
                }
                return m_GeometrySO;
            }
        }

        private void Awake()
        {
            m_GeometrySO = sharedGeometrySO;
            m_GeometrySO.Deserialize();
            Debug.Log("GeometryRenderer Test: " + (m_GeometrySO.innerData.ouputJob as OutputJobManaged).testSerialize);
        }

        public void Schedule()
        {
            m_GeometrySO.Schedule();
        }

        public ref GeometryData Compelete()
        {
            m_GeometrySO.Compelete();
            return ref m_GeometrySO.innerData.geometryData;
        }
    }
}