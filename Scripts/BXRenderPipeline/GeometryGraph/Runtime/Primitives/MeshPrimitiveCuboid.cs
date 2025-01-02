using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace BXGeometryGraph.Runtime
{
    static class MeshPrimitiveCuboid
    {
		struct CuboidConfig
		{
			internal float3 size;
			internal int verts_x;
			internal int verts_y;
			internal int verts_z;
			internal int edges_x;
			internal int edges_y;
			internal int edges_z;
			internal int vertex_count;
			internal int face_count;
			internal int loop_count;

			public CuboidConfig(float3 size, int verts_x, int verts_y, int verts_z)
			{
				this.size = size;
				this.verts_x = verts_x;
				this.verts_y = verts_y;
				this.verts_z = verts_z;
				edges_x = verts_x - 1;
				edges_y = verts_y - 1;
				edges_z = verts_z - 1;
				vertex_count = 0;
				face_count = 0;
				loop_count = 0;

				vertex_count = get_vertex_count();
				face_count = get_face_count();
				loop_count = face_count * 4;
			}

			private int get_vertex_count()
			{
				int inner_position_count = (verts_x - 2) * (verts_y - 2) * (verts_z - 2);
				return verts_x * verts_y * verts_z - inner_position_count;
			}

			private int get_face_count()
			{
				return 2 * (edges_x * edges_y + edges_y * edges_z + edges_z * edges_x);
			}
		}

		[BurstCompile]
		internal static MeshData CreateCuboidMesh(float3 size, int verts_x, int verts_y, int verts_z)
		{
			CuboidConfig config = new CuboidConfig(size, verts_x, verts_y, verts_z);

			MeshData mesh = new MeshData(config.vertex_count, 0, config.face_count, config.loop_count);
			// mesh_smooth_set
			var positions = mesh.positions;
			var corner_verts = mesh.cornerVertices;

			return mesh;
		}

		private static void calculate_positions(CuboidConfig config, NativeArray<float3> positions)
		{
			float z_bottom = -config.size.x * 0.5f;
			float z_delta = config.size.z / config.edges_z;

			float x_left = -config.size.x * 0.5f;
			float x_delta = config.size.x / config.edges_x;

			float y_front = -config.size.y * 0.5f;
			float y_delta = config.size.y / config.edges_y;

			int vert_index = 0;

			for (int z = 0; z < config.verts_z; ++z)
			{
				if (z == 0 || z == config.edges_y)
				{

				}
			}
		}
	}
}
