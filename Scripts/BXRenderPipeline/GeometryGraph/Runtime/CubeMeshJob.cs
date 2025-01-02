using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace BXGeometryGraph.Runtime
{
	[Serializable]
	public class CubeMeshJobManaged : AbstractGeometryJob
	{
		[SerializeField]
		private ValueFrom m_SizeValueFrom;
		[SerializeField]
		private ValueFrom m_VerticesXValueFrom;
		[SerializeField]
		private ValueFrom m_VerticesYValueFrom;
		[SerializeField]
		private ValueFrom m_VerticesZValueFrom;

		[SerializeField]
		private int m_SizeValueID;
		[SerializeField]
		private int m_VerticesXValueID;
		[SerializeField]
		private int m_VerticesYValueID;
		[SerializeField]
		private int m_VerticesZValueID;

		[SerializeField]
		private float3 m_SizeValueDefault;
		[SerializeField]
		private int m_VerticesXValueDefault;
		[SerializeField]
		private int m_VerticesYValueDefault;
		[SerializeField]
		private int m_VerticesZValueDefault;

		private float3 size;
		private int verticesX;
		private int verticesY;
		private int verticesZ;

		private int meshIndex;

		public CubeMeshJobManaged(string nodeGuid, ValueFrom sizeValueFrom, ValueFrom verticesXValueFrom, ValueFrom verticesYValueFrom, ValueFrom verticesZValueFrom,
			int sizeValueID = 0, int verticesXValueID = 0, int verticesYValueID = 0, int verticesZValueID = 0,
			float3 sizeValueDefault = default, int verticesXValueDefault = 0, int verticesYValueDefault = 0, int verticesZValueDefault = 0)
		{
			this.nodeGuid = nodeGuid;	
			m_SizeValueFrom = sizeValueFrom;
			m_VerticesXValueFrom = verticesXValueFrom;
			m_VerticesYValueFrom = verticesYValueFrom;
			m_VerticesZValueFrom = verticesZValueFrom;

			m_SizeValueID = sizeValueID;
			m_VerticesXValueID = verticesXValueID;
			m_VerticesYValueID = verticesYValueID;
			m_VerticesZValueID = verticesZValueID;

			m_SizeValueDefault = sizeValueDefault;
			m_VerticesXValueDefault = verticesXValueDefault;
			m_VerticesYValueDefault = verticesYValueDefault;
			m_VerticesZValueDefault = verticesZValueDefault;
		}

		public CubeMeshJobManaged()
        {

        }

		public override JobHandle Schedule(ref GeometryData geoData, JobHandle dependsOn = default)
		{
			size = m_SizeValueFrom == ValueFrom.Default ? m_SizeValueDefault : depenedJobs[0].GetFloat3(m_SizeValueID);
			verticesX = m_VerticesXValueFrom == ValueFrom.Default ? m_VerticesXValueDefault : depenedJobs[1].GetInt(m_VerticesXValueID);
			verticesY = m_VerticesYValueFrom == ValueFrom.Default ? m_VerticesYValueDefault : depenedJobs[2].GetInt(m_VerticesYValueID);
			verticesZ = m_VerticesZValueFrom == ValueFrom.Default ? m_VerticesZValueDefault : depenedJobs[3].GetInt(m_VerticesZValueID);

			meshIndex = geoData.meshs.Length;

			if (depenedJobs != null && depenedJobs.Length > 0)
			{
				for (int i = 0; i < depenedJobs.Length; ++i)
				{
					dependsOn = JobHandle.CombineDependencies(dependsOn, depenedJobs[i].Schedule(ref geoData));
				}
			}

			JobHandle jobHandle;
			MeshData meshData;
			int dimensions = ((verticesX - 1) > 0 ? 1 : 0) + ((verticesY - 1) > 0 ? 1 : 0) + ((verticesZ - 1) > 0 ? 1 : 0);

			if (dimensions == 0)
			{
				(jobHandle, meshData) = MeshPrimitiveLine.create_line_mesh(float3.zero, float3.zero, 1, dependsOn);
			}
            else if(dimensions == 1)
            {
				float3 start;
				float3 delta;
				if (verticesX > 1)
				{
					start = new float3(-size.x * 0.5f, 0f, 0f);
					delta = new float3(size.x / (verticesX - 1), 0f, 0f);
				}
				else if (verticesY > 1)
				{
					start = new float3(0f, -size.y * 0.5f, 0f);
					delta = new float3(0f, size.y / (verticesY - 1), 0f);
				}
				else
				{
					start = new float3(0f, 0f, -size.z * 0.5f);
					delta = new float3(0f, 0f, size.z / (verticesZ - 1));
				}

				(jobHandle, meshData) = MeshPrimitiveLine.create_line_mesh(start, delta, verticesX * verticesY * verticesZ, dependsOn);
			}
            else if(dimensions == 2)
            {
				// XY Plane
				if (verticesZ <= 1)
				{
					(jobHandle, meshData) = MeshPrimitiveGrid.create_grid_mesh(verticesX, verticesY, size.x, size.y, dependsOn);
				}
				else if (verticesY <= 1)
				{
					(jobHandle, meshData) = MeshPrimitiveGrid.create_grid_mesh(verticesX, verticesZ, size.x, size.z, dependsOn);
					// TODO
					// transform_mesh
				}
                else
                {
					(jobHandle, meshData) = MeshPrimitiveGrid.create_grid_mesh(verticesZ, verticesY, size.z, size.y, dependsOn);
					// TODO
					// transform_mesh
                }
			}
            else
            {
				(jobHandle, meshData) = MeshPrimitiveCuboid.create_cuboid_mesh(size, verticesX, verticesY, verticesZ, dependsOn);
			}

			geoData.meshs.Add(meshData);

			return jobHandle;
		}

		public override void WriteResultToGeoData(ref GeometryData geoData)
		{
			throw new System.NotImplementedException();
		}
    }
}
