using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace BXGeometryGraph.Runtime
{
	[Serializable]
    public class SetPositionJob : AbstractGeometryJob
    {
		[SerializeField]
		private ValueFrom m_GeometryValueFrom;
		[SerializeField]
		private ValueFrom m_SelectionValueFrom;
		[SerializeField]
		private ValueFrom m_PositionValueFrom;
		[SerializeField]
		private ValueFrom m_OffsetValueFrom;

		[SerializeField]
		private int m_GeometryValueID;
		[SerializeField]
		private int m_SelectionValueID;
		[SerializeField]
		private int m_PositionValueID;
		[SerializeField]
		private int m_OffsetValueID;

		[SerializeField]
		private bool m_SelectionValueDefault;
		[SerializeField]
		private float3 m_PositionValueDefault;
		[SerializeField]
		private float3 m_OffsetValueDefault;

		private bool selection;
		private float3 position;
		private float3 offset;

		private GeometryData geo;

		public SetPositionJob(string nodeGuid, ValueFrom geometryValueFrom, ValueFrom selectionValueFrom, ValueFrom positionValueFrom, ValueFrom offsetValueFrom,
			int geometryValueID = -1, int selectionValueID = -1, int positionValueID = -1, int offsetValueID = -1,
			bool selectionValueDefault = false, float3 positionValueDefault = default, float3 offsetValueDefault = default)
		{
			this.nodeGuid = nodeGuid;
			m_GeometryValueFrom = geometryValueFrom;
			m_SelectionValueFrom = selectionValueFrom;
			m_PositionValueFrom = positionValueFrom;
			m_OffsetValueFrom = offsetValueFrom;

			m_GeometryValueID = geometryValueID;
			m_SelectionValueID = selectionValueID;
			m_PositionValueID = positionValueID;
			m_OffsetValueID = offsetValueID;

			m_SelectionValueDefault = selectionValueDefault;
			m_PositionValueDefault = positionValueDefault;
			m_OffsetValueDefault = offsetValueDefault;
		}

		public SetPositionJob()
		{

		}

		public override JobHandle Schedule(JobHandle dependsOn = default)
        {
			if (m_GeometryValueFrom != ValueFrom.DepenedJob)
				return new JobHandle();
			if (m_SelectionValueFrom == ValueFrom.Default && !m_SelectionValueDefault)
				return new JobHandle();

			if (depenedJobs != null && depenedJobs.Length > 0)
			{
				for (int i = 0; i < depenedJobs.Length; ++i)
				{
					if (depenedJobs[i] == null)
						continue;
					dependsOn = JobHandle.CombineDependencies(dependsOn, depenedJobs[i].Schedule());
				}
			}

			geo = depenedJobs[0].GetGeometry(m_GeometryValueID);
			JobHandle job = default;
			if(geo.points.IsCreated && geo.points.Length > 0)
            {
				SetPointsPosition setpointJob = new SetPointsPosition()
				{
					points = geo.points.AsArray(),
					time = owner.time
				};
				job = JobHandle.CombineDependencies(job, setpointJob.Schedule(dependsOn));
            }
			if(geo.meshs.IsCreated && geo.meshs.Length > 0)
            {
				for(int i = 0; i < geo.meshs.Length; ++i)
                {
					MeshData mesh = geo.meshs[i];
					SetPointsPosition setpointJob = new SetPointsPosition()
					{
						points = mesh.positions,
						time = owner.time
					};
					job = JobHandle.CombineDependencies(job, setpointJob.Schedule(dependsOn));
				}
            }
			
			return job;
        }

		[BurstCompile]
        struct SetPointsPosition : IJob
        {
			public NativeArray<float3> points;
			public float time;

            public void Execute()
            {
                for(int i = 0; i < points.Length; ++i)
                {
					points[i] += math.float3(0, 1, 0) * time;
                }
            }
        }

        public override unsafe JobHandle WriteResultToGeoData(GeometryData* geoData, JobHandle dependsOn = default)
        {
			return geo.AddToGeometry(geoData, dependsOn);
        }

        public override void Dispose()
        {
			base.Dispose();
			geo.Dispose();
        }
    }
}
