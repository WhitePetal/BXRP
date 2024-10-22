#ifndef CUSTOME_UNITY_INPUT_INCLUDE
#define CUSTOME_UNITY_INPUT_INCLUDE

CBUFFER_START(UnityPerFrame)
    half4 glstate_lightmodel_ambient;
    half4 unity_AmbientSky;
    half4 unity_AmbientEquator;
    half4 unity_AmbientGround;
    half4 unity_IndirectSpecColorl;

    float4x4 unity_MatrixV;
    float4x4 unity_MatrixInvV;
    float4x4 unity_MatrixVP;
    float4x4 glstate_matrix_projection;
    int unity_StereoEyeIndex;

    half4 unity_ShadowColor;
CBUFFER_END

CBUFFER_STAR(UnityPerCameraRare)
    float4 unity_CameraWorldClipPlanes[6];

    float4x4 unity_CameraProjection;
    float4x4 unity_CameraInvProjection;
    float4x4 unity_WorldToCamera;
    float4x4 unity_CameraToWorld;
CBUFFER_END

CBUFFER_START(UnityPerCamera)
    float3 _WorldSpaceCameraPos;

    // t = time since current level load
    // x = t/20
    // y = t
    // z = t * 2
    // w = t * 3
    float4 _Time;
    half4 _SinTime; // sin(t/8), sin(t/4), sin(t/2), sin(t)
    half4 _CosTime; // cos(t/8), cos(t/4), cos(t/2), cos(t)
    float4 unity_DeltaTime; // dt, 1/dt, smoothdt, 1/smoothdt

    // x = 1 or -1 (-1 if projection is flipped)
    // y = near plane
    // z = far plane
    // w = 1 / far plane
    float4 _ProjectionParams;

    // Values used to linearize the z buffer (http://www.humus.name/temp/Linearize%20depth.txt)
    // x = 1-fra/near
    // y = far/near
    // z = x/far
    // w = y/far
    // or in case of a reversed depth buffer (UNITY_REVERSED_Z is 1)
    // x = -1+far/near
    // y = 1
    // z = x/far
    // w = 1/far
    float4 _ZBufferParams;

    // x = width
    // y = height
    // z = 1 + 1.0/width
    // w = 1 + 1.0/height
    float4 _ScreenParams;

    // x = orthographic camera's width
    // y = orthographic camera's height
    // z = unused
    // w = 1.0 if camera is ortho, 0.0 if perspective
    float4 unity_OrthoParams;
CBUFFER_END

CBUFFER_START(UnityPerDraw)
    float4x4 unity_ObjectToWorld;
    float4x4 unity_WorldToObject;
    float4 unity_LODFade;
    float4 unity_WorldTransformParams; // x is usually 1.0 or -1.0 for  odd-negative scale transforms
    float4 unity_RenderingLayer;
CBUFFER_END

CBUFFER_START(UnityLighting)
    // part of Light because it can be used outside of shadow distance
    half4 unity_OcclusionMaskSelector;
    half4 unity_ProbesOcclusion;

    half4 unity_SHAr;
    half4 unity_SHAg;
    half4 unity_SHAb;
    half4 unity_SHBr;
    half4 unity_SHBg;
    half4 unity_SHBb;
    half4 unity_SHC;
CBUFFER_END

CBUFFER_START(UnityProbeVolume)
    // x = Disabled(0)/Enable(1)
    // y = Computation are done in global space(0) or local space(1)
    // z = Texel size on U texture coordinate
    float4 unity_ProbeVolumeParams;

    float4x4 unity_ProbeVolumeWorldToObject;
    float3 unity_ProbeVolumeSizeInv;
    float3 unity_ProbeVolumeMin;
CBUFFER_END

CBUFFER_START(UnityReflectionProbes)
    float4 unity_SpecCube0_BoxMax;
    float4 unity_SpecCube0_BoxMin;
    float4 unity_SpecCube0_ProbePosition;
    half4 unity_SpecCube0_HDR;

    float4 unity_SpecCube1_BoxMax;
    float4 unity_SpecCube1_BoxMin;
    float4 unity_SpecCube1_ProbePosition;
    half4 unity_SpecCube1_HDR;
CBUFFER_END

CBUFFER_START(UnityLightMaps)
    float4 unity_LightmapsST;
    float4 unity_DynamicLightmapST;
CBUFFER_END

#endif