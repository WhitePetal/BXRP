#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace BXRenderPipeline.OcclusionCulling
{
    public partial class OcclusionRootComponent
    {
        private const int k_BaseSampleCount = 512;

        private static float RadicalInverse(int Base, int i)
		{
            float Digity, Radical, Inverse;
            Digity = Radical = 1.0f / Base;
            Inverse = 0f;
			while (i > 0)
			{
                // i 余 Base 求出 i 在 "Base" 进制下的最低位数
                Inverse += Digity * (i % Base);
                Digity *= Radical;

                // i 除以 Base 即可求右一位的数
                i /= Base;
			}
            return Inverse;
		}

        private bool IsPrime(int num)
		{
            if (num <= 1)
                return false;
            for(int i = 2; i < num; ++i)
			{
                if (num % i == 0) return false;
			}
            return true;
		}

        private int PrimeNum(int n)
		{
            int k = 0;
            int i = 2;
            while (k < n)
			{
				while (true)
				{
                    if (IsPrime(i))
                        break;
                    ++i;
				}
                ++k;
            }
            return i;
		}

        private static float Halton(int Prime, int index)
		{
            return RadicalInverse(Prime, index);
		}
        private float Hammersley(int Prime, int PrePrime, int index, int count)
		{
            if (Prime == 2)
                return index * 1f / count;
            else
                return RadicalInverse(PrePrime, index);
		}

        private static readonly uint[] FaurePremutation5 = new uint[]
        {
            0, 75, 50, 25, 100, 15, 90, 65, 40, 115, 10, 85, 60, 35, 110, 5, 80, 55,
            30, 105, 20, 95, 70, 45, 120, 3, 78, 53, 28, 103, 18, 93, 68, 43, 118, 13, 88, 63, 38, 113, 8, 83, 58, 33, 108,
            23, 98, 73, 48, 123, 2, 77, 52, 27, 102, 17, 92, 67, 42, 117, 12, 87, 62, 37, 112, 7, 82, 57, 32, 107, 22, 97,
            72, 47, 122, 1, 76, 51, 26, 101, 16, 91, 66, 41, 116, 11, 86, 61, 36, 111, 6, 81, 56, 31, 106, 21, 96, 71, 46,
            121, 4, 79, 54, 29, 104, 19, 94, 69, 44, 119, 14, 89, 64, 39, 114, 9, 84, 59, 34, 109, 24, 99, 74, 49, 124
        };
        private static readonly uint[] FaurePremutation7 = new uint[]
        {
            0,  98,  245, 147, 49, 196, 294, 14, 112, 259, 161, 63, 210, 308,
            35, 133, 280, 182, 84, 231, 329, 21, 119, 266, 168, 70, 217, 315,
            7,  105, 252, 154, 56, 203, 301, 28, 126, 273, 175, 77, 224, 322,
            42, 140, 287, 189, 91, 238, 336, 2,  100, 247, 149, 51, 198, 296,
            16, 114, 261, 163, 65, 212, 310, 37, 135, 282, 184, 86, 233, 331,
            23, 121, 268, 170, 72, 219, 317, 9,  107, 254, 156, 58, 205, 303,
            30, 128, 275, 177, 79, 226, 324, 44, 142, 289, 191, 93, 240, 338,
            5,  103, 250, 152, 54, 201, 299, 19, 117, 264, 166, 68, 215, 313,
            40, 138, 285, 187, 89, 236, 334, 26, 124, 271, 173, 75, 222, 320,
            12, 110, 257, 159, 61, 208, 306, 33, 131, 278, 180, 82, 229, 327,
            47, 145, 292, 194, 96, 243, 341, 3,  101, 248, 150, 52, 199, 297,
            17, 115, 262, 164, 66, 213, 311, 38, 136, 283, 185, 87, 234, 332,
            24, 122, 269, 171, 73, 220, 318, 10, 108, 255, 157, 59, 206, 304,
            31, 129, 276, 178, 80, 227, 325, 45, 143, 290, 192, 94, 241, 339,
            1,  99,  246, 148, 50, 197, 295, 15, 113, 260, 162, 64, 211, 309,
            36, 134, 281, 183, 85, 232, 330, 22, 120, 267, 169, 71, 218, 316,
            8,  106, 253, 155, 57, 204, 302, 29, 127, 274, 176, 78, 225, 323,
            43, 141, 288, 190, 92, 239, 337, 4,  102, 249, 151, 53, 200, 298,
            18, 116, 263, 165, 67, 214, 312, 39, 137, 284, 186, 88, 235, 333,
            25, 123, 270, 172, 74, 221, 319, 11, 109, 256, 158, 60, 207, 305,
            32, 130, 277, 179, 81, 228, 326, 46, 144, 291, 193, 95, 242, 340,
            6,  104, 251, 153, 55, 202, 300, 20, 118, 265, 167, 69, 216, 314,
            41, 139, 286, 188, 90, 237, 335, 27, 125, 272, 174, 76, 223, 321,
            13, 111, 258, 160, 62, 209, 307, 34, 132, 279, 181, 83, 230, 328,
            48, 146, 293, 195, 97, 244, 342
        };

        [BurstCompile]
        private static float Halton5(uint index)
		{
            // 0.9999999403953552f mean 0x1.fffffcp-1
            return (FaurePremutation5[index % 125u] * 1953125u + FaurePremutation5[(index / 125u) % 125u] * 15625u +
                FaurePremutation5[(index / 15625u) % 125u] * 125u + FaurePremutation5[(index / 1953125u) % 125u]) * (0.9999999403953552f / 244140625u);
		}
        [BurstCompile]
        private static float Halton7(uint index)
		{
            // 0.9999999403953552f mean 0x1.fffffcp-1
            return (FaurePremutation7[index % 343u] * 117649u + FaurePremutation7[(index / 343u) % 343u] * 343u +
                FaurePremutation7[(index / 117649u) % 343u]) * (0.9999999403953552f / 40353607u);
		}

        [BurstCompile]
        private struct FindCastJob : IJobParallelFor
		{
            [ReadOnly]
            public NativeArray<RaycastHit> hits;
            [ReadOnly]
            public int colliderInstanceID;

            [WriteOnly, NativeDisableParallelForRestriction]
            public NativeArray<bool> canSee;


            public void Execute(int index)
			{
                if (hits[index].colliderInstanceID == colliderInstanceID)
                {
                    canSee[0] = true;
                    return;
                }
            }
		}

        [BurstCompile]
        private struct GenerateSamplePointsJob : IJobParallelFor
		{
            [ReadOnly]
            public AABB aabb;

            [WriteOnly, NativeDisableParallelForRestriction]
            public NativeArray<float3> samplePoints;

            public void Execute(int index)
			{
                int samplePointIndex = index * 6;
                float3 size = aabb.Size;
                for (int axis = 0; axis < 3; ++axis)
                {
                    switch (axis)
                    {
                        case 0:
                            {
                                float3 point0 = aabb.Min;
                                point0.y = point0.y + Halton5((uint)samplePointIndex) * size.y;
                                point0.z = point0.z + Halton7((uint)samplePointIndex) * size.z;
                                samplePoints[samplePointIndex++] = point0;

                                float3 point1 = aabb.Max;
                                point1.y = point1.y - Halton5((uint)samplePointIndex) * size.y;
                                point1.z = point1.z - Halton7((uint)samplePointIndex) * size.z;
                                samplePoints[samplePointIndex++] = point1;
                            }
                            break;
                        case 1:
                            {
                                float3 point0 = aabb.Min;
                                point0.x = point0.x + Halton5((uint)samplePointIndex) * size.x;
                                point0.z = point0.z + Halton7((uint)samplePointIndex) * size.z;
                                samplePoints[samplePointIndex++] = point0;

                                float3 point1 = aabb.Max;
                                point1.x = point1.x - Halton5((uint)samplePointIndex) * size.x;
                                point1.z = point1.z - Halton7((uint)samplePointIndex) * size.z;
                                samplePoints[samplePointIndex++] = point1;
                            }
                            break;
                        case 2:
                            {
                                float3 point0 = aabb.Min;
                                point0.x = point0.x + Halton5((uint)samplePointIndex) * size.x;
                                point0.y = point0.y + Halton7((uint)samplePointIndex) * size.y;
                                samplePoints[samplePointIndex++] = point0;

                                float3 point1 = aabb.Max;
                                point1.x = point1.x - Halton5((uint)samplePointIndex) * size.x;
                                point1.y = point1.y - Halton7((uint)samplePointIndex) * size.y;
                                samplePoints[samplePointIndex++] = point1;
                            }
                            break;
                    }
                }
            }
		}

        [BurstCompile]
        private struct InitRayCastDataJob : IJobParallelFor
		{
            [ReadOnly]
            public PhysicsScene physicScene;
            [ReadOnly]
            public NativeArray<float3> startSamplePoints;
            [ReadOnly]
            public NativeArray<float3> endSamplePoints;

            [WriteOnly]
            public NativeArray<RaycastCommand> raycastCmds;

            public void Execute(int index)
			{
                int i = index / startSamplePoints.Length;
                int j = index % startSamplePoints.Length;

                float3 start = startSamplePoints[i];
                float3 end = endSamplePoints[j];
                float3 dir = math.normalize(end - start);
                float dst = math.distance(start, end) * 2f;
                raycastCmds[index] = new RaycastCommand(physicScene, start, dir, QueryParameters.Default, dst);
            }
		}

        private bool baked = false;
        private bool baking = false;
        private int bakeCellGroup;

        private PhysicsScene physicScene;
        private Collider[] colliders;
        private List<OctreeNode> nodes;

        private const int batchCount = 32;
        private const int samplePointCountPerDim = 8;
        private NativeArray<float3>[] collidersSamplePoints;
        private NativeArray<float3>[] cellsSamplePoints;

        private bool CellColliderCast(
            NativeArray<float3> cellSamplePoints, NativeArray<float3> colliderSamplePoints,
            int colliderInstance
            )
		{
            int rayCastCount = cellSamplePoints.Length * colliderSamplePoints.Length;
            NativeArray<RaycastCommand> raycastCmds = new NativeArray<RaycastCommand>(rayCastCount, Allocator.TempJob);
            NativeArray<RaycastHit> raycastResult = new NativeArray<RaycastHit>(rayCastCount, Allocator.TempJob);
            InitRayCastDataJob initRayCastJob = new InitRayCastDataJob()
            {
                physicScene = physicScene,
                startSamplePoints = cellSamplePoints,
                endSamplePoints = colliderSamplePoints,
                raycastCmds = raycastCmds
            };
            JobHandle rayCastHandle = initRayCastJob.Schedule(rayCastCount, 64);
            rayCastHandle = RaycastCommand.ScheduleBatch(raycastCmds, raycastResult, 64, 1, rayCastHandle);
            NativeArray<bool> canSee = new NativeArray<bool>(1, Allocator.TempJob);
            FindCastJob findJob = new FindCastJob()
            {
                hits = raycastResult,
                colliderInstanceID = colliderInstance,
                canSee = canSee
            };
            findJob.Schedule(rayCastCount, 64, rayCastHandle).Complete();

            bool result = canSee[0];

            raycastCmds.Dispose();
            raycastResult.Dispose();
            canSee.Dispose();
            return result;
		}

        private void BakeBatch(int cellStart, int cellCount, int colliderStart, int colliderCount)
		{
            for (int i = 0; i < cellCount; ++i)
			{
                for (int j = 0; j < colliderCount; ++j)
                {
                    int colliderID = colliders[colliderStart + j].GetInstanceID();
                    if(CellColliderCast(cellsSamplePoints[i], collidersSamplePoints[j], colliderID))
					{
                        var node = nodes[cellStart + i];
                        if (node.m_Masks == null)
                            node.m_Masks = new List<int>();

                        int colliderIndex = colliderStart + j;
                        if (!node.m_Masks.Contains(colliderIndex)) node.m_Masks.Add(colliderIndex);
					}
                }
            }
		}

        private void BakeStep()
		{
            if(bakeCellGroup * batchCount >= nodes.Count + batchCount)
            {
                for (int i = 0; i < batchCount; ++i)
                {
                    collidersSamplePoints[i].Dispose();
                    cellsSamplePoints[i].Dispose();
                }
                baking = false;
                baked = true;
                bakeCellGroup = 0;
                UnityEditor.EditorUtility.ClearProgressBar();
                return;
            }

            UnityEditor.EditorUtility.DisplayProgressBar("Baking...", $"Group Index: {bakeCellGroup}", bakeCellGroup * batchCount * 1f / (nodes.Count + batchCount));
            int cellIndex = bakeCellGroup * batchCount;
            int cellStart = cellIndex;
            int cellCount = (cellIndex + batchCount) >= nodes.Count ? (nodes.Count - cellIndex) : batchCount;
            JobHandle cellSamplePointGenHandle = default;
            for (int k = 0; k < batchCount && cellIndex < nodes.Count; ++k, ++cellIndex)
            {
                GenerateSamplePointsJob genPointJob = new GenerateSamplePointsJob()
                {
                    aabb = nodes[cellIndex].m_AABB,
                    samplePoints = cellsSamplePoints[k]
                };
                cellSamplePointGenHandle = JobHandle.CombineDependencies(cellSamplePointGenHandle, genPointJob.Schedule(samplePointCountPerDim, 1));
            }
            for (int colliderGroup = 0; colliderGroup * batchCount < colliders.Length + batchCount; ++colliderGroup)
            {
                int colliderIndex = colliderGroup * batchCount;
                int colliderStart = colliderIndex;
                int colliderCount = (colliderIndex + batchCount) >= colliders.Length ? (colliders.Length - colliderIndex) : batchCount;
                JobHandle colliderSamplePointGenHandle = default;
                for (int k = 0; k < batchCount && colliderIndex < colliders.Length; ++k, ++colliderIndex)
                {
                    GenerateSamplePointsJob genPointJob = new GenerateSamplePointsJob()
                    {
                        aabb = colliders[colliderIndex].bounds.ToAABB(),
                        samplePoints = collidersSamplePoints[k]
                    };
                    colliderSamplePointGenHandle = JobHandle.CombineDependencies(colliderSamplePointGenHandle, genPointJob.Schedule(samplePointCountPerDim, 1));
                }
                cellSamplePointGenHandle.Complete();
                colliderSamplePointGenHandle.Complete();

				BakeBatch(cellStart, cellCount, colliderStart, colliderCount);
			}
            ++bakeCellGroup;
        }


        public void Bake()
		{
            baked = false;
            colliders = GetComponentsInChildren<Collider>();
            physicScene = PhysicsSceneExtensions.GetPhysicsScene(gameObject.scene);

            collidersSamplePoints = new NativeArray<float3>[batchCount];
            cellsSamplePoints = new NativeArray<float3>[batchCount];
            for(int i = 0; i < batchCount; ++i)
			{
                collidersSamplePoints[i] = new NativeArray<float3>((samplePointCountPerDim + samplePointCountPerDim + samplePointCountPerDim) * 2, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                cellsSamplePoints[i] = new NativeArray<float3>((samplePointCountPerDim + samplePointCountPerDim + samplePointCountPerDim) * 2, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }

            Queue<OctreeNode> nodeQueue = new Queue<OctreeNode>();
            nodeQueue.Enqueue(m_TreeRoot);
            nodes = new List<OctreeNode>();
            while(nodeQueue.Count > 0)
			{
                var node = nodeQueue.Dequeue();
                if (node.m_Children != null && node.m_Children.Length > 0)
                {
                    for (int i = 0; i < node.m_Children.Length; ++i)
                    {
                        nodeQueue.Enqueue(node.m_Children[i]);
                    }
                    continue;
                }
                nodes.Add(node);
            }

            baking = true;
        }
    }
}
#endif