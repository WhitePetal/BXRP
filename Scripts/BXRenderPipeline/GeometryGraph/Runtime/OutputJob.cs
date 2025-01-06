using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;

namespace BXGeometryGraph.Runtime
{
    [Serializable]
    public unsafe class OutputJobManaged : AbstractGeometryJob
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

        public override JobHandle Schedule(JobHandle dependsOn = default)
        {
            if (depenedJobs == null || depenedJobs.Length <= 0)
                return dependsOn;


            for(int i = 0; i < depenedJobs.Length; ++i)
            {
                dependsOn = depenedJobs[i].Schedule(dependsOn);
            }
            return dependsOn;
        }

        public override JobHandle WriteResultToGeoData(GeometryData* geoData, JobHandle dependsOn = default)
        {
            for (int i = 0; i < depenedJobs.Length; ++i)
            {
                dependsOn = depenedJobs[i].WriteResultToGeoData(geoData, dependsOn);
            }
            return dependsOn;
        }

        public override void Dispose()
        {
            if (depenedJobs == null || depenedJobs.Length <= 0)
                return;


            for (int i = 0; i < depenedJobs.Length; ++i)
            {
                depenedJobs[i].Dispose();
            }
        }
    }
}
