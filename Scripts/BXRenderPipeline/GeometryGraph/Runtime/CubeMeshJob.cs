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

		[BurstCompile]
        public struct CubeMeshJob : IJobParallelFor
		{
			[ReadOnly]
			public float3 size;
			[ReadOnly]
			public int verticesX;
			[ReadOnly]
			public int verticesY;
			[ReadOnly]
			public int verticesZ;

			public void Execute(int index)
			{
				int dimensions = ((verticesX - 1) > 0 ? 1 : 0) + ((verticesY - 1) > 0 ? 1 : 0) + ((verticesZ - 1) > 0 ? 1 : 0);
				if(dimensions == 0)
                {
					MeshPrimitiveLine.CreateLineMesh(float3.zero, float3.zero, 1);
					return;
                }
				if(dimensions == 1)
                {
					float3 start;
					float3 delta;
					if(verticesX > 1)
                    {
						start = new float3(-size.x * 0.5f, 0f, 0f);
						delta = new float3(size.x / (verticesX - 1), 0f, 0f);
                    }
					else if(verticesY > 1)
                    {
						start = new float3(0f, -size.y * 0.5f, 0f);
						delta = new float3(0f, size.y / (verticesY - 1), 0f);
					}
                    else
                    {
						start = new float3(0f, 0f, -size.z * 0.5f);
						delta = new float3(0f, 0f, size.z / (verticesZ - 1));
					}

					MeshPrimitiveLine.CreateLineMesh(start, delta, verticesX * verticesY * verticesZ);
					return;
                }
				if(dimensions == 2)
                {
					// XY Plane
					if(verticesZ == 1)
                    {
						MeshPrimitiveGrid.CreateGridMesh(verticesX, verticesY, size.x, size.y);
						return;
                    }
					if(verticesY == 1)
                    {
						MeshPrimitiveGrid.CreateGridMesh(verticesX, verticesZ, size.x, size.z);
						// TODO
						// transform_mesh
						return;
					}
					MeshPrimitiveGrid.CreateGridMesh(verticesZ, verticesY, size.z, size.y);
					// TODO
					// transform_mesh
					return;
				}

				MeshPrimitiveCuboid.CreateCuboidMesh(size, verticesX, verticesY, verticesZ);
				return;
			}
		}

		public override JobHandle Schedule(ref GeometryData geoData, JobHandle dependsOn = default)
		{
			CubeMeshJob job = new CubeMeshJob()
			{
				size = m_SizeValueFrom == ValueFrom.Default ? m_SizeValueDefault : depenedJobs[0].GetOutput<float3>(m_SizeValueID),
				verticesX = m_SizeValueFrom == ValueFrom.Default ? m_VerticesXValueDefault : depenedJobs[1].GetOutput<int>(m_VerticesXValueID),
				verticesY = m_SizeValueFrom == ValueFrom.Default ? m_VerticesYValueDefault : depenedJobs[2].GetOutput<int>(m_VerticesYValueID),
				verticesZ = m_SizeValueFrom == ValueFrom.Default ? m_VerticesZValueDefault : depenedJobs[3].GetOutput<int>(m_VerticesZValueID),
			};
			return job.Schedule(1, 1);
		}

		public override void WriteResultToGeoData(ref GeometryData geoData)
		{
			throw new System.NotImplementedException();
		}

        public override T GetOutput<T>(int outputId)
        {
            throw new NotImplementedException();
        }
    }
}
