using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace BXRenderPipeline
{
    public class BXClusterLightCullCompute : IDisposable
    {
        private const string BufferName = "ClusterLightCulling";
        private CommandBuffer commandBuffer = new CommandBuffer()
        {
            name = BufferName
        };

        private ComputeBuffer clusterLightingIndicesBuffer;
        private ComputeBuffer clusterLightingDatasBuffer;

        private const int tileCountX = 8;
        private const int tileCountY = 4;
        private int clusterCountZ;

        public BXClusterLightCullCompute()
        {
            clusterCountZ = 64;
            clusterLightingIndicesBuffer = new ComputeBuffer(tileCountX * tileCountY * clusterCountZ * BXLights.maxClusterLightCount, sizeof(uint), ComputeBufferType.Default);
            clusterLightingDatasBuffer = new ComputeBuffer(tileCountX * tileCountY * clusterCountZ, sizeof(uint), ComputeBufferType.Default);
        }

        private struct ClusterComputeJob : IJobFor
        {
            [ReadOnly]
            public int clusterCountZ, clusterLightCount;
            [ReadOnly]
            public float nearPlane;
            [ReadOnly]
            public Vector4 cameraPostion, cameraForward, cameraUpward, tileLBStart, tileRBStart, tileLUStart, clusterSize, tileRVec;

            [ReadOnly]
            public NativeArray<Vector4> minBounds;
            [ReadOnly]
            public NativeArray<Vector4> maxBounds;

            // Unity默认只能读写 Exexute(index): index 下的数据以避免多线程读写冲突
            // 这里我们的写操作虽然会发生在index之外，但是可以保证是安全操作
            // 因此这里关闭安全检查
            [WriteOnly, NativeDisableContainerSafetyRestriction]
            public NativeArray<uint> clusterLightingIndices;
            [WriteOnly]
            public NativeArray<uint> clusterLightingDatas;

            public void Execute(int index)
            {
                int groupIds = index;
                int groupIdZ = groupIds / 32;
                int groupIdXY = groupIds % 32;
                int groupIdY = groupIdXY / 8;
                int groupIdX = groupIdXY % 8;

                int clusterIndex = groupIds;
                int startIndex = clusterIndex * 16;

                int lightCount = 0;
                for (int groupIndexs = 0; groupIndexs < 16; ++groupIndexs)
                {
                    if (groupIndexs >= clusterLightCount) break;
                    if (IntersectTileAndClusterLight(groupIdZ, groupIdX, groupIdY, minBounds[groupIndexs], maxBounds[groupIndexs]))
                    {
                        clusterLightingIndices[startIndex + lightCount] = (uint)groupIndexs;
                        lightCount++;
                    }
                }
                clusterLightingDatas[clusterIndex] = (uint)lightCount;
            }

            private bool IntersectTileAndClusterLight(int k, int groupIdX, int groupIdY, Vector4 minBox, Vector4 maxBox)
            {
                float zMin = nearPlane * Mathf.Pow(clusterSize.z, k);
                float zMax = zMin * clusterSize.z;

                float tileUV0_X = groupIdX / 8f;
                float tileUV0_Y = groupIdY / 4f;
                float tileUV1_X = (groupIdX + 1f) / 8f;
                float tileUV1_Y = (groupIdY + 1f) / 4f;
                Vector3 n = cameraForward;
                if (!AABBPlaneIntersect(n, -Vector3.Dot(cameraPostion + cameraForward * zMin, n), minBox, maxBox))
                {
                    return false;
                }
                n = -cameraForward;
                if (!AABBPlaneIntersect(n, -Vector3.Dot(cameraPostion + cameraForward * zMax, n), minBox, maxBox))
                {
                    return false;
                }
                Vector3 f0 = cameraUpward;
                Vector3 f1 = Vector3.Lerp(tileLBStart, tileRBStart, tileUV0_X);
                n = Vector3.Cross(f1, f0).normalized;
                if (!AABBPlaneIntersect(n, -Vector3.Dot(cameraPostion, n), minBox, maxBox))
                {
                    return false;
                }
                f1 = Vector3.Lerp(tileLBStart, tileRBStart, tileUV1_X);
                n = Vector3.Cross(f1, f0).normalized;
                if (!AABBPlaneIntersect(n, -Vector3.Dot(cameraPostion, n), minBox, maxBox))
                {
                    return false;
                }

                Vector3 rightV = tileRVec * tileUV0_X;

                f0 = Vector3.Lerp(tileLBStart, tileLUStart, tileUV0_Y);
                f1 = f0 + rightV;
                n = Vector3.Cross(f0, f1).normalized;
                if (!AABBPlaneIntersect(n, -Vector3.Dot(cameraPostion, n), minBox, maxBox))
                {
                    return false;
                }
                f0 = Vector3.Lerp(tileLBStart, tileLUStart, tileUV1_Y);
                f1 = f0 + rightV;
                n = Vector3.Cross(f1, f0).normalized;
                if (!AABBPlaneIntersect(n, -Vector3.Dot(cameraPostion, n), minBox, maxBox))
                {
                    return false;
                }
                return true;
            }

            private bool AABBPlaneIntersect(Vector3 n, float d, Vector4 minBox, Vector4 maxBox)
            {
                Vector3 boxReal = new Vector3(n.x > 0f ? maxBox.x : minBox.x, n.y > 0f ? maxBox.y : minBox.y, n.z > 0f ? maxBox.z : minBox.z);
                float r = Vector3.Dot(n, boxReal);
                return (r + d) >= 0;
            }
        }

        public void Render(Camera camera, BXLights lights, BXRenderCommonSettings commonSettings, int width, int height)
        {
            if (camera.orthographic || commonSettings.clusterLightCompute == null) return;
            Transform cam_trans = camera.transform;
            Vector3 cameraUpward = cam_trans.up;
            Vector3 cameraForward = cam_trans.forward;
            Vector3 cameraRightward = cam_trans.right;

            float cam_yf = Mathf.Tan(Mathf.Deg2Rad * camera.fieldOfView * 0.5f);
            float cam_xf = cam_yf * camera.aspect;

            float nearPlane = camera.nearClipPlane;
            float farPlane = camera.farClipPlane;
            float nearPlaneXHalf = cam_xf * nearPlane;
            float nearPlaneYHalf = cam_yf * nearPlane;
            Vector3 up = cameraUpward * nearPlaneYHalf;
            Vector3 right = cameraRightward * nearPlaneXHalf;
            Vector3 forward = cameraForward * nearPlane;

            Vector3 faddr = forward + right;
            Vector3 fsubr = forward - right;
            Vector3 tileLB = fsubr - up;
            Vector3 tileRB = faddr - up;
            Vector3 tileLU = fsubr + up;

            float clusterZNumFUnLog = 1f + 2 * cam_yf / tileCountY;
            float clusterZNumF = Mathf.Log(clusterZNumFUnLog);
            this.clusterCountZ = Mathf.CeilToInt(Mathf.Log(farPlane / nearPlane) / clusterZNumF);
            Vector4 clusterSize = new Vector4(width / tileCountX, height / tileCountY, clusterZNumFUnLog, 1f / clusterZNumF);
            commandBuffer.SetGlobalVector(BXShaderPropertyIDs._ClusterSize_ID, clusterSize);
            if (!commonSettings.supportComputeShader)
            {
                ClusterComputeJob job = new ClusterComputeJob();
                job.clusterCountZ = clusterCountZ;
                job.clusterLightCount = lights.clusterLightCount;
                job.nearPlane = nearPlane;
                job.cameraPostion = cam_trans.position;
                job.cameraForward = cameraForward;
                job.cameraUpward = cameraUpward;
                job.tileLBStart = tileLB;
                job.tileRBStart = tileRB;
                job.tileLUStart = tileLU;
                job.tileRVec = cameraRightward * nearPlaneXHalf * 2f;
                job.clusterSize = clusterSize;
                int clusterCount = tileCountX * tileCountY * clusterCountZ;
                job.minBounds = new NativeArray<Vector4>(lights.clusterLightMinBounds, Allocator.TempJob);
                job.maxBounds = new NativeArray<Vector4>(lights.clusterLightMaxBounds, Allocator.TempJob);
                job.clusterLightingIndices = new NativeArray<uint>(clusterCount * BXLights.maxClusterLightCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                job.clusterLightingDatas = new NativeArray<uint>(clusterCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                JobHandle tempHandle = new JobHandle();
                JobHandle jobHandle = job.ScheduleParallel(clusterCount, 8, tempHandle);
                jobHandle.Complete();
                clusterLightingIndicesBuffer.SetData(job.clusterLightingIndices);
                clusterLightingDatasBuffer.SetData(job.clusterLightingDatas);
                job.minBounds.Dispose();
                job.maxBounds.Dispose();
                job.clusterLightingIndices.Dispose();
                job.clusterLightingDatas.Dispose();
                //commandBuffer.SetGlobalInt(BXShaderPropertyIDs._SupportClusterLight_ID, 0);
                commandBuffer.SetGlobalBuffer(BXShaderPropertyIDs._ClusterLightingIndices_ID, clusterLightingIndicesBuffer);
                commandBuffer.SetGlobalBuffer(BXShaderPropertyIDs._ClusterLightingDatas_ID, clusterLightingDatasBuffer);
                Graphics.ExecuteCommandBuffer(commandBuffer);
                commandBuffer.Clear();
                return;
            }

            //cmd.SetGlobalInt(BXShaderPropertyIDs._SupportClusterLight_ID, 1);

            commandBuffer.SetComputeIntParam(commonSettings.clusterLightCompute, BXShaderPropertyIDs._ClusterLightCount_ID, lights.clusterLightCount);
            commandBuffer.SetComputeVectorArrayParam(commonSettings.clusterLightCompute, BXShaderPropertyIDs._ClusterLightMaxBounds_ID, lights.clusterLightMaxBounds);
            commandBuffer.SetComputeVectorArrayParam(commonSettings.clusterLightCompute, BXShaderPropertyIDs._ClusterLightMinBounds_ID, lights.clusterLightMinBounds);
            commandBuffer.SetComputeVectorParam(commonSettings.clusterLightCompute, BXShaderPropertyIDs._CameraPosition_ID, cam_trans.position);
            commandBuffer.SetComputeVectorParam(commonSettings.clusterLightCompute, BXShaderPropertyIDs._CmaeraUpward_ID, cameraUpward);
            commandBuffer.SetComputeVectorParam(commonSettings.clusterLightCompute, BXShaderPropertyIDs._CameraForward_ID, cameraForward);
            commandBuffer.SetComputeVectorParam(commonSettings.clusterLightCompute, BXShaderPropertyIDs._CameraRightword_ID, cameraRightward);
            commandBuffer.SetComputeVectorParam(commonSettings.clusterLightCompute, BXShaderPropertyIDs._TileLBStart_ID, tileLB);
            commandBuffer.SetComputeVectorParam(commonSettings.clusterLightCompute, BXShaderPropertyIDs._TileRBStart_ID, tileRB);
            commandBuffer.SetComputeVectorParam(commonSettings.clusterLightCompute, BXShaderPropertyIDs._TileLUStart_ID, tileLU);
            commandBuffer.SetComputeVectorParam(commonSettings.clusterLightCompute, BXShaderPropertyIDs._TileRVec_ID, cameraRightward * nearPlaneXHalf * 2f);
            commandBuffer.SetComputeVectorParam(commonSettings.clusterLightCompute, BXShaderPropertyIDs._ClusterSize_ID, clusterSize);
            commandBuffer.SetComputeVectorParam(commonSettings.clusterLightCompute, BXShaderPropertyIDs._ProjectionParams_ID, new Vector4(farPlane, nearPlane));
            commandBuffer.SetComputeBufferParam(commonSettings.clusterLightCompute, 0, BXShaderPropertyIDs._ClusterLightingIndices_ID, clusterLightingIndicesBuffer);
            commandBuffer.SetComputeBufferParam(commonSettings.clusterLightCompute, 0, BXShaderPropertyIDs._ClusterLightingDatas_ID, clusterLightingDatasBuffer);
            commandBuffer.DispatchCompute(commonSettings.clusterLightCompute, 0, tileCountX, tileCountY, clusterCountZ);

            commandBuffer.SetGlobalBuffer(BXShaderPropertyIDs._ClusterLightingIndices_ID, clusterLightingIndicesBuffer);
            commandBuffer.SetGlobalBuffer(BXShaderPropertyIDs._ClusterLightingDatas_ID, clusterLightingDatasBuffer);
            Graphics.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
        }

        public void Dispose()
        {
            commandBuffer.Dispose();
            commandBuffer = null;
            clusterLightingIndicesBuffer.Dispose();
            clusterLightingDatasBuffer.Dispose();
            clusterLightingIndicesBuffer = null;
            clusterLightingDatasBuffer = null;
        }
    }
}
