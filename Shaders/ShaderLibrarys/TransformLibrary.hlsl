#ifndef CUSTOME_TRANSFORM_LIBRARY_INCLUDE
#define CUSTOME_TRANSFORM_LIBRARY_INCLUDE

half3 BlendNormals(half3 n1, half3 n2)
{
    return normalize(half3(n1.xy + n2.xy, n1.z*n2.z));
}

half3 UnpackScaleNormalRGorAGCustome(half4 packednormal, half bumpScale)
{
    #if defined(UNITY_NO_DXT5nm)
        half3 normal = packednormal.xyz * half(2.0) - half(1.0);
        #if (SHADER_TARGET >= 30)
            // SM2.0: instruction count limitation
            // SM2.0: normal scaler is not supported
            normal.xy *= bumpScale;
        #endif
        return normal;
    #elif defined(UNITY_ASTC_NORMALMAP_ENCODING)
        half3 normal;
        normal.xy = (packednormal.wy * half(2.0) - half(1.0));
        normal.z = sqrt(half(1.0) - saturate(dot(normal.xy, normal.xy)));
        normal.xy *= bumpScale;
        return normal;
    #else
        // This do the trick
        packednormal.x *= packednormal.w;

        half3 normal;
        normal.xy = (packednormal.xy * half(2.0) - half(1.0));
        #if (SHADER_TARGET >= 30)
            // SM2.0: instruction count limitation
            // SM2.0: normal scaler is not supported
            normal.xy *= bumpScale;
        #endif
        normal.z = sqrt(half(1.0) - saturate(dot(normal.xy, normal.xy)));
        return normal;
    #endif
}

half3 UnpackScaleNormalCustome(half4 packednormal, half bumpScale)
{
    return UnpackScaleNormalRGorAGCustome(packednormal, bumpScale);
}

half3 GetBlendNormalWorldFromMapAB(half3 tangent_world, half3 binormal_world, half3 normal_world, half4 mainNormalMap, half4 detilNormalMapA, half4 detilNormalMapB, half mainNormalScale, half detilNormalScaleA, half detilNormalScaleB, half2 mask)
{
    tangent_world = normalize(tangent_world);
    normal_world = normalize(normal_world);
    binormal_world = normalize(binormal_world);
    half3 mainNor = UnpackScaleNormalCustome(mainNormalMap, mainNormalScale);
    half3 detilNorA = UnpackScaleNormalCustome(detilNormalMapA, detilNormalScaleA);
    half3 detilNorB = UnpackScaleNormalCustome(detilNormalMapB, detilNormalScaleB);
    half3 detilNor = lerp(detilNorB, detilNorA, mask.r);
    half3 normal_tangent = lerp(mainNor, BlendNormals(mainNor, detilNor), mask.g);
    half3 n = normalize(
        normal_tangent.x * tangent_world +
        normal_tangent.y * binormal_world +
        normal_tangent.z * normal_world
    );
    return  n;
}

#endif