#ifndef CUSTOME_UNITY_INPUT_INCLUDE
#define CUSTOME_UNITY_INPUT_INCLUDE

#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_I_V unity_MatrixInvV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_MATRIX_P glstate_matrix_projection
#define UNITY_PREV_MATRIX_M   unity_MatrixPreviousM
#define UNITY_PREV_MATRIX_I_M unity_MatrixPreviousMI

CBUFFER_START(UnityPerFrame)
    half4 glstate_lightmodel_ambient;
    half4 unity_AmbientSky;
    half4 unity_AmbientEquator;
    half4 unity_AmbientGround;
    half4 unity_IndirectSpecColorl;
    float4 unity_FogParams;
    half4 unity_FogColor;

    float4x4 glstate_matrix_projection;
    float4x4 unity_MatrixV;
    float4x4 unity_MatrixInvV;
    float4x4 unity_MatrixVP;
    float4 unity_StereoScaleOffset;
    int unity_StereoEyeIndex;

    half4 unity_ShadowColor;
    uint _TaaFrameIndex;
CBUFFER_END

CBUFFER_START(UnityPerCameraRare)
    float4 unity_CameraWorldClipPlanes[6];
    float4x4 unity_CameraProjection;
    float4x4 unity_CameraInvProjection;
    float4x4 unity_WorldToCamera;
    float4x4 unity_CameraToWorld;
    #if BX_DEFERRED
    float4x4 _ViewPortRays;
    #endif
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

    half _ReleateExpourse;
CBUFFER_END

#ifndef DOTS_INSTANCING_ON // UnityPerDraw cbuffer doesn't exist with hybrid renderer
CBUFFER_START(UnityPerDraw)
    float4x4 unity_ObjectToWorld;
    float4x4 unity_WorldToObject;
    float4 unity_LODFade;
    half4 unity_WorldTransformParams; // x is usually 1.0 or -1.0 for  odd-negative scale transforms
    half4 unity_RenderingLayer;

    half4 unity_LightData;
    half4 unity_LightIndices[2];

    half4 unity_ProbesOcclusion;
    
    // cube probe is deprecated, now use probe_atlas
    // float4 unity_SpecCube0_HDR;
    // float4 unity_SpecCube1_HDR;

    // float4 unity_SpecCube0_BoxMax;
    // float4 unity_SpecCube0_BoxMin;
    // float4 unity_SpecCube0_ProbePosition;

    // float4 unity_SpecCube1_BoxMax;
    // float4 unity_SpecCube1_BoxMin;
    // float4 unity_SpecCube1_ProbePosition;

    float4 unity_LightmapST;
    float4 unity_DynamicLightmapST;

    half4 unity_SHAr;
    half4 unity_SHAg;
    half4 unity_SHAb;
    half4 unity_SHBr;
    half4 unity_SHBg;
    half4 unity_SHBb;
    half4 unity_SHC;

    // Renderer bounding box
    float4 unity_RendererBounds_Min;
    float4 unity_RendererBounds_Max;

    // Velocity
    float4x4 unity_MatrixPreviousM;
    float4x4 unity_MatrixPreviousMI;
    //X : Use last frame positions (right now skinned meshes are the only objects that use this
    //Y : Force No Motion
    //Z : Z bias value
    //W : Camera only
    float4 unity_MotionVectorsParams;

    // Sprite.
    half4 unity_SpriteColor;
    //X : FlipX
    //Y : FlipY
    //Z : Reserved for future use.
    //W : Reserved for future use.
    half4 unity_SpriteProps;

    // Light Probe Proxy Volume is deprecated in the latest srp
    // x = Disabled(0)/Enable(1)
    // y = Computation are done in global space(0) or local space(1)
    // z = Texel size on U texture coordinate
    // float4 unity_ProbeVolumeParams;
    // float4x4 unity_ProbeVolumeWorldToObject;
    // float3 unity_ProbeVolumeSizeInv;
    // float3 unity_ProbeVolumeMin;
CBUFFER_END
#endif

#endif