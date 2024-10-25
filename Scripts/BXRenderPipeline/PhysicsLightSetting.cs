using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXRenderPipeline
{
    [RequireComponent(typeof(Light)), ExecuteAlways]
    public class PhysicsLightSetting : MonoBehaviour
    {
        public ColorSystemType colorSystemType = ColorSystemType.PAL;
        //[ColorUsageAttribute(false, true)]
        public Color color; // R G B
        [Range(800f, 15000f)]
        public float color_temperature = 3800; // K
        public float radiant_power = 40; // W
        [Range(0f, 1f)]
        public float light_efficacy = 1f;
        [Range(0f, 1f)]
        public float luminous_efficacy = 1f; // /
        public const float spectral_luminous_efficacy = 683f; // /
        public float luminous_power; // lm
        public float luminous_intensity; // cd
        public float illuminance_unit; // lx-lux 距离光源距离为1m时的值
        public float luminance_unit; // nt 距离光源距离为1m时的值
        public float ev; // /

        private Light light;

        private ColorSystem useColorSystem;

        public enum ColorSystemType
        {
            PAL,
            CIE,
            NTSC,
            SMPTE,
            HDTV,
            Rec709
        }

        public struct ColorSystem
        {
            public float xRed, yRed;
            public float xGreen, yGreen;
            public float xBlue, yBlue;
            public float xWhite, yWhite;
            public float gama;

            public ColorSystem(float xRed, float yRed, float xGreen, float yGreen, float xBlue, float yBlue,
                float xWhite, float yWhite, float gama)
            {
                this.xRed = xRed;
                this.yRed = yRed;
                this.xGreen = xGreen;
                this.yGreen = yGreen;
                this.xBlue = xBlue;
                this.yBlue = yBlue;
                this.xWhite = xWhite;
                this.yWhite = yWhite;
                this.gama = gama;
            }
        }
        // White point chromaticities.
        // For NTSC television
        public const float IlluminantC_x = 0.3101f;
        public const float IlluminantC_y = 0.3162f;
        // For EBU and SMPTE
        public const float IlluminantD65_x = 0.3127f;
        public const float IlluminantD65_y = 0.3291f;
        // CIE equal-energy illuminant
        public const float IlluminantE_x = 0.33333333f;
        public const float IlluminantE_y = 0.33333333f;
        // nonlinear
        public const float GAMMA_REC709 = 0;             /* Rec. 709 */
        public static readonly ColorSystem NTSCsystem = new(0.67f, 0.33f, 0.21f, 0.71f, 0.14f, 0.08f, IlluminantC_x, IlluminantC_y, GAMMA_REC709);
        public static readonly ColorSystem PALsystem = new ColorSystem(0.64f, 0.33f, 0.29f, 0.60f, 0.15f, 0.06f, IlluminantD65_x, IlluminantD65_y, GAMMA_REC709);
        public static readonly ColorSystem SMPTEsystem = new ColorSystem(0.630f, 0.340f, 0.310f, 0.595f, 0.155f, 0.070f, IlluminantD65_x, IlluminantD65_y, GAMMA_REC709);
        public static readonly ColorSystem HDTVsystem = new ColorSystem(0.670f, 0.330f, 0.210f, 0.710f, 0.150f, 0.060f, IlluminantD65_x, IlluminantD65_y, GAMMA_REC709);
        public static readonly ColorSystem CIEsystem = new ColorSystem(0.7355f, 0.2645f, 0.2658f, 0.7243f, 0.1669f, 0.0085f, IlluminantE_x, IlluminantE_y, GAMMA_REC709);
        public static readonly ColorSystem Rec709system = new ColorSystem(0.64f, 0.33f, 0.30f, 0.60f, 0.15f, 0.06f, IlluminantD65_x, IlluminantD65_y, GAMMA_REC709);

        public struct SpectralLuminousEfficacyFuncEelment
		{
            public float lambda;
            public float efficacy;

            public SpectralLuminousEfficacyFuncEelment(float lambda, float efficacy)
			{
                this.lambda = lambda;
                this.efficacy = efficacy;
			}
        }
        public struct ColorWaveLengthElement
		{
            public float lambda;
            public Vector3 color;

            public ColorWaveLengthElement(float lambda, Vector3 color)
			{
                this.lambda = lambda;
                this.color = color;
			}
        }
        #region DataTables
        // 大于 0.1 lx-lux 时，使用这个
        public static readonly SpectralLuminousEfficacyFuncEelment[] photopic_spectral_luminous_efficacy_curve =
        {
            new SpectralLuminousEfficacyFuncEelment(380, 0.000039f),
            new SpectralLuminousEfficacyFuncEelment(390, 0.000120f),
            new SpectralLuminousEfficacyFuncEelment(400, 0.000396f),
            new SpectralLuminousEfficacyFuncEelment(410, 0.001210f),
            new SpectralLuminousEfficacyFuncEelment(420, 0.004000f),
            new SpectralLuminousEfficacyFuncEelment(430, 0.011600f),
            new SpectralLuminousEfficacyFuncEelment(440, 0.023000f),
            new SpectralLuminousEfficacyFuncEelment(450, 0.038000f),
            new SpectralLuminousEfficacyFuncEelment(460, 0.060000f),
            new SpectralLuminousEfficacyFuncEelment(470, 0.091000f),
            new SpectralLuminousEfficacyFuncEelment(480, 0.139000f),
            new SpectralLuminousEfficacyFuncEelment(490, 0.208000f),
            new SpectralLuminousEfficacyFuncEelment(500, 0.323000f),
            new SpectralLuminousEfficacyFuncEelment(510, 0.503000f),
            new SpectralLuminousEfficacyFuncEelment(520, 0.710000f),
            new SpectralLuminousEfficacyFuncEelment(530, 0.862000f),
            new SpectralLuminousEfficacyFuncEelment(540, 0.954000f),
            new SpectralLuminousEfficacyFuncEelment(550, 0.995000f),
            new SpectralLuminousEfficacyFuncEelment(560, 0.995000f),
            new SpectralLuminousEfficacyFuncEelment(570, 0.952000f),
            new SpectralLuminousEfficacyFuncEelment(580, 0.870000f),
            new SpectralLuminousEfficacyFuncEelment(590, 0.757000f),
            new SpectralLuminousEfficacyFuncEelment(600, 0.631000f),
            new SpectralLuminousEfficacyFuncEelment(610, 0.503000f),
            new SpectralLuminousEfficacyFuncEelment(620, 0.381000f),
            new SpectralLuminousEfficacyFuncEelment(630, 0.265000f),
            new SpectralLuminousEfficacyFuncEelment(640, 0.175000f),
            new SpectralLuminousEfficacyFuncEelment(650, 0.107000f),
            new SpectralLuminousEfficacyFuncEelment(660, 0.061000f),
            new SpectralLuminousEfficacyFuncEelment(670, 0.032000f),
            new SpectralLuminousEfficacyFuncEelment(680, 0.017000f),
            new SpectralLuminousEfficacyFuncEelment(690, 0.008200f),
            new SpectralLuminousEfficacyFuncEelment(700, 0.004100f),
            new SpectralLuminousEfficacyFuncEelment(710, 0.002100f),
            new SpectralLuminousEfficacyFuncEelment(720, 0.001050f),
            new SpectralLuminousEfficacyFuncEelment(730, 0.000520f),
            new SpectralLuminousEfficacyFuncEelment(740, 0.000250f),
            new SpectralLuminousEfficacyFuncEelment(750, 0.000120f),
            new SpectralLuminousEfficacyFuncEelment(760, 0.000060f),
            new SpectralLuminousEfficacyFuncEelment(770, 0.000030f),
            new SpectralLuminousEfficacyFuncEelment(780, 0.000015f),
        };
        // 小于 0.1 lx-lux 时，使用这个
        public static readonly SpectralLuminousEfficacyFuncEelment[] scotopic_spectral_luminous_efficacy_curve =
        {
            new SpectralLuminousEfficacyFuncEelment(380, 0.000589f),
            new SpectralLuminousEfficacyFuncEelment(390, 0.002209f),
            new SpectralLuminousEfficacyFuncEelment(400, 0.009290f),
            new SpectralLuminousEfficacyFuncEelment(410, 0.034840f),
            new SpectralLuminousEfficacyFuncEelment(420, 0.096600f),
            new SpectralLuminousEfficacyFuncEelment(430, 0.199800f),
            new SpectralLuminousEfficacyFuncEelment(440, 0.328100f),
            new SpectralLuminousEfficacyFuncEelment(450, 0.455000f),
            new SpectralLuminousEfficacyFuncEelment(460, 0.567000f),
            new SpectralLuminousEfficacyFuncEelment(470, 0.676000f),
            new SpectralLuminousEfficacyFuncEelment(480, 0.793000f),
            new SpectralLuminousEfficacyFuncEelment(490, 0.904000f),
            new SpectralLuminousEfficacyFuncEelment(500, 0.982000f),
            new SpectralLuminousEfficacyFuncEelment(510, 0.997000f),
            new SpectralLuminousEfficacyFuncEelment(520, 0.935000f),
            new SpectralLuminousEfficacyFuncEelment(530, 0.811000f),
            new SpectralLuminousEfficacyFuncEelment(540, 0.650000f),
            new SpectralLuminousEfficacyFuncEelment(550, 0.481000f),
            new SpectralLuminousEfficacyFuncEelment(560, 0.328800f),
            new SpectralLuminousEfficacyFuncEelment(570, 0.207600f),
            new SpectralLuminousEfficacyFuncEelment(580, 0.121200f),
            new SpectralLuminousEfficacyFuncEelment(590, 0.065500f),
            new SpectralLuminousEfficacyFuncEelment(600, 0.033150f),
            new SpectralLuminousEfficacyFuncEelment(610, 0.015930f),
            new SpectralLuminousEfficacyFuncEelment(620, 0.007370f),
            new SpectralLuminousEfficacyFuncEelment(630, 0.003335f),
            new SpectralLuminousEfficacyFuncEelment(640, 0.001497f),
            new SpectralLuminousEfficacyFuncEelment(650, 0.000677f),
            new SpectralLuminousEfficacyFuncEelment(660, 0.0003129f),
            new SpectralLuminousEfficacyFuncEelment(670, 0.0001480f),
            new SpectralLuminousEfficacyFuncEelment(680, 0.0000715f),
            new SpectralLuminousEfficacyFuncEelment(690, 0.00003533f),
            new SpectralLuminousEfficacyFuncEelment(700, 0.00001780f),
            new SpectralLuminousEfficacyFuncEelment(710, 0.00000914f),
            new SpectralLuminousEfficacyFuncEelment(720, 0.00000478f),
            new SpectralLuminousEfficacyFuncEelment(730, 0.000002546f),
            new SpectralLuminousEfficacyFuncEelment(740, 0.000001379f),
            new SpectralLuminousEfficacyFuncEelment(750, 0.000000760f),
            new SpectralLuminousEfficacyFuncEelment(760, 0.000000425f),
            new SpectralLuminousEfficacyFuncEelment(770, 0.000000241f),
            new SpectralLuminousEfficacyFuncEelment(780, 0.000000139f),
        };
        public static readonly Vector3[] cie_color_match =
        {
            new(0.0014f,0.0000f,0.0065f), new(0.0022f,0.0001f,0.0105f), new(0.0042f,0.0001f,0.0201f),
            new(0.0076f,0.0002f,0.0362f), new(0.0143f,0.0004f,0.0679f), new(0.0232f,0.0006f,0.1102f),
            new(0.0435f,0.0012f,0.2074f), new(0.0776f,0.0022f,0.3713f), new(0.1344f,0.0040f,0.6456f),
            new(0.2148f,0.0073f,1.0391f), new(0.2839f,0.0116f,1.3856f), new(0.3285f,0.0168f,1.6230f),
            new(0.3483f,0.0230f,1.7471f), new(0.3481f,0.0298f,1.7826f), new(0.3362f,0.0380f,1.7721f),
            new(0.3187f,0.0480f,1.7441f), new(0.2908f,0.0600f,1.6692f), new(0.2511f,0.0739f,1.5281f),
            new(0.1954f,0.0910f,1.2876f), new(0.1421f,0.1126f,1.0419f), new(0.0956f,0.1390f,0.8130f),
            new(0.0580f,0.1693f,0.6162f), new(0.0320f,0.2080f,0.4652f), new(0.0147f,0.2586f,0.3533f),
            new(0.0049f,0.3230f,0.2720f), new(0.0024f,0.4073f,0.2123f), new(0.0093f,0.5030f,0.1582f),
            new(0.0291f,0.6082f,0.1117f), new(0.0633f,0.7100f,0.0782f), new(0.1096f,0.7932f,0.0573f),
            new(0.1655f,0.8620f,0.0422f), new(0.2257f,0.9149f,0.0298f), new(0.2904f,0.9540f,0.0203f),
            new(0.3597f,0.9803f,0.0134f), new(0.4334f,0.9950f,0.0087f), new(0.5121f,1.0000f,0.0057f),
            new(0.5945f,0.9950f,0.0039f), new(0.6784f,0.9786f,0.0027f), new(0.7621f,0.9520f,0.0021f),
            new(0.8425f,0.9154f,0.0018f), new(0.9163f,0.8700f,0.0017f), new(0.9786f,0.8163f,0.0014f),
            new(1.0263f,0.7570f,0.0011f), new(1.0567f,0.6949f,0.0010f), new(1.0622f,0.6310f,0.0008f),
            new(1.0456f,0.5668f,0.0006f), new(1.0026f,0.5030f,0.0003f), new(0.9384f,0.4412f,0.0002f),
            new(0.8544f,0.3810f,0.0002f), new(0.7514f,0.3210f,0.0001f), new(0.6424f,0.2650f,0.0000f),
            new(0.5419f,0.2170f,0.0000f), new(0.4479f,0.1750f,0.0000f), new(0.3608f,0.1382f,0.0000f),
            new(0.2835f,0.1070f,0.0000f), new(0.2187f,0.0816f,0.0000f), new(0.1649f,0.0610f,0.0000f),
            new(0.1212f,0.0446f,0.0000f), new(0.0874f,0.0320f,0.0000f), new(0.0636f,0.0232f,0.0000f),
            new(0.0468f,0.0170f,0.0000f), new(0.0329f,0.0119f,0.0000f), new(0.0227f,0.0082f,0.0000f),
            new(0.0158f,0.0057f,0.0000f), new(0.0114f,0.0041f,0.0000f), new(0.0081f,0.0029f,0.0000f),
            new(0.0058f,0.0021f,0.0000f), new(0.0041f,0.0015f,0.0000f), new(0.0029f,0.0010f,0.0000f),
            new(0.0020f,0.0007f,0.0000f), new(0.0014f,0.0005f,0.0000f), new(0.0010f,0.0004f,0.0000f),
            new(0.0007f,0.0002f,0.0000f), new(0.0005f,0.0002f,0.0000f), new(0.0003f,0.0001f,0.0000f),
            new(0.0002f,0.0001f,0.0000f), new(0.0002f,0.0001f,0.0000f), new(0.0001f,0.0000f,0.0000f),
            new(0.0001f,0.0000f,0.0000f), new(0.0001f,0.0000f,0.0000f), new(0.0000f,0.0000f,0.0000f)
        };                                          
        #endregion

        private Vector2 UPVP_TO_XY(Vector2 upvp)
        {
            Vector2 XY;
            XY.x = (9 * upvp.x) / ((6 * upvp.x) - (16 * upvp.y) + 12);
            XY.y = (4 * upvp.y) / ((6 * upvp.y) - (16 * upvp.y) + 12);
            return XY;

        }
        private Vector2 XY_TO_UPVP(Vector2 XY)
        {
            Vector2 upvp;
            upvp.x = (4 * XY.x) / ((-2 * XY.x) + (12 * XY.y) + 3);
            upvp.y = (9 * XY.y) / ((-2 * XY.x) + (12 * XY.y) + 3);
            return upvp;
        }

        // XYZ 转 RGB 可能得到负值的RGB
        private Vector3 XYZ_TO_RGB(ColorSystem cs, Vector3 XYZ)
        {
            Vector3 rgb;

            float xr = cs.xRed; float yr = cs.yRed; float zr = 1 - (xr + yr);
            float xg = cs.xGreen; float yg = cs.yGreen; float zg = 1 - (xg + yg);
            float xb = cs.xBlue; float yb = cs.yBlue; float zb = 1 - (xb + yb);
            float xw = cs.xWhite; float yw = cs.yWhite; float zw = 1 - (xw + yw);

            float rx = (yg * zb) - (yb * zg); float ry = (xb * zg) - (xg * zb); float rz = (xg * yb) - (xb * yg);
            float gx = (yb * zr) - (yr * zb); float gy = (xr * zb) - (xb * zr); float gz = (xb * yr) - (xr * yb);
            float bx = (yr * zg) - (yg * zr); float by = (xg * zr) - (xr * zg); float bz = (xr * yg) - (xg * yr);

            float rw = ((rx * xw) + (ry * yw) + (rz * zw)) / yw;
            float gw = ((gx * xw) + (gy * yw) + (gz * zw)) / yw;
            float bw = ((bx * xw) + (by * yw) + (bz * zw)) / yw;

            rx = rx / rw; ry = ry / rw; rz = rz / rw;
            gx = gx / gw; gy = gy / gw; gz = gz / gw;
            bx = bx / bw; by = by / bw; bz = bz / bw;

            rgb.x = (rx * XYZ.x) + (ry * XYZ.y) + (rz * XYZ.z);
            rgb.y = (gx * XYZ.x) + (gy * XYZ.y) + (gz * XYZ.z);
            rgb.z = (bx * XYZ.x) + (by * XYZ.y) + (bz * XYZ.z);

            return rgb;
        }

        private bool InisdeGamut(Vector3 rgb)
        {
            return rgb.x >= 0 && rgb.y >= 0 && rgb.z >= 0;
        }

        // 如果RGB有负值则将其转为最接近的正值RGB
        private Vector3 ConstrainRGB(Vector3 sourceRGB)
        {
            Vector3 dstRGB = sourceRGB;
            float w;
            w = (0 < sourceRGB.x) ? 0 : sourceRGB.x;
            w = (w < sourceRGB.y) ? w : sourceRGB.y;
            w = (w < sourceRGB.z) ? w : sourceRGB.z;
            w = -w;

            if(w > 0)
            {
                dstRGB = dstRGB + new Vector3(w, w, w);
            }
            return dstRGB;
        }

        // Linear Color To Nonlinear Color
        private float GammaCorrect(ColorSystem cs, float c)
        {
            float gama = cs.gama;
            float r = c;
            if(gama == GAMMA_REC709)
            {
                float cc = 0.018f;
                if(c < cc)
                {
                    r = c * ((1.099f * Mathf.Pow(cc, 0.45f)) - 0.099f) / cc;
                }
                else
                {
                    r = (1.099f * Mathf.Pow(c, 0.45f)) - 0.099f;
                }
            }
            else
            {
                // Nonlinear color = (Linear Color)^(1.0 / gama)
                r = Mathf.Pow(c, 1.0f / gama);
            }
            return r;
        }

        private Vector3 GammaCorrectRGB(ColorSystem cs, Vector3 rgb)
        {
            return new Vector3(GammaCorrect(cs, rgb.x), GammaCorrect(cs, rgb.y), GammaCorrect(cs, rgb.z));
        }

        private Vector3 NormRGB(Vector3 rgb)
        {
            Vector3 r = rgb;
            float greatest = Mathf.Max(rgb.x, Mathf.Max(rgb.y, rgb.z));
            if(greatest > 0)
            {
                r = new Vector3(rgb.x / greatest, rgb.y / greatest, rgb.z / greatest);
            }
            return r;
        }

        private float bb_spectrum(float temperature, float wavelength)
        {
            float wlm = wavelength * 1e-9f;
            return (3.74183e-16f * Mathf.Pow(wlm, -5f)) / (Mathf.Exp(1.4388e-2f / (wlm * temperature)) - 1f);
        }

        private Vector3 Spectrum_TO_XYZ(float temperature)
        {
            Vector3 r;
            int i;
            float lambda, X = 0, Y = 0, Z = 0, XYZ;

            for(i = 0, lambda = 380; lambda < 780.1f; i++, lambda += 5)
            {
                float ME;
                ME = bb_spectrum(temperature, lambda);
                X += ME * cie_color_match[i][0];
                Y += ME * cie_color_match[i][1];
                Z += ME * cie_color_match[i][2];
            }
            XYZ = X + Y + Z;
            r.x = X / XYZ;
            r.y = Y / XYZ;
            r.z = Z / XYZ;
            return r;
        }

        private ColorSystem GetColorSystem()
        {
            switch (colorSystemType)
            {
                case ColorSystemType.PAL:
                    return PALsystem;
                case ColorSystemType.CIE:
                    return CIEsystem;
                case ColorSystemType.HDTV:
                    return HDTVsystem;
                case ColorSystemType.NTSC:
                    return NTSCsystem;
                case ColorSystemType.Rec709:
                    return Rec709system;
                case ColorSystemType.SMPTE:
                    return SMPTEsystem;
            }
            return PALsystem;
        }

        private float GetLuminousEfficiencyFromRadiant(float temperature, SpectralLuminousEfficacyFuncEelment[] curve)
        {
            float XV = 0f;
            float X = 0f;
            for (int i = 0; i < curve.Length; ++i)
            {
                var luminous_efficay_element = curve[i];
                float Xe = bb_spectrum(temperature, luminous_efficay_element.lambda);
                X += Xe;
                XV += Xe * luminous_efficay_element.efficacy;
            }
            return XV / X;
        }

        private void GetLuminousEfficiencyFromRadiant(float temperature)
        {

            float power_to_intensity = 1f;
            switch (light.type)
            {
                case LightType.Point:
                    power_to_intensity = 1f / (4f * Mathf.PI);
                    break;
                case LightType.Spot:
                    float halfAngle = light.spotAngle * 0.5f * Mathf.Deg2Rad;
                    float cos = Mathf.Cos(halfAngle);
                    power_to_intensity = 1f / (2 * Mathf.PI * (1f - cos));
                    break;
            }
            float luminous_efficacy_temp = GetLuminousEfficiencyFromRadiant(temperature, photopic_spectral_luminous_efficacy_curve) * light_efficacy;
            float luminous_power_temp = radiant_power * luminous_efficacy_temp * spectral_luminous_efficacy;
            float luminous_intensity_temp = luminous_power_temp * power_to_intensity;
            float illuminance_unit_temp = luminous_intensity_temp;
            if(illuminance_unit_temp < 0.1f)
            {
                luminous_efficacy_temp = GetLuminousEfficiencyFromRadiant(temperature, scotopic_spectral_luminous_efficacy_curve) * light_efficacy;
                luminous_power_temp = radiant_power * luminous_efficacy_temp * spectral_luminous_efficacy;
                luminous_intensity_temp = luminous_power_temp * power_to_intensity;
                illuminance_unit_temp = luminous_intensity_temp;
            }
            this.luminous_efficacy = luminous_efficacy_temp;
            this.luminous_power = luminous_power_temp;
            this.luminous_intensity = luminous_intensity_temp;
            this.illuminance_unit = illuminance_unit_temp;
        }

        private void UpdateByRadiantPower()
		{
            luminous_power = luminous_efficacy * radiant_power;

        }

        private void OnEnable()
        {
            this.light = GetComponent<Light>();
        }

        private void Update()
        {
            OnValidate();
        }

        private void OnValidate()
        {
            this.light = GetComponent<Light>();
            Vector3 XYZ = Spectrum_TO_XYZ(color_temperature);
            ColorSystem cs = GetColorSystem();
            Vector3 rgb = XYZ_TO_RGB(cs, XYZ);
            rgb = ConstrainRGB(rgb);
            rgb = NormRGB(rgb);
            this.color = new Color(rgb.x, rgb.y, rgb.z);
            light.color = color;
            GetLuminousEfficiencyFromRadiant(color_temperature);
            light.intensity = luminous_intensity;
        }
    }
}
