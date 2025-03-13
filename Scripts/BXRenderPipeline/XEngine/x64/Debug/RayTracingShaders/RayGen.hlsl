#include "Common.hlsl"

// Raytracing output texture, accessed as a UAV
RWTexture2D< float4 > gOutput : register(u0);

// Raytracing acceleration structure, accessed as a SRV
RaytracingAccelerationStructure SceneBVH : register(t0);

cbuffer CameraBuffer : register(b0)
{
    float4x4 Matrix_V;
    float4x4 Matrix_P;
    float4x4 Matrix_VI;
    float4x4 Matrix_PI;
}

[shader("raygeneration")] 
void RayGen() {
  // Initialize the ray payload
  HitInfo payload;
  payload.colorAndDistance = float4(0.0, 0.0, 0.0, 0);

  // Get the location within the dispatched 2D grid of work items
  // (often maps to pixels, so this could represent a pixel coordinate).
  uint2 launchIndex = DispatchRaysIndex().xy;
  float2 dims = float2(DispatchRaysDimensions().xy);
  float2 d = (((launchIndex.xy + 0.5f) / dims.xy) * 2.f - 1.f);

  RayDesc ray;
  ray.Origin = mul(Matrix_VI, float4(0, 0, 0, 1));
  float4 target = mul(Matrix_PI, float4(d.x, -d.y, 1, 1));
  ray.Direction = mul(Matrix_VI, float4(target.xyz, 0.0));
  ray.TMin = 0;
  ray.TMax = 100000;

  TraceRay(
    SceneBVH,
    RAY_FLAG_NONE,
    // InstanceInclusionMask
    0xFF,
    // RayContributionToHitGroupIndex
    0,
    // MultiplierForGeometryContributionToHitGroupIndex
    0,
    // MissShaderIndex
    // Index of the miss shader to use in case several consecutive miss shaders are present in the SBT 
    0,
    ray,
    payload
  );

  gOutput[launchIndex] = float4(payload.colorAndDistance.rgb, 1.f);
}
