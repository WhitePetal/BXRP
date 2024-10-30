#ifndef CUSTOME_GSR_INCLUDE
#define CUSTOME_GSR_INCLUDE

#define OperationMode 1
#define EdgeThreshold half(8.0) / half(255.0)
#define EdgeSharpness half(2.0)
#define CfgSRArea 0

// Lanczos�˲��˸��
// https://en.wikipedia.org/wiki/Lanczos_resampling
half fastLanczos2(half x)
{
    half wA = x - half(4.0);
    half wB = x * wA - wA;
    wA *= wA;
    return wB * wA;
}
half2 weightY(half dx, half dy, half c, half std)
{
    half x = ((dx * dx)+(dy * dy)) * half(0.5) + clamp(abs(c) * std, half(0.0), half(1.0));
    half w = fastLanczos2(x);
    return half2(w, w * c);
}
half4 SGSRRH(float2 p)
{
    half4 res = _PostProcessInput.GatherRed(sampler_PostProcessInput, p);
    return res;
}
half4 SGSRGH(float2 p)
{
    half4 res = _PostProcessInput.GatherGreen(sampler_PostProcessInput, p);
    return res;
}
half4 SGSRBH(float2 p)
{
    half4 res = _PostProcessInput.GatherBlue(sampler_PostProcessInput, p);
    return res;
}
half4 SGSRAH(float2 p)
{
    half4 res = _PostProcessInput.GatherAlpha(sampler_PostProcessInput, p);
    return res;
}
half4 SGSRRGBH(float2 p)
{
    half4 res = _PostProcessInput.SampleLevel(sampler_PostProcessInput, p, 0);
    return res;
}
half4 SGSRH(float2 p)
{
    // #if OperationMode == 0
    // return SGSRRH(p);
    // #elif OperationMode == 1
    return SGSRGH(p);
    // #elif OperationMode == 2
    // return SGSRBH(p);
    // #else
    // return SGSRAH(p);
    // #endif
}
void SgsrYuvH(out half4 pix, float2 uv, float4 con1)
{
    int mode = OperationMode;
    pix.xyz = SGSRRGBH(uv).xyz; // mode != 1 ;;;; pix.xyzw = SGSRRGBH(uv).xyzw; 
    // pix.w = 1.0;
    // return;
    float xCenter;
    xCenter = abs(uv.x+-0.5);
    float yCenter;
    yCenter = abs(uv.y+-0.5);

    float2 imgCoord = (uv.xy * con1.zw) + float2(-0.5, 0.5);
    float2 imgCoordPixel = floor(imgCoord);
    float2 coord = (imgCoordPixel * con1.xy);
    half2  pl = (imgCoord+(-imgCoordPixel));
    half4  left = SGSRH(coord);

    half edgeVote = abs(left.z - left.y) + abs(pix[mode] - left.y)  + abs(pix[mode] - left.z);
    if (edgeVote > EdgeThreshold)
    {
        coord.x += con1.x;

        half4 right = SGSRH(coord + float2(con1.x,  0.0));
        half4 upDown;
        upDown.xy = SGSRH(coord + float2(0.0, -con1.y)).wz;
        upDown.zw = SGSRH(coord + float2(0.0,  con1.y)).yx;

        half mean = (left.y + left.z + right.x + right.w) * half(0.25);
        left = left - mean;
        right = right - mean;
        upDown = upDown - mean;
        pix.w = pix[mode] - mean;

        half sum = (((((abs(left.x)+abs(left.y))+abs(left.z))+abs(left.w))+(((abs(right.x)+abs(right.y))+abs(right.z))+abs(right.w)))+(((abs(upDown.x)+abs(upDown.y))+abs(upDown.z))+abs(upDown.w)));
        half std = half(2.181818) / sum;

        half2 aWY = weightY(pl.x, pl.y + half(1.0), upDown.x, std);
        aWY += weightY(pl.x - half(1.0), pl.y + half(1.0), upDown.y, std);
        aWY += weightY(pl.x - half(1.0), pl.y - half(2.0), upDown.z, std);
        aWY += weightY(pl.x, pl.y - half(2.0), upDown.w, std);			
        aWY += weightY(pl.x + half(1.0), pl.y - half(1.0), left.x, std);
        aWY += weightY(pl.x, pl.y - half(1.0), left.y, std);
        aWY += weightY(pl.x, pl.y, left.z, std);
        aWY += weightY(pl.x + half(1.0), pl.y, left.w, std);
        aWY += weightY(pl.x - half(1.0), pl.y - half(1.0), right.x, std);
        aWY += weightY(pl.x - half(2.0), pl.y - half(1.0), right.y, std);
        aWY += weightY(pl.x - half(2.0), pl.y, right.z, std);
        aWY += weightY(pl.x - half(1.0), pl.y, right.w, std);

        half finalY = aWY.y / aWY.x;

        half max4 = max(max(left.y, left.z), max(right.x, right.w));
        half min4 = min(min(left.y, left.z), min(right.x, right.w));
        finalY = clamp(EdgeSharpness * finalY, min4, max4);

        half deltaY = finalY - pix.w;

        pix.x = saturate((pix.x + deltaY));
        pix.y = saturate((pix.y + deltaY));
        pix.z = saturate((pix.z + deltaY));
    }
    #ifdef HALF_SCREEN_LINE
    half ppp = uv.x - half(0.5) < half(1e-3);
    pix.rgb = half3(half(0.0), half(1.0), half(0.0)) * ppp + pix.rgb * (half(1.0) - ppp);
    pix.w = uv.x > half(0.5);
    #else
    pix.w = half(1.0);  //����û��ʹ��alphaͨ��
    #endif
}

#endif