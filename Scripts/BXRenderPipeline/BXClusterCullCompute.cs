using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace BXRenderPipeline
{
    public class BXClusterCullCompute : BXClusterCullBase
    {
        protected static class ThisShaderProperties
        {
            public static readonly int _ClusterLightMaxBounds_ID = Shader.PropertyToID("_ClusterLightMaxBounds");
            public static readonly int _ClusterLightMinBounds_ID = Shader.PropertyToID("_ClusterLightMinBounds");

            public static readonly int _TileLBStart_ID = Shader.PropertyToID("_TileLBStart");
            public static readonly int _TileRBStart_ID = Shader.PropertyToID("_TileRBStart");
            public static readonly int _TileLUStart_ID = Shader.PropertyToID("_TileLUStart");
            public static readonly int _TileRVec_ID = Shader.PropertyToID("_TileRVec");
        }

        private ComputeBuffer clusterLightingIndicesBuffer;
        private ComputeBuffer clusterLightingDatasBuffer;

        public BXClusterCullCompute()
        {
            clusterLightingIndicesBuffer = new ComputeBuffer(tileCountX * tileCountY * clusterCountZ * BXLightsBase.maxClusterLightCount, sizeof(uint) * 2, ComputeBufferType.Default);
            clusterLightingDatasBuffer = new ComputeBuffer(tileCountX * tileCountY * clusterCountZ, sizeof(uint) * 2, ComputeBufferType.Default);
        }

        public override void Render(Camera camera, BXLightsBase lights, BXRenderCommonSettings commonSettings, int width, int height)
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
            commandBuffer.SetGlobalVector(BaseShaderProperties._ClusterSize_ID, clusterSize);

            //cmd.SetGlobalInt(BXShaderPropertyIDs._SupportClusterLight_ID, 1);

            commandBuffer.SetComputeIntParam(commonSettings.clusterLightCompute, BXLightsBase.BaseShaderProperties._ClusterLightCount_ID, lights.clusterLightCount);
            commandBuffer.SetComputeVectorArrayParam(commonSettings.clusterLightCompute, ThisShaderProperties._ClusterLightMaxBounds_ID, lights.clusterLightMaxBounds);
            commandBuffer.SetComputeVectorArrayParam(commonSettings.clusterLightCompute, ThisShaderProperties._ClusterLightMinBounds_ID, lights.clusterLightMinBounds);

            commandBuffer.SetComputeIntParam(commonSettings.clusterLightCompute, BXReflectionProbeManager.ShaderProperties.bx_ReflProbes_Count_ID, lights.reflectProneCount);
            commandBuffer.SetComputeVectorArrayParam(commonSettings.clusterLightCompute, BXReflectionProbeManager.ShaderProperties.bx_ReflProbes_BoxMin_ID, lights.reflectProbeMinBounds);
            commandBuffer.SetComputeVectorArrayParam(commonSettings.clusterLightCompute, BXReflectionProbeManager.ShaderProperties.bx_ReflProbes_BoxMax_ID, lights.reflectProbeMaxBounds);

            commandBuffer.SetComputeVectorParam(commonSettings.clusterLightCompute, BXShaderPropertyIDs._CameraPosition_ID, cam_trans.position);
            commandBuffer.SetComputeVectorParam(commonSettings.clusterLightCompute, BXShaderPropertyIDs._CmaeraUpward_ID, cameraUpward);
            commandBuffer.SetComputeVectorParam(commonSettings.clusterLightCompute, BXShaderPropertyIDs._CameraForward_ID, cameraForward);
            commandBuffer.SetComputeVectorParam(commonSettings.clusterLightCompute, BXShaderPropertyIDs._CameraRightword_ID, cameraRightward);
            commandBuffer.SetComputeVectorParam(commonSettings.clusterLightCompute, ThisShaderProperties._TileLBStart_ID, tileLB);
            commandBuffer.SetComputeVectorParam(commonSettings.clusterLightCompute, ThisShaderProperties._TileRBStart_ID, tileRB);
            commandBuffer.SetComputeVectorParam(commonSettings.clusterLightCompute, ThisShaderProperties._TileLUStart_ID, tileLU);
            commandBuffer.SetComputeVectorParam(commonSettings.clusterLightCompute, ThisShaderProperties._TileRVec_ID, cameraRightward * nearPlaneXHalf * 2f);
            commandBuffer.SetComputeVectorParam(commonSettings.clusterLightCompute, BaseShaderProperties._ClusterSize_ID, clusterSize);
            commandBuffer.SetComputeVectorParam(commonSettings.clusterLightCompute, BXShaderPropertyIDs._ProjectionParams_ID, new Vector4(farPlane, nearPlane));
            commandBuffer.SetComputeBufferParam(commonSettings.clusterLightCompute, 0, BaseShaderProperties._ClusterLightingIndices_ID, clusterLightingIndicesBuffer);
            commandBuffer.SetComputeBufferParam(commonSettings.clusterLightCompute, 0, BaseShaderProperties._ClusterLightingDatas_ID, clusterLightingDatasBuffer);
            commandBuffer.DispatchCompute(commonSettings.clusterLightCompute, 0, tileCountX, tileCountY, clusterCountZ);

            commandBuffer.SetGlobalBuffer(BaseShaderProperties._ClusterLightingIndices_ID, clusterLightingIndicesBuffer);
            commandBuffer.SetGlobalBuffer(BaseShaderProperties._ClusterLightingDatas_ID, clusterLightingDatasBuffer);
            Graphics.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
        }

        public override void Dispose()
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
