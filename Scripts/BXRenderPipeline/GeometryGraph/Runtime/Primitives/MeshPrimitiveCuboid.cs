using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
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
		internal static (JobHandle, MeshData) create_cuboid_mesh(float3 size, int verts_x, int verts_y, int verts_z, JobHandle dependsOn = default)
		{
			CuboidConfig config = new CuboidConfig(size, verts_x, verts_y, verts_z);

			MeshData mesh = new MeshData(config.vertex_count, 0, config.face_count, config.loop_count);

			create_cuboid_mesh_job job = new create_cuboid_mesh_job()
			{
				config = config,
				positions = mesh.positions,
				face_offset_indices = mesh.face_offset_indices,
				corner_verts = mesh.corner_verts
			};
			JobHandle jobHandle = job.Schedule(dependsOn);

			return (jobHandle, mesh);
		}

		[BurstCompile]
        private struct create_cuboid_mesh_job : IJob
        {
			[ReadOnly]
			public CuboidConfig config;

			[WriteOnly]
			public NativeArray<float3> positions;
			[WriteOnly]
			public NativeArray<int> face_offset_indices;
			[WriteOnly]
			public NativeArray<int> corner_verts;

            public void Execute()
            {
				// mesh_smooth_set

				calculate_positions(ref config, ref positions);
				offset_indices.fill_constant_grouo_size(4, 0, ref face_offset_indices);
				calculate_corner_verts(ref config, ref corner_verts);
			}
        }

        [BurstCompile]
		private static void calculate_positions(ref CuboidConfig config, ref NativeArray<float3> positions)
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
				if (utils.ELEM(z, 0, config.edges_z))
				{
					// Fill Bottom and Top
					float z_pos = z_bottom + z_delta * z;
					for(int y = 0; y < config.verts_y; ++y)
                    {
						float y_pos = y_front + y_delta * y;
						for(int x = 0; x < config.verts_x; ++x)
                        {
							float x_pos = x_left + x_delta * x;
							positions[vert_index++] = new float3(x_pos, y_pos, z_pos);
                        }
                    }
				}
                else
                {
					for(int y = 0; y < config.verts_y; ++y)
                    {
						if(utils.ELEM(y, 0, config.verts_y))
                        {
							// Fill y-sides
							float y_pos = y_front + y_delta * y;
							float z_pos = z_bottom + z_delta * z;
							for(int x = 0; x < config.verts_x; ++x)
                            {
								float x_pos = x_left + x_delta * x;
								positions[vert_index++] = new float3(x_pos, y_pos, z_pos);
                            }
                        }
                        else
                        {
							// Fill x-sides
							float x_pos = x_left;
							float y_pos = y_front + y_delta * y;
							float z_pos = z_bottom + z_delta * z;
							positions[vert_index++] = new float3(x_pos, y_pos, z_pos);
							float x_pos2 = x_left + x_delta * config.edges_x;
							positions[vert_index++] = new float3(x_pos2, y_pos, z_pos);
                        }
                    }
                }
			}
		}

		[BurstCompile]
		private static void calculate_corner_verts(ref CuboidConfig config, ref NativeArray<int> corner_verts)
        {
			int loop_index = 0;

			// Number of vertices in an XY cross-section of the cube (barring top and bottom faces)
			int xy_cross_section_vert_count = config.verts_x * config.verts_y - (config.verts_x - 2) * (config.verts_y - 2);

			// Calculate faces for Bottom faces
			int vert_1_start = 0;

			for(int y = 0; y < config.edges_y; ++y)
            {
				for(int x = 0; x < config.edges_x; ++x)
                {
					int vert_1 = vert_1_start + x;
					int vert_2 = vert_1_start + config.verts_x + x;
					int vert_3 = vert_2 + 1;
					int vert_4 = vert_1 + 1;

					define_quad(ref corner_verts, loop_index, vert_1, vert_2, vert_3, vert_4);
					loop_index += 4;
                }
				vert_1_start += config.verts_x;
            }

			// Calculate faces for Front faces
			vert_1_start = 0;
			int vert_2_start = config.verts_x * config.verts_y;

			for(int z = 0; z < config.edges_z; ++z)
            {
				for(int x = 0; x < config.edges_x; ++x)
                {
					define_quad(ref corner_verts, loop_index, vert_1_start + x, vert_1_start + x + 1, vert_2_start + x + 1, vert_2_start + x);
					loop_index += 4;
                }
				vert_1_start = vert_2_start;
				vert_2_start += config.verts_x * config.verts_y - (config.verts_x - 2) * (config.verts_y - 2);
            }

			// Calculate faces for Top faces
			vert_1_start = config.verts_x * config.verts_y + (config.verts_z - 2) * (config.verts_x * config.verts_y - (config.verts_x - 2) * (config.verts_y - 2));
			vert_2_start = vert_1_start + config.verts_x;

			for(int y = 0; y < config.edges_y; ++y)
            {
				for(int x = 0; x < config.edges_x; ++x)
                {
					define_quad(ref corner_verts, loop_index, vert_1_start + x, vert_1_start + x + 1, vert_2_start + x + 1, vert_2_start + x);
					loop_index += 4;
                }
				vert_2_start += config.verts_x;
				vert_1_start += config.verts_x;
            }

			// Calculate faces for Back faces
			vert_1_start = config.verts_x * config.edges_y;
			vert_2_start = vert_1_start + xy_cross_section_vert_count;

			for(int z = 0; z < config.edges_z; ++z)
            {
				if(z == (config.edges_z - 1))
                {
					vert_2_start += (config.verts_x - 2) * (config.verts_y - 2);
                }
				for(int x = 0; x < config.edges_x; ++x)
                {
					define_quad(ref corner_verts, loop_index, vert_1_start + x, vert_2_start + x, vert_2_start + x + 1, vert_1_start + x + 1);
					loop_index += 4;
                }
				vert_2_start += xy_cross_section_vert_count;
				vert_1_start += xy_cross_section_vert_count;
            }

			// Calculate faces for left faces
			vert_1_start = 0;
			vert_2_start = config.verts_x * config.verts_y;

			for(int z = 0; z < config.edges_z; ++z)
            {
				for(int y = 0; y < config.edges_y; ++y)
                {
					int vert_1;
					int vert_2;
					int vert_3;
					int vert_4;

					if(z == 0 || y == 0)
                    {
						vert_1 = vert_1_start + config.verts_x * y;
						vert_4 = vert_1 + config.verts_x;
                    }
                    else
                    {
						vert_1 = vert_1_start + 2 * y;
						vert_1 += config.verts_x - 2;
						vert_4 = vert_1 + 2;
                    }

					if(y == 0 || z == (config.edges_z - 1))
                    {
						vert_2 = vert_2_start + config.verts_x * y;
						vert_3 = vert_2 + config.verts_x;
                    }
                    else
                    {
						vert_2 = vert_2_start + 2 * y;
						vert_2 += config.verts_x - 2;
						vert_3 = vert_2 + 2;
                    }

					define_quad(ref corner_verts, loop_index, vert_1, vert_2, vert_3, vert_4);
					loop_index += 4;
				}
				if(z == 0)
                {
					vert_1_start += config.verts_x * config.verts_y;
                }
                else
                {
					vert_1_start += xy_cross_section_vert_count;
                }
				vert_2_start += xy_cross_section_vert_count;
            }

			// Calculate faces for Right faces
			vert_1_start = config.edges_x;
			vert_2_start = vert_1_start + config.verts_x * config.verts_y;

			for(int z = 0; z < config.edges_z; ++z)
            {
				for(int y = 0; y < config.edges_y; ++y)
                {
					int vert_1 = vert_1_start;
					int vert_2 = vert_2_start;
					int vert_3 = vert_2_start + 2;
					int vert_4 = vert_1 + config.verts_x;

					if(z == 0)
                    {
						vert_1 = vert_1_start + config.verts_x * y;
						vert_4 = vert_1 + config.verts_x;
                    }
                    else
                    {
						vert_1 = vert_1_start + 2 * y;
						vert_4 = vert_1 + 2;
                    }

					if(z == (config.edges_z - 1))
                    {
						vert_2 = vert_2_start + config.verts_x * y;
						vert_3 = vert_2 + config.verts_x;
                    }
                    else
                    {
						vert_2 = vert_2_start + 2 * y;
						vert_3 = vert_2 * 2;
                    }

					if(y == (config.edges_y - 1))
                    {
						vert_3 = vert_2 + config.verts_x;
						vert_4 = vert_1 + config.verts_x;
                    }

					define_quad(ref corner_verts, loop_index, vert_1, vert_4, vert_3, vert_2);
					loop_index += 4;
                }
				if(z == 0)
                {
					vert_1_start += config.verts_x * config.verts_y;
                }
                else
                {
					vert_1_start += xy_cross_section_vert_count;
                }
				vert_2_start += xy_cross_section_vert_count;
            }
		}

		/// <summary>
		/// vert_1 = bottom left, vert_2 = bottom right, vert_3 = top right, vert_4 = top left.
		/// Hence they are passed as 1,4,3,2 when calculating faces clockwise, and 1,2,3,4 for anti-clockwise.
		/// </summary>
		/// <param name="corner_verts"></param>
		/// <param name="loop_index"></param>
		/// <param name="vert_1"></param>
		/// <param name="vert_2"></param>
		/// <param name="vert_3"></param>
		/// <param name="vert_4"></param>
		[BurstCompile]
		private static void define_quad(ref NativeArray<int> corner_verts, int loop_index, int vert_1, int vert_2, int vert_3, int vert_4)
        {
			corner_verts[loop_index] = vert_1;
			corner_verts[loop_index + 1] = vert_2;
			corner_verts[loop_index + 2] = vert_3;
			corner_verts[loop_index + 3] = vert_4;
		}
	}
}
