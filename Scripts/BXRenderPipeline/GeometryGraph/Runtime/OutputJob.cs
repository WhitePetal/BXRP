using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;

namespace BXGeometryGraph.Runtime
{
    [Serializable]
    public class OutputJobManaged : AbstractGeometryJob
    {
        [SerializeField]
        private ValueFrom m_GeometryValueFrom;

        [SerializeField]
        private int m_GeometryValueID;


        public OutputJobManaged(string nodeGuid, ValueFrom geometryValueFrom, int geometryValueID)
        {
            this.nodeGuid = nodeGuid;
            m_GeometryValueFrom = geometryValueFrom;
            m_GeometryValueID = geometryValueID;
        }

        public OutputJobManaged()
        {

        }

        public override T GetOutput<T>(int outputId)
        {
            throw new System.NotImplementedException();
        }

        public override JobHandle Schedule(ref GeometryData geoData, JobHandle dependsOn = default)
        {
            throw new System.NotImplementedException();
        }

        public override void WriteResultToGeoData(ref GeometryData geoData)
        {
            throw new System.NotImplementedException();
        }
    }
}
