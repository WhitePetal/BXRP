#ifndef CUSTOME_PBR_FUNCTIONS_INCLUDE
#define CUSTOME_PBR_FUNCTIONS_INCLUDE

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
    return half(0.5) / (lambdaV + lambdaL + half(0.001));
}
half PBR_D(half r, half ndoth)
{
    half rr = r*r;
    half cc = ndoth * ndoth;
    half b = cc * (rr - half(1.0)) + half(1.0);
    return r * r / (b*b + half(0.00001));
}

#endif