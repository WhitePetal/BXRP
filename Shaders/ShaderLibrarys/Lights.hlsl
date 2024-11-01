#ifndef CUSTOME_LIGHTS_INCLUDE
#define CUSTOME_LIGHTS_INCLUDE

#define MAX_DIRECTIONAL_LIGHT_COUNT 1
#define MAX_CLUSTER_LIGHT_COUNT 64

#ifdef BX_DEFERRED
#define MAX_STENCIL_LIGHT_COUNT 8
#define MAX_OTHER_LIGHT_COUNT 72 // 8 + 64
#define MAX_IMPORTED_OTHER_LIGHT_COUNT 8
#else
#define MAX_OTHER_LIGHT_COUNT 64
#define MAX_IMPORTED_OTHER_LIGHT_COUNT 64
#endif

#define CLUSTER_X_COUNT 8
#define CLUSTER_Y_COUNT 4
#define CLUSTER_X_MUL_Y_COUNT CLUSTER_X_COUNT * CLUSTER_Y_COUNT
#define MAX_CLUSTER_Z 64
#define MAX_CLUSTER_DATA_INDEX CLUSTER_X_COUNT * CLUSTER_Y_COUNT * MAX_CLUSTER_Z - 1

#define LIGHT_TYPE_SPOT 0
#define LIGHT_TYPE_DIRECTIONAL 1
#define LIGHT_TYPE_POINT 2

CBUFFER_START(UnityLights)
    // part of Light because it can be used outside of shadow distance
    // half4 unity_OcclusionMaskSelector;

    int _DirectionalLightCount;
    half4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
    half4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
    half4 _DirectionalShadowDatas[MAX_DIRECTIONAL_LIGHT_COUNT];
    int _ClusterLightCount;
    half4 _OtherLightColors[MAX_OTHER_LIGHT_COUNT];
    float4 _OtherLightSpheres[MAX_OTHER_LIGHT_COUNT];
    half4 _OtherLightDirections[MAX_OTHER_LIGHT_COUNT];
    half4 _OtherLightThresholds[MAX_OTHER_LIGHT_COUNT];
    half4 _OtherShadowDatas[MAX_IMPORTED_OTHER_LIGHT_COUNT];
    float4x4 _OtherLightWorldToObjectMatrixs[MAX_OTHER_LIGHT_COUNT];
    float4 _ClusterSize;
CBUFFER_END

StructuredBuffer<uint> _ClusterLightingIndices;
StructuredBuffer<uint> _ClusterLightingDatas;

#include "LightCookies.hlsl"

int GetClusterCount(float3 pos_screen, float depthEye, out int lightIndexStart)
{
    float2 tileXY = pos_screen.xy / _ClusterSize.xy;
    tileXY = floor(tileXY);
    float k = depthEye / _ProjectionParams.y;
    k = log(k) * _ClusterSize.w;
    k = floor(k);
    float tileIndex = tileXY.y * float(CLUSTER_X_COUNT) + tileXY.x;
    float clusterIndex = k * float(CLUSTER_X_MUL_Y_COUNT) + tileIndex;
    clusterIndex = min(clusterIndex, float(MAX_CLUSTER_DATA_INDEX));
    lightIndexStart = clusterIndex * float(MAX_CLUSTER_LIGHT_COUNT);
    int clusterData = _ClusterLightingDatas[int(clusterIndex)];
    clusterData = min(clusterData, _ClusterLightCount);
    return clusterData;
}

#endif