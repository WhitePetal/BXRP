using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace BXRenderPipeline
{
#if UNITY_EDITOR

    /// <summary>
    /// A manager to enqueue extra probe rendering outside of probe volumes.
    /// </summary>
    public class AdditionalGIBakeRequestsManager
    {
        // The baking ID for the extra requests
        // TODO: Need to ensure this never conflicts with bake IDs from others interacting with the API.
        // In our project, this is ProbeVolumes.
        internal static readonly int s_BakingID = 912345678;

        private static AdditionalGIBakeRequestsManager s_Instance = new AdditionalGIBakeRequestsManager();

        /// <summary>
        /// Get the manager that governs the additional light probe rendering requests.
        /// </summary>
        public static AdditionalGIBakeRequestsManager instance { get { return s_Instance; } }

        private const float kInvalidSH = 1f;
        private const float kValidSHThresh = 0.33f;

        private static Dictionary<int, SphericalHarmonicsL2> m_SHCoefficients = new Dictionary<int, SphericalHarmonicsL2>();
        private static Dictionary<int, float> m_SHValidity = new Dictionary<int, float>();
        private static Dictionary<int, Vector3> m_RequestPositions = new Dictionary<int, Vector3>();

        /// <summary>
        /// Enqueue a request for probe rendering at the specified location.
        /// </summary>
        /// <param name="capturePosition">The position at which a probe is baked.</param>
        /// <param name="probeInstanceID">The instance ID of the probe doing the request.</param>
        public void EnqueueRequest(Vector3 capturePosition, int probeInstanceID)
        {
            m_SHCoefficients[probeInstanceID] = new SphericalHarmonicsL2();
            m_SHValidity[probeInstanceID] = kInvalidSH;
            m_RequestPositions[probeInstanceID] = capturePosition;
        }

        /// <summary>
        /// Dequeue a request for probe rendering.
        /// </summary>
        /// <param name="probeInstanceID">The instance ID of the probe for which we want to dequeue a request.</param>
        public void DequeueRequest(int probeInstanceID)
        {
            if (m_SHCoefficients.ContainsKey(probeInstanceID))
            {
                m_SHCoefficients.Remove(probeInstanceID);
                m_SHValidity.Remove(probeInstanceID);
                m_RequestPositions.Remove(probeInstanceID);
            }
        }

        /// <summary>
        /// Retrieve the result of a capture request, it will return false if the request has not been fulfilled yet or the request ID is invalid.
        /// </summary>
        /// <param name="probeInstanceID">TheThe instance ID of the probe doing the request.</param>
        /// <param name="sh">The output SH coefficients that have been computed.</param>
        /// <param name="pos">The position for which the computed SH coefficients are valid.</param>
        /// <returns></returns>
        public bool RetrieveProbeSH(int probeInstanceID, out SphericalHarmonicsL2 sh, out Vector3 pos)
        {
            if (m_SHCoefficients.ContainsKey(probeInstanceID))
            {
                sh = m_SHCoefficients[probeInstanceID];
                pos = m_RequestPositions[probeInstanceID];
                return m_SHValidity[probeInstanceID] < kValidSHThresh;
            }

            sh = new SphericalHarmonicsL2();
            pos = Vector3.negativeInfinity;
            return false;
        }

        internal static bool GetPositionForRequest(int probeInstanceID, out Vector3 pos)
        {
            if (m_SHCoefficients.ContainsKey(probeInstanceID))
            {
                pos = m_RequestPositions[probeInstanceID];
                return true;
            }

            pos = Vector3.negativeInfinity;
            return false;
        }

        /// <summary>
        /// Update the capture location for the probe request.
        /// </summary>
        /// <param name="probeInstanceID">The instance ID of the probe doing the request and that wants the capture position updated.</param>
        /// <param name="newPosition">The position at which a probe is baked.</param>
        public void UpdatePositionForRequest(int probeInstanceID, Vector3 newPosition)
        {
            if (m_SHCoefficients.ContainsKey(probeInstanceID))
            {
                m_RequestPositions[probeInstanceID] = newPosition;
                m_SHCoefficients[probeInstanceID] = new SphericalHarmonicsL2();
                m_SHValidity[probeInstanceID] = kInvalidSH;
            }
            else
            {
                EnqueueRequest(newPosition, probeInstanceID);
            }
        }

        internal static List<Vector3> GetProbeNormalizationRequests() => new List<Vector3>(m_RequestPositions.Values);

        internal static void OnAdditionalProbesBakeCompleted(NativeArray<SphericalHarmonicsL2> sh, NativeArray<float> validity)
        {
            SetSHCoefficients(sh, validity);

            ProbeReferenceVolume.instance.retrieveExtraDataAction?.Invoke(new ProbeReferenceVolume.ExtraDataActionInput());
        }

        private static bool IsZero(in SphericalHarmonicsL2 s)
        {
            for (var r = 0; r < 3; ++r)
            {
                for (var c = 0; c < 9; ++c)
                {
                    if (s[r, c] != 0f)
                        return false;
                }
            }
            return true;
        }

        private static void SetSHCoefficients(NativeArray<SphericalHarmonicsL2> sh, NativeArray<float> validity)
        {
            Debug.Assert(sh.Length == m_SHCoefficients.Count);
            Debug.Assert(sh.Length == validity.Length);

            List<int> requestsInstanceIDs = new List<int>(m_SHCoefficients.Keys);

            for (int i = 0; i < sh.Length; ++i)
                SetSHCoefficients(requestsInstanceIDs[i], sh[i], validity[i]);
        }

        internal static void SetSHCoefficients(int instanceID, SphericalHarmonicsL2 sh, float validity)
        {
            if(validity < kValidSHThresh)
            {
                if(IsZero(in sh))
                {
                    // Use max value as a sentinel to explicitly pass coefficients to light loop that cancel out reflection probe contribution
                    const float k = float.MaxValue;
                    sh.AddAmbientLight(new Color(k, k, k));
                }
            }

            m_SHCoefficients[instanceID] = sh;
            m_SHValidity[instanceID] = validity;
        }
    }

#endif
}
