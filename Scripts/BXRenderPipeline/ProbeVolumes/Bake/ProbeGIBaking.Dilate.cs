#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace BXRenderPipeline
{
    partial class AdaptiveProbeVolumes
    {
        [GenerateHLSL(needAccessors = false)]
        struct DilatedProbe
        {
            public Vector3 L0;

            public Vector3 L1_0;
            public Vector3 L1_1;
            public Vector3 L1_2;

            public Vector3 L2_0;
            public Vector3 L2_1;
            public Vector3 L2_2;
            public Vector3 L2_3;
            public Vector3 L2_4;

            public Vector4 SO_L1L1;
            public Vector3 SO_Direction;

            public Vector4 ProbeOcclusion;

            void ToSphericalHarmonicsL2(ref SphericalHarmonicsL2 sh)
            {
                SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 0, L0);
                SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 1, L1_0);
                SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 2, L1_1);
                SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 3, L1_2);
                SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 4, L2_0);
                SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 5, L2_1);
                SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 6, L2_2);
                SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 7, L2_3);
                SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 8, L2_4);
            }

            void FromSphericalHarmonicsL2(ref SphericalHarmonicsL2 sh)
            {
                L0 = new Vector3(sh[0, 0], sh[1, 0], sh[2, 0]);
                L1_0 = new Vector3(sh[0, 1], sh[1, 1], sh[2, 1]);
                L1_1 = new Vector3(sh[0, 2], sh[1, 2], sh[2, 2]);
                L1_2 = new Vector3(sh[0, 3], sh[1, 3], sh[2, 3]);
                L2_0 = new Vector3(sh[0, 4], sh[1, 4], sh[2, 4]);
                L2_1 = new Vector3(sh[0, 5], sh[1, 5], sh[2, 5]);
                L2_2 = new Vector3(sh[0, 6], sh[1, 6], sh[2, 6]);
                L2_3 = new Vector3(sh[0, 7], sh[1, 7], sh[2, 7]);
                L2_4 = new Vector3(sh[0, 8], sh[1, 8], sh[2, 8]);
            }

            internal void FromSphericalHarmonicsShaderConstants(ProbeReferenceVolume.Cell cell, int probeIdx)
            {

            }
        }
    }
}
#endif