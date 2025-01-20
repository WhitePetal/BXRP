using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BXRenderPipeline
{
    /// <summary>
    /// Modes for Debugging Probes
    /// </summary>
    [GenerateHLSL]
    public enum DebugProbeShadingMode
    {
        /// <summary>
        /// Based on Spherical Harmonics
        /// </summary>
        SH,
        /// <summary>
        /// Based on Spherical Harmonics first band only (ambient)
        /// </summary>
        SHL0,
        /// <summary>
        /// Based on Spherical Harmonics band zero and one only
        /// </summary>
        SHL0L1,
        /// <summary>
        /// Based on validity
        /// </summary>
        Validity,
        /// <summary>
        /// Based on validity over a dilation threshold
        /// </summary>
        ValidityOverDilationThreshold,
        /// <summary>
        /// Based on rendering layer masks
        /// </summary>
        RenderingLayerMasks,
        /// <summary>
        /// Show in red probes that have been made invalid by adjustment volumes. Important to note that this debug view will only show result for volumes still pressent in the scene
        /// </summary>
        InvalidateByAdjustmentVolumes,
        /// <summary>
        /// Base on size
        /// </summary>
        Size,
        /// <summary>
        /// Based on spherical harmonics sky occlusion
        /// </summary>
        SkyOcclusionSH,
        /// <summary>
        /// Based on shading direction
        /// </summary>
        SkyDirection,
        /// <summary>
        /// Occlusion values per probe
        /// </summary>
        ProbeOcclusion,
    }

    internal enum ProbeSamplingDebugUpdate
    {
        Never,
        Once,
        Always
    }

    internal class ProbeSamplingDebugData
    {
        public ProbeSamplingDebugUpdate update = ProbeSamplingDebugUpdate.Never; // When compute buffer should be updated
        public Vector2 coordinates = new Vector2(0.5f, 0.5f);
        public bool forceScreenCenterCoordinates = false; // use screen center instead of mouse position
        public Camera camera = null; // useful in editor when multiple scene tabs are opened
        public bool shortcutPressed = false;
        public GraphicsBuffer positionNormalBuffer; // buffer storing position and normal
    }

    internal class ProbeVolumeDebug : IDebugData
    {
        public bool drawProbes;
        public bool drawBricks;
        public bool drawCells;
        public bool realtimeSubdivision;
        public int subdivisionCellUpdatePerFrame = 4;
        public float subdivisionDelayInSeconds = 1;
        public DebugProbeShadingMode probeShading;
        public float probeSize = 0.3f;
        public float subdivisionViewCullingDistance = 500.0f;
        public float probeCullingDistance = 200.0f;
        public int maxSubdivToVisualize = ProbeBrickIndex.kMaxSubdivisionLevels;
        public int minSubdivToVisualize = 0;
        public float exposureCompensation;
        public bool drawProbeSamplingDebug = false;
        public float probeSamplingDebugSize = 0.3f;
        public bool debugWithSamplingNoise = false;
        public uint samplingRenderingLayer;
        public bool drawVirtualOffsetPush;
        public float offsetSize = 0.025f;
        public bool freezeStreaming;
        public bool displayCellStreamingScore;
        public bool displayIndexFragmentation;
        public int otherStateIndex = 0;
        public bool verboseStreamingLog;
        public bool debugStreaming = false;
        public bool autoDrawProbes = true;
        public bool isolationProbeDebug = true;
        public byte visibleLayers;

        // NOTE: we shold get that from the camera directly instead of storing it as static
        // But we can't access the volume parameters as they are specific to the RP
        public static Vector3 currentOffset;

        internal static int s_ActiveAdjustmentVolumes = 0;

        public ProbeVolumeDebug()
        {
            Init();
        }

        private void Init()
        {
            drawProbes = false;
            drawBricks = false;
            drawCells = false;
            realtimeSubdivision = false;
            subdivisionCellUpdatePerFrame = 4;
            subdivisionDelayInSeconds = 1;
            probeShading = DebugProbeShadingMode.SH;
            probeSize = 0.3f;
            subdivisionViewCullingDistance = 500.0f;
            probeCullingDistance = 200.0f;
            maxSubdivToVisualize = ProbeBrickIndex.kMaxSubdivisionLevels;
            minSubdivToVisualize = 0;
            exposureCompensation = 0.0f;
            drawProbeSamplingDebug = false;
            probeSamplingDebugSize = 0.3f;
            drawVirtualOffsetPush = false;
            offsetSize = 0.025f;
            freezeStreaming = false;
            displayCellStreamingScore = false;
            displayIndexFragmentation = false;
            otherStateIndex = 0;
            autoDrawProbes = true;
            isolationProbeDebug = true;
            visibleLayers = 0xFF;
        }

        public Action GetReset() => () => Init();
    }

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    internal class ProbeVolumeDebugColorPreferences
    {
        internal static Func<Color> GetDetailSubdivisionColor;
        internal static Func<Color> GetMediumSubdivisionColor;
        internal static Func<Color> GetLowSubdivisionColor;
        internal static Func<Color> GetVeryLowSubdivisionColor;
        internal static Func<Color> GetSparseSubdivisionColor;
        internal static Func<Color> GetSparsestSubdivisionColor;

        internal static Color s_DetailSubdivision = new Color32(135, 35, 255, 255);
        internal static Color s_MediumSubdivision = new Color32(54, 208, 228, 255);
        internal static Color s_LowSubdivision = new Color32(255, 100, 45, 255);
        internal static Color s_VeryLowSubdivision = new Color32(52, 87, 255, 255);
        internal static Color s_SparseSubdivision = new Color32(255, 71, 97, 255);
        internal static Color s_SparsestSubdivision = new Color32(200, 227, 39, 255);

        static ProbeVolumeDebugColorPreferences()
        {
#if UNITY_EDITOR
            GetDetailSubdivisionColor = CoreRenderPipelinePreferences.RegisterPreferenceColor("Adaptive Probe Volumes/Level 0 Subdivision", s_DetailSubdivision);
            GetMediumSubdivisionColor = CoreRenderPipelinePreferences.RegisterPreferenceColor("Adaptive Probe Volumes/Level 1 Subdivision", s_MediumSubdivision);
            GetLowSubdivisionColor = CoreRenderPipelinePreferences.RegisterPreferenceColor("Adaptive Probe Volumes/Level 2 Subdivision", s_LowSubdivision);
            GetVeryLowSubdivisionColor = CoreRenderPipelinePreferences.RegisterPreferenceColor("Adaptive Probe Volumes/Level 3 Subdivision", s_VeryLowSubdivision);
            GetSparseSubdivisionColor = CoreRenderPipelinePreferences.RegisterPreferenceColor("Adaptive Probe Volumes/Level 4 Subdivision", s_SparseSubdivision);
            GetSparsestSubdivisionColor = CoreRenderPipelinePreferences.RegisterPreferenceColor("Adaptive Probe Volumes/Level 5 Subdivision", s_SparsestSubdivision);
#endif
        } 
    }

    public partial class ProbeReferenceVolume
    {
        internal class CellInstancedDebugProbes
        {
            public List<Matrix4x4[]> probeBuffers;
            public List<Matrix4x4[]> offsetBuffers;
            public List<MaterialPropertyBlock> props;
        }

        const int kProbesPerBatch = 511;

        /// <summary>
        /// Name of debug panel for Probe Volume
        /// </summary>
        public static readonly string k_DebugPanelName = "Probe Volumes";

        internal ProbeVolumeDebug probeVolumeDebug { get; } = new ProbeVolumeDebug();

        /// <summary>
        /// Colors that can be used for debug visualization of the brick structure subdivision.
        /// </summary>
        public Color[] subdivisionDebugColors { get; } = new Color[ProbeBrickIndex.kMaxSubdivisionLevels];

        private Mesh m_DebugMesh;
        private Mesh debugMesh
        {
            get
            {
                if(m_DebugMesh == null)
                {
                    m_DebugMesh = BXDebugShapes.BuildCustomSphereMesh(0.5f, 9, 8); // (longSubdiv + 1) * latSubdiv + 2 = 82
                    m_DebugMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 9999999.9f); // dirty way of disabling culling (objects spawned at (0.0, 0.0, 0.0) but vertices moved in vertex shader)
                }
                return m_DebugMesh;
            }
        }

        private DebugUI.Widget[] m_DebugItems;
        private Material m_DebugMaterial;

        // Sample position debug
        private Mesh m_DebugProbeSamplingMesh; // mesh with 8 quads, 1 arrow and 2 locators
        private Material m_ProbeSamplingDebugMaterial; // Used to draw probe sampling information (quad with weight, arrow, locator)
        private Material m_ProbeSampingDebugMaterial02; // Used to draw probe sampling information (shaded probes)

        private Texture m_DisplayNumbersTexture;

        internal static ProbeSamplingDebugData probeSamplingDebugData = new ProbeSamplingDebugData();

        private Mesh m_DebugOffsetMesh;
        private Material m_DebugOffsetMaterial;
        private Material m_DebugFragmentationMaterial;
        private Plane[] m_DebugFrustumPlanes = new Plane[6];

        // Scenario blending debug data
        private GUIContent[] m_DebugScenarioNames = new GUIContent[0];
        private int[] m_DebugScenarioValues = new int[0];
        private string m_DebugActiveSceneGUID, m_DebugActiveScenario;
        private DebugUI.EnumField m_DebugScenarioField;

        // Field used for the realtime subdivision preview
        internal Dictionary<Bounds, ProbeBrickIndex.Brick[]> realtimeSubdivisionInfo = new();

        private bool m_MaxSubdivVisualizedIsMaxAvailable = false;

        /// <summary>
        /// Obsolete. Render Probe Volume related debug
        /// </summary>
        /// <param name="camera">The <see cref="Camera"/></param>
        /// <param name="exposureTexture">Texture containing the exposure value for this frame.</param>
        public void RenderDebug(Camera camera, Texture exposureTexture)
        {
            RenderDebug(camera, null, exposureTexture);
        }

        /// <summary>
        /// Render Probe Volume related debug
        /// </summary>
        /// <param name="camera">The <see cref="Camera"/></param>
        /// <param name="options">Options coming from the volume stack.</param>
        /// <param name="exposureTexture">Texture containing the exposure value for this frame.</param>
        public void RenderDebug(Camera camera, ProbeVolumesOptions options, Texture exposureTexture)
        {

        }
    }
}
