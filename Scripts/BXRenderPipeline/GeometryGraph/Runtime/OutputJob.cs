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

        public override JobHandle Schedule(ref GeometryData geoData, JobHandle dependsOn = default)
        {
            if (depenedJobs == null || depenedJobs.Length <= 0)
                return dependsOn;


            for(int i = 0; i < depenedJobs.Length; ++i)
            {
                dependsOn = JobHandle.CombineDependencies(dependsOn, depenedJobs[i].Schedule(ref geoData));
            }
            return dependsOn;
        }

        public override void WriteResultToGeoData(ref GeometryData geoData)
        {
            throw new System.NotImplementedException();
        }
    }
}
