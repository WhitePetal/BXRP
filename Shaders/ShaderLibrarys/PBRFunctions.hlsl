#ifndef CUSTOME_PBR_FUNCTIONS_INCLUDE
#define CUSTOME_PBR_FUNCTIONS_INCLUDE

// ========================= Optimize ==================
half PBR_F(half v)
{
    half f = half(1.0) - v;
    half ff = f * f;
    return ff * ff * f;
}
half PBR_G(half ndotl, half ndotv, half roughness)
{
    half lambdaV = ndotl * (ndotv * (half(1.0) - roughness) + roughness);
    half lambdaL = ndotv * (ndotl * (half(1.0) - roughness) + roughness);
    return half(0.5) / (lambdaV + lambdaL + half(0.00001));
}
half PBR_D(half r, half ndoth)
{
    half rr = r*r;
    half cc = ndoth * ndoth;
    half b = cc * (rr - half(1.0)) + half(1.0);
    return r * r / (b*b + half(0.00001));
}
// =================================================

// ================== Disney + Energy conservation ===================
half3 F_Schlick(in half3 f0, in half f90, in half u)
{
    half d = half(1.0) - u;
    half dd = d * d;
    return f0 + (f90 - f0) * dd * dd * d;
}
half F_Schlick(in half f0, in half f90, in half u)
{
    half d = half(1.0) - u;
    half dd = d * d;
    return f0 + (f90 - f0) * dd * dd * d;
}

half V_SmithGGXCorrelated(half ndotl, half ndotv, half rr)
{
    half lambda_GGXV = ndotl * sqrt((-ndotv * rr + ndotv) * ndotv + rr);
    half lambda_GGXL = ndotv * sqrt((-ndotl * rr + ndotl) * ndotl + rr);
    return half(0.5) / (lambda_GGXV + lambda_GGXL + half(0.00001));
}

half D_GGX(half ndoth, half rr)
{
    // 当粗糙度为0时，镜面反射方向聚焦于一点
    // 此时 D => ~0 / (1-ndoth^2)^2
    // 即只有当观察角度处于完美镜面反射角时(ndoth = 1)，可观察到镜面反射
    // 但程序上不允许0做分母，因此粗糙度需要限制不为0
    half f = (ndoth * rr - ndoth) * ndoth + half(1.0);
    return rr / (f * f + half(0.00001));
}

half Fr_DisneyDiffuse(half ndotv, half ndotl, half ldoth, half rr)
{
    half energyBias = lerp(half(0.0), half(0.5), rr);
    half energyFactor = lerp(half(1.0), half(1.0) / half(1.51), rr);
    // if f90 is const 0 => is good for cell shading
    half fd90 = energyBias + half(2.0) * ldoth * ldoth * rr;

    half lightScatter = F_Schlick(half(1.0), fd90, ndotl);
    half viewScatter = F_Schlick(half(1.0), fd90, ndotv);
    return lightScatter * viewScatter * energyFactor;
}
// ===================================================================

#endif