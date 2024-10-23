using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXRenderPipeline
{
    public class PhysicsLightSetting : MonoBehaviour
    {
        public float radiant_power = 40; // W
        public float luminous_efficacy; // /
        public float spectral_luminous_efficacy; // /
        public float luminous_power; // lm
        public float luminous_intensity; // cd
        public float illuminance_unit; // lx-lux 距离光源距离为1m时的值
        public float luminance_unit; // nt 距离光源距离为1m时的值
        public float ev; // /

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
            new SpectralLuminousEfficacyFuncEelment(555, 1.000000f),
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
            new SpectralLuminousEfficacyFuncEelment(512, 1.000000f),
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
        public static readonly ColorWaveLengthElement[] color_wavelenth_table =
        {
            new ColorWaveLengthElement(380, new Vector3(97f/255f, 0f/255f, 97f/255f)),
            new ColorWaveLengthElement(390, new Vector3(121f/255f, 0f/255f, 141f/255f)),
            new ColorWaveLengthElement(400, new Vector3(131f/255f, 0f/255f, 181f/255f)),
            new ColorWaveLengthElement(410, new Vector3(126f/255f, 0f/255f, 219f/255f)),
            new ColorWaveLengthElement(420, new Vector3(106f/255f, 0f/255f, 255f/255f)),
            new ColorWaveLengthElement(430, new Vector3(61f/255f, 0f/255f, 255f/255f)),

            new ColorWaveLengthElement(440, new Vector3(0f/255f, 0f/255f, 255f/255f)),
            new ColorWaveLengthElement(450, new Vector3(0f/255f, 70f/255f, 255f/255f)),
            new ColorWaveLengthElement(460, new Vector3(0f/255f, 123f/255f, 255f/255f)),
            new ColorWaveLengthElement(470, new Vector3(0f/255f, 169f/255f, 255f/255f)),
            new ColorWaveLengthElement(480, new Vector3(0f/255f, 213f/255f, 255f/255f)),
            new ColorWaveLengthElement(490, new Vector3(0f/255f, 255f/255f, 255f/255f)),
            new ColorWaveLengthElement(500, new Vector3(0f/255f, 255f/255f, 146f/255f)),

            new ColorWaveLengthElement(510, new Vector3(0f/255f, 255f/255f, 0f/255f)),
            new ColorWaveLengthElement(520, new Vector3(54f/255f, 255f/255f, 0f/255f)),
            new ColorWaveLengthElement(530, new Vector3(94f/255f, 255f/255f, 0f/255f)),
            new ColorWaveLengthElement(540, new Vector3(129f/255f, 255f/255f, 0f/255f)),
            new ColorWaveLengthElement(550, new Vector3(163f/255f, 255f/255f, 0f/255f)),
            new ColorWaveLengthElement(560, new Vector3(195f/255f, 255f/255f, 0f/255f)),
            new ColorWaveLengthElement(570, new Vector3(225f/255f, 255f/255f, 0f/255f)),
            new ColorWaveLengthElement(580, new Vector3(255f/255f, 255f/255f, 0f/255f)),
            new ColorWaveLengthElement(590, new Vector3(255f/255f, 223f/255f, 0f/255f)),
            new ColorWaveLengthElement(600, new Vector3(255f/255f, 190f/255f, 0f/255f)),
            new ColorWaveLengthElement(610, new Vector3(255f/255f, 155f/255f, 0f/255f)),
            new ColorWaveLengthElement(620, new Vector3(255f/255f, 119f/255f, 0f/255f)),
            new ColorWaveLengthElement(630, new Vector3(255f/255f, 79f/255f, 0f/255f)),
            new ColorWaveLengthElement(640, new Vector3(255f/255f, 33f/255f, 0f/255f)),

            new ColorWaveLengthElement(650, new Vector3(255f/255f, 0f/255f, 0f/255f)),
            new ColorWaveLengthElement(660, new Vector3(241f/255f, 0f/255f, 0f/255f)),
            new ColorWaveLengthElement(670, new Vector3(232f/255f, 0f/255f, 0f/255f)),
            new ColorWaveLengthElement(680, new Vector3(223f/255f, 0f/255f, 0f/255f)),
            new ColorWaveLengthElement(690, new Vector3(214f/255f, 0f/255f, 0f/255f)),
            new ColorWaveLengthElement(700, new Vector3(205f/255f, 0f/255f, 0f/255f)),
            new ColorWaveLengthElement(710, new Vector3(196f/255f, 0f/255f, 0f/255f)),
            new ColorWaveLengthElement(720, new Vector3(187f/255f, 0f/255f, 0f/255f)),
            new ColorWaveLengthElement(730, new Vector3(177f/255f, 0f/255f, 0f/255f)),
            new ColorWaveLengthElement(740, new Vector3(168f/255f, 0f/255f, 0f/255f)),
            new ColorWaveLengthElement(750, new Vector3(158f/255f, 0f/255f, 0f/255f)),
            new ColorWaveLengthElement(760, new Vector3(148f/255f, 0f/255f, 0f/255f)),
            new ColorWaveLengthElement(770, new Vector3(138f/255f, 0f/255f, 0f/255f)),
            new ColorWaveLengthElement(780, new Vector3(128f/255f, 0f/255f, 0f/255f)),
        };
        #endregion

		private void UpdateByRadiantPower()
		{
            luminous_power = luminous_efficacy * radiant_power;

        }
    }
}
