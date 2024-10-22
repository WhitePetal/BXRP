#ifndef CUSTOME_LIGHTS_INCLUDE
#define CUSTOME_LIGHTS_INCLUDE

#define MAX_DIRECTIONAL_LIGHT_COUNT 1
#define MAX_CLUSTER_LIGHT_COUNT 16

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

CBUFFER_END

#endif