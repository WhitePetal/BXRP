#ifndef CUSTOME_LIGHTS_INCLUDE
#define CUSTOME_LIGHTS_INCLUDE

#define MAX_DIRECTIONAL_LIGHT_COUNT 1
#define MAX_CLUSTER_LIGHT_COUNT 64
#define CLUSTER_X_COUNT 8
#define CLUSTER_Y_COUNT 4
#define CLUSTER_X_MUL_Y_COUNT CLUSTER_X_COUNT * CLUSTER_Y_COUNT
#define MAX_CLUSTER_Z 64
#define MAX_CLUSTER_DATA_INDEX CLUSTER_X_COUNT * CLUSTER_Y_COUNT * MAX_CLUSTER_Z - 1

CBUFFER_START(UnityLights)
    // part of Light because it can be used outside of shadow distance
    // half4 unity_OcclusionMaskSelector;

    int _DirectionalLightCount;
    half4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
    half4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
    half4 _DirectionalShadowDatas[MAX_DIRECTIONAL_LIGHT_COUNT];
    int _ClusterLightCount;
    half4 _ClusterLightColors[MAX_CLUSTER_LIGHT_COUNT];
    float4 _ClusterLightSpheres[MAX_CLUSTER_LIGHT_COUNT];
    half4 _ClusterLightDirections[MAX_CLUSTER_LIGHT_COUNT];
    half4 _ClusterLightSpotAngles[MAX_CLUSTER_LIGHT_COUNT];
    half4 _ClusterShadowDatas[MAX_CLUSTER_LIGHT_COUNT];
    float4x4 _ClusterLightWorldToObjectMatrixs[MAX_CLUSTER_LIGHT_COUNT];
    float4 _ClusterSize;
CBUFFER_END

StructuredBuffer<uint> _ClusterLightingIndices;
StructuredBuffer<uint> _ClusterLightingDatas;

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