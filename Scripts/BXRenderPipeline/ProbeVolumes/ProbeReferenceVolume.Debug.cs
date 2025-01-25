using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
        InvalidatedByAdjustmentVolumes,
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
			if(camera.cameraType != CameraType.Reflection && camera.cameraType != CameraType.Preview)
			{
				if (options != null)
					ProbeVolumeDebug.currentOffset = options.worldOffset_runtime;

				DrawProbeDebug(camera, exposureTexture);
			}
        }

		/// <summary>
		/// Checks if APV sampling debug is enabled
		/// </summary>
		/// <returns>True if APV sampling debug is enabled</returns>
		public bool IsProbeSamplingDebugEnabled()
		{
			return probeSamplingDebugData.update != ProbeSamplingDebugUpdate.Never;
		}

		/// <summary>
		/// Returns the resources used for APV probe sampling debug mode
		/// </summary>
		/// <param name="camera">The camera for which to evaluate the debug mode</param>
		/// <param name="resultBuffer">The buffer that should be filled with position and normal</param>
		/// <param name="coords">The screen space coords to sample the position and normal</param>
		/// <returns>True if the pipeline should write position and normal at coords in resultBuffer</returns>
		public bool GetProbeSamplingDebugResources(Camera camera, out GraphicsBuffer resultBuffer, out Vector2 coords)
		{
			resultBuffer = probeSamplingDebugData.positionNormalBuffer;
			coords = probeSamplingDebugData.coordinates;

			if (!probeVolumeDebug.drawProbeSamplingDebug)
				return false;

#if UNITY_EDITOR
			if (probeSamplingDebugData.camera != camera)
				return false;
#endif

			if (probeSamplingDebugData.update == ProbeSamplingDebugUpdate.Never)
				return false;

			if(probeSamplingDebugData.update == ProbeSamplingDebugUpdate.Once)
			{
				probeSamplingDebugData.update = ProbeSamplingDebugUpdate.Never;
				probeSamplingDebugData.forceScreenCenterCoordinates = false;
			}

			return true;
		}

#if UNITY_EDITOR
		private static void SceneGUI(SceneView sceneView)
		{
			// APV debug needs to detect user keyboard and mouse position to update ProbeSamplingPositionDebug
			Event e = Event.current;

			if (e.control && !ProbeReferenceVolume.probeSamplingDebugData.shortcutPressed)
				ProbeReferenceVolume.probeSamplingDebugData.update = ProbeSamplingDebugUpdate.Always;

			if (!e.control && ProbeReferenceVolume.probeSamplingDebugData.shortcutPressed)
				ProbeReferenceVolume.probeSamplingDebugData.update = ProbeSamplingDebugUpdate.Never;

			ProbeReferenceVolume.probeSamplingDebugData.shortcutPressed = e.control;

			if(e.clickCount > 0 && e.button == 0)
			{
				if (ProbeReferenceVolume.probeSamplingDebugData.shortcutPressed)
					ProbeReferenceVolume.probeSamplingDebugData.update = ProbeSamplingDebugUpdate.Once;
				else
					ProbeReferenceVolume.probeSamplingDebugData.update = ProbeSamplingDebugUpdate.Never;
			}

			if (ProbeReferenceVolume.probeSamplingDebugData.update == ProbeSamplingDebugUpdate.Never)
				return;

			Vector2 screenCoordinates;

			if (ProbeReferenceVolume.probeSamplingDebugData.forceScreenCenterCoordinates)
				screenCoordinates = new Vector2(sceneView.camera.scaledPixelWidth * 0.5f, sceneView.camera.scaledPixelHeight * 0.5f);
			else
				screenCoordinates = HandleUtility.GUIPointToScreenPixelCoordinate(e.mousePosition);

			if (screenCoordinates.x < 0 || screenCoordinates.x > sceneView.camera.scaledPixelWidth ||
				screenCoordinates.y < 0 || screenCoordinates.y > sceneView.camera.scaledPixelHeight)
				return;

			ProbeReferenceVolume.probeSamplingDebugData.camera = sceneView.camera;
			ProbeReferenceVolume.probeSamplingDebugData.coordinates = screenCoordinates;

			if(e.type != EventType.Repaint && e.type != EventType.Layout)
			{
				var go = HandleUtility.PickGameObject(e.mousePosition, false);
				if (go != null && go.TryGetComponent<MeshRenderer>(out var renderer))
					instance.probeVolumeDebug.samplingRenderingLayer = renderer.renderingLayerMask;
			}

			SceneView.currentDrawingSceneView.Repaint(); // useful when 'Always Refresh' is not toggled
		}
#endif

		private bool TryCreateDebugRenderData()
		{
			if (!BXRenderPipeline.TryGetRenderCommonSettings(out var settings))
				return false;

            var debugResources = settings.probeVolumeDebugResources;

            // Unity 2023.x
#if !UNITY_EDITOR // On non editor builds, we need to check if the standalone build contains debug shaders
            //var shaderStrippingSetting = settings.shaderStrippingSetting;
            //if (shaderStrippingSetting != null && shaderStrippingSetting.stripRuntimeDebugShaders)
                return false;
#endif
            m_DebugMaterial = CoreUtils.CreateEngineMaterial(debugResources.probeVolumeDebugShader);
            m_DebugMaterial.enableInstancing = true;

            // Probe Sampling Debug Mesh : useful to show additional information concerning probe sampling for a specific fragment
            // - Arrow : Debug fragment position and normal
            // - Locator : Debug sampling position
            // - 8 Quads : Debug probes weights
            m_DebugProbeSamplingMesh = debugResources.probeSamplingDebugMesh;
            m_DebugProbeSamplingMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 9999999.9f); // dirty way of disabling culling (objects spawned at (0.0, 0.0, 0.0) but vertices moved in vertex shader)
            m_ProbeSamplingDebugMaterial = CoreUtils.CreateEngineMaterial(debugResources.probeVolumeSamplingDebugShader);
            m_ProbeSampingDebugMaterial02 = CoreUtils.CreateEngineMaterial(debugResources.probeVolumeDebugShader);
            m_ProbeSampingDebugMaterial02.enableInstancing = true;

            probeSamplingDebugData.positionNormalBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 2, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector4)));

            m_DisplayNumbersTexture = debugResources.numbersDisplayTex;

            m_DebugOffsetMesh = Resources.GetBuiltinResource<Mesh>("pyramid.fbx");
            m_DebugOffsetMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 9999999.9f); // dirty way of disabling culling (objects spawned at (0.0, 0.0, 0.0) but vertices moved in vertex shader)
            m_DebugOffsetMaterial = CoreUtils.CreateEngineMaterial(debugResources.probeVolumeOffsetDebugShader);
            m_DebugOffsetMaterial.enableInstancing = true;
            m_DebugFragmentationMaterial = CoreUtils.CreateEngineMaterial(debugResources.probeVolumeFragmentationDebugShader);

            // Hard-coded colors for now
            Debug.Assert(ProbeBrickIndex.kMaxSubdivisionLevels == 7); // Update list if ths changes;

            subdivisionDebugColors[0] = ProbeVolumeDebugColorPreferences.s_DetailSubdivision;
            subdivisionDebugColors[1] = ProbeVolumeDebugColorPreferences.s_MediumSubdivision;
            subdivisionDebugColors[2] = ProbeVolumeDebugColorPreferences.s_LowSubdivision;
            subdivisionDebugColors[3] = ProbeVolumeDebugColorPreferences.s_VeryLowSubdivision;
            subdivisionDebugColors[4] = ProbeVolumeDebugColorPreferences.s_SparseSubdivision;
            subdivisionDebugColors[5] = ProbeVolumeDebugColorPreferences.s_SparsestSubdivision;
            subdivisionDebugColors[6] = ProbeVolumeDebugColorPreferences.s_DetailSubdivision;


            return true;
		}

        private void InitializeDebug()
        {
#if UNITY_EDITOR
            SceneView.duringSceneGui += SceneGUI; // Used to get click and keyboard event on scene view for Probe Sampling Debug
#endif
            if (TryCreateDebugRenderData())
                RegisterDebug();

#if UNITY_EDITOR
            UnityEditor.Lightmapping.lightingDataAssetCleared += OnClearLightingdata;
#endif
        }

        void CleanupDebug()
        {
            UnregisterDebug(true);
            CoreUtils.Destroy(m_DebugMaterial);
            CoreUtils.Destroy(m_ProbeSamplingDebugMaterial);
            CoreUtils.Destroy(m_ProbeSampingDebugMaterial02);
            CoreUtils.Destroy(m_DebugOffsetMaterial);
            CoreUtils.Destroy(m_DebugFragmentationMaterial);
            CoreUtils.SafeRelease(probeSamplingDebugData?.positionNormalBuffer);

#if UNITY_EDITOR
            UnityEditor.Lightmapping.lightingDataAssetCleared -= OnClearLightingdata;
            SceneView.duringSceneGui -= SceneGUI;
#endif
        }

        private void DebugCellIndexChanged<T>(DebugUI.Field<T> field, T value)
        {
            ClearDebugData();
        }

        void RegisterDebug()
        {
            void RefreshDebug<T>(DebugUI.Field<T> field, T value)
            {
                UnregisterDebug(false);
                RegisterDebug();
            }

            const float kProbeSizeMin = 0.05f, kProbeSizeMax = 10.0f;
            const float kOffsetSizeMin = 0.001f, kOffsetSizeMax = 0.1f;

            var widgetList = new List<DebugUI.Widget>();

            widgetList = new List<DebugUI.Widget>();

            widgetList.Add(new RuntimeDebugShadersMessageBox());

            var subdivContainer = new DebugUI.Container()
            {
                displayName = "Subdivision Visualization",
                isHiddenCallback = () =>
                {
#if UNITY_EDITOR
                    return false;
#else
                    return false; // Cells / Bricks visualization is not implemented in a runtime compatible way atm.
#endif
                }
            };
            subdivContainer.children.Add(new DebugUI.BoolField
            {
                displayName = "Display Cells",
                tooltip = "Draw Cells used for loading and streaming.",
                getter = () => probeVolumeDebug.drawCells,
                setter = value => probeVolumeDebug.drawCells = value,
                onValueChanged = RefreshDebug
            });
            subdivContainer.children.Add(new DebugUI.BoolField
            {
                displayName = "Display Bricks",
                tooltip = "Display Subdivision bricks",
                getter = () => probeVolumeDebug.drawBricks,
                setter = value => probeVolumeDebug.drawBricks = value,
                onValueChanged = RefreshDebug
            });

            subdivContainer.children.Add(new DebugUI.FloatField
            {
                displayName = "Debug Draw Distance",
                tooltip = "How far from the Scene Camera to draw debug visualization for Cells and Bricks. Large distances can impact Editor performance.",
                getter = () => probeVolumeDebug.subdivisionViewCullingDistance,
                setter = value => probeVolumeDebug.subdivisionViewCullingDistance = value,
                min = () => 0.0f
            });
            widgetList.Add(subdivContainer);

#if UNITY_EDITOR
            var subdivPreviewContainer = new DebugUI.Container()
            {
                displayName = "Subdivision Preview",
                isHiddenCallback = () =>
                {
                    return (!probeVolumeDebug.drawCells && !probeVolumeDebug.drawBricks);
                }
            };
            subdivPreviewContainer.children.Add(new DebugUI.BoolField
            {
                displayName = "Live Updates",
                tooltip = "Enable a preview of Adaptive Probe Volumes data in the Scene without baking. Can impact Editor performance.",
                getter = () => probeVolumeDebug.realtimeSubdivision,
                setter = value => probeVolumeDebug.realtimeSubdivision = value
            });

            var realtimeSubdivisionChildContainer = new DebugUI.Container()
            {
                isHiddenCallback = () => !probeVolumeDebug.realtimeSubdivision
            };
            realtimeSubdivisionChildContainer.children.Add(new DebugUI.IntField
            {
                displayName = "Cell Updates Per Frame",
                tooltip = "The number of Cells, bricks, and probe positions update per frame. Higher numbers can impact Editor performance.",
                getter = () => probeVolumeDebug.subdivisionCellUpdatePerFrame,
                setter = value => probeVolumeDebug.subdivisionCellUpdatePerFrame = value,
                min = () => 1,
                max = () => 100
            });
            realtimeSubdivisionChildContainer.children.Add(new DebugUI.FloatField
            {
                displayName = "Update Frequency",
                tooltip = "Deplay in seconds between updates to Cell, Brick and Probe positions if Live Subdivision Preview if enabled",
                getter = () => probeVolumeDebug.subdivisionDelayInSeconds,
                setter = value => probeVolumeDebug.subdivisionDelayInSeconds = value,
                min = () => 0.1f,
                max = () => 10
            });
            subdivPreviewContainer.children.Add(realtimeSubdivisionChildContainer);
            widgetList.Add(subdivPreviewContainer);
#endif

            widgetList.Add(new RuntimeDebugShadersMessageBox());

            var probeContainer = new DebugUI.Container()
            {
                displayName = "Probe Visualization"
            };

            probeContainer.children.Add(new DebugUI.BoolField
            {
                displayName = "Display Probes",
                tooltip = "Render the debug view showing probe position. Use the shading mode to determine which type of lighting data to visualize.",
                getter = () => probeVolumeDebug.drawProbes,
                setter = value => probeVolumeDebug.drawProbes = value,
                onValueChanged = RefreshDebug
            });
            {
                var probeContainerChildren = new DebugUI.Container()
                {
                    isHiddenCallback = () => !probeVolumeDebug.drawProbes
                };

                probeContainerChildren.children.Add(new DebugUI.EnumField
                {
                    displayName = "Probe Shading Mode",
                    tooltip = "Choose which lighting data to show in the probe debug visualization",
                    getter = () => (int)probeVolumeDebug.probeShading,
                    setter = value => probeVolumeDebug.probeShading = (DebugProbeShadingMode)value,
                    autoEnum = typeof(DebugProbeShadingMode),
                    getIndex = () => (int)probeVolumeDebug.probeShading,
                    setIndex = value => probeVolumeDebug.probeShading = (DebugProbeShadingMode)value
                });
                probeContainerChildren.children.Add(new DebugUI.FloatField
                {
                    displayName = "Debug Size",
                    tooltip = "The size of probes shown in the debug view.",
                    getter = () => probeVolumeDebug.probeSize,
                    setter = value => probeVolumeDebug.probeSize = value,
                    min = () => kProbeSizeMin,
                    max = () => kProbeSizeMax
                });

                //var exposureCompensation = new DebugUI.FloatField
                //{
                //    displayName = "Exposure Compensation",
                //    tooltip = "Modify the brightness of probe visualizations. Decrease this number to make very bright probes more visible.",
                //    getter = () => probeVolumeDebug.exposureCompensation,
                //    setter = value => probeVolumeDebug.exposureCompensation = value,
                //    isHiddenCallback = () =>
                //    {
                //        return probeVolumeDebug.probeShading switch
                //        {
                //            DebugProbeShadingMode.SH => false,
                //            DebugProbeShadingMode.SHL0 => false,
                //            DebugProbeShadingMode.SHL0L1 => false,
                //            DebugProbeShadingMode.SkyOcclusionSH => false,
                //            DebugProbeShadingMode.SkyDirection => false,
                //            DebugProbeShadingMode.ProbeOcclusion => false,
                //            _ => true
                //        };
                //    }
                //};
                //probeContainerChildren.children.Add(exposureCompensation);

                probeContainerChildren.children.Add(new DebugUI.IntField
                {
                    displayName = "Max Subdivisions Displayed",
                    tooltip = "The highest (most dense) probe subdivision level displayed in the debug view",
                    getter = () => probeVolumeDebug.maxSubdivToVisualize,
                    setter = value => probeVolumeDebug.maxSubdivToVisualize = Mathf.Max(0, Mathf.Min(value, GetMaxSubdivision() - 1)),
                    min = () => 0,
                    max = () => Mathf.Max(0, GetMaxSubdivision() - 1)
                });

                probeContainerChildren.children.Add(new DebugUI.IntField
                {
                    displayName = "Min Subdivision Displayed",
                    tooltip = "The lowest (least dense) probe subdivision level displayed in the debug view",
                    getter = () => probeVolumeDebug.minSubdivToVisualize,
                    setter = value => probeVolumeDebug.minSubdivToVisualize = Mathf.Max(0, value),
                    min = () => 0,
                    max = () => Mathf.Max(0, GetMaxSubdivision() - 1)
                });

                probeContainer.children.Add(probeContainerChildren);
            }

            probeContainer.children.Add(new DebugUI.BoolField
            {
                displayName = "Debug Probe Sampling",
                tooltip = "Render the debug view displaying how probes are sampled for a selected pixel. Use the viewport overlay 'SelectPixel' button or Ctrl+Click on the viewport to select the debugged pixel",
                getter = () => probeVolumeDebug.drawProbeSamplingDebug,
                setter = value =>
                {
                    probeVolumeDebug.drawProbeSamplingDebug = value;
                    probeSamplingDebugData.update = ProbeSamplingDebugUpdate.Once;
                    probeSamplingDebugData.forceScreenCenterCoordinates = true;
                },
            });

            var drawProbeSamplingDebugChildren = new DebugUI.Container()
            {
                isHiddenCallback = () => !probeVolumeDebug.drawProbeSamplingDebug
            };

            drawProbeSamplingDebugChildren.children.Add(new DebugUI.FloatField
            {
                displayName = "Debug Size",
                tooltip = "The size of gizmos shown in the debug view",
                getter = () => probeVolumeDebug.probeSamplingDebugSize,
                setter = value => probeVolumeDebug.probeSamplingDebugSize = value,
                min = () => kProbeSizeMin,
                max = () => kProbeSizeMax
            });
            drawProbeSamplingDebugChildren.children.Add(new DebugUI.BoolField
            {
                displayName = "Debug With Sampling Noise",
                tooltip = "Enable Sampling Noise for this debug view. It should be enabled for accuracy but it can make results more difficult to read",
                getter = () => probeVolumeDebug.debugWithSamplingNoise,
                setter = value => probeVolumeDebug.debugWithSamplingNoise = value,
                onValueChanged = RefreshDebug
            });

            probeContainer.children.Add(new DebugUI.BoolField
            {
                displayName = "Virtual Offset Debug",
                tooltip = "Enable Virtual Offset debug visualization. Indicates the offsets applied to probe positions. These are used to capture lighting when probes are considered invalid.",
                getter = () => probeVolumeDebug.drawVirtualOffsetPush,
                setter = value =>
                {
                    probeVolumeDebug.drawVirtualOffsetPush = value;

                    if (probeVolumeDebug.drawVirtualOffsetPush && probeVolumeDebug.drawProbes && m_CurrentBakingSet != null)
                    {
                        // If probes are being drawn when enabling offset, automatically scale them down to a reasonable size so the arrows aren't obscured by the probes.
                        var searchDistance = CellSize(0) * MinBrickSize() / ProbeBrickPool.kBrickCellCount * m_CurrentBakingSet.settings.virtualOffsetSettings.searchMultiplier + m_CurrentBakingSet.settings.virtualOffsetSettings.outOfGeoOffset;
                        probeVolumeDebug.probeSize = Mathf.Min(probeVolumeDebug.probeSize, Mathf.Clamp(searchDistance, kProbeSizeMin, kProbeSizeMax));
                    }
                }
            });

            var drawVirtualOffsetDebugChildren = new DebugUI.Container()
            {
                isHiddenCallback = () => !probeVolumeDebug.drawVirtualOffsetPush
            };

            var voOffset = new DebugUI.FloatField
            {
                displayName = "Debug Size",
                tooltip = "Modify the size of the arrows used in the virtual offset debug visualization.",
                getter = () => probeVolumeDebug.offsetSize,
                setter = value => probeVolumeDebug.offsetSize = value,
                min = () => kOffsetSizeMin,
                max = () => kOffsetSizeMax,
                isHiddenCallback = () => !probeVolumeDebug.drawVirtualOffsetPush
            };
            drawVirtualOffsetDebugChildren.children.Add(voOffset);
            probeContainer.children.Add(drawVirtualOffsetDebugChildren);

            probeContainer.children.Add(new DebugUI.FloatField
            {
                displayName = "Debug Draw Distance",
                tooltip = "How far from the Scene Camera to draw probe debug visualizations. Large distances can impact Editor performance",
                getter = () => probeVolumeDebug.probeCullingDistance,
                setter = value => probeVolumeDebug.probeCullingDistance = value,
                min = () => 0.0f
            });
            widgetList.Add(probeContainer);

            var adjustmentContainer = new DebugUI.Container()
            {
                displayName = "Probe Adjustment Volumes"
            };
            adjustmentContainer.children.Add(new DebugUI.BoolField
            {
                displayName = "Auto Display Probes",
                tooltip = "When enabled and a Probe Adjustment Volume is selected, automatically display the probes.",
                getter = () => probeVolumeDebug.autoDrawProbes,
                setter = value => probeVolumeDebug.autoDrawProbes = value,
                onValueChanged = RefreshDebug
            });
            adjustmentContainer.children.Add(new DebugUI.BoolField
            {
                displayName = "Isolate Affected",
                tooltip = "When enabled, only displayed probes in the influence of the currently selected Probe Adjustment Volumes",
                getter = () => probeVolumeDebug.isolationProbeDebug,
                setter = value => probeVolumeDebug.isolationProbeDebug = value,
                onValueChanged = RefreshDebug
            });
            widgetList.Add(adjustmentContainer);

            var streamingContainer = new DebugUI.Container()
            {
                displayName = "Streaming",
                isHiddenCallback = () => !(gpuStreamingEnabled || diskStreamingEnabled)
            };
            streamingContainer.children.Add(new DebugUI.BoolField
            {
                displayName = "Freeze Streaming",
                tooltip = "Stop Unity from streaming probe data in or out of GPU memory",
                getter = () => probeVolumeDebug.freezeStreaming,
                setter = value => probeVolumeDebug.freezeStreaming = value
            });
            streamingContainer.children.Add(new DebugUI.BoolField
            {
                displayName = "Display Streaming Score",
                getter = () => probeVolumeDebug.displayCellStreamingScore,
                setter = value => probeVolumeDebug.displayCellStreamingScore = value
            });
            streamingContainer.children.Add(new DebugUI.BoolField
            {
                displayName = "Maximum cell streaming",
                tooltip = "Enable streaming as many cells as possible every frame",
                getter = () => instance.loadMaxCellsPerFrame,
                setter = value => instance.loadMaxCellsPerFrame = value
            });

            var maxCellStreamingContainerChildren = new DebugUI.Container()
            {
                isHiddenCallback = () => instance.loadMaxCellsPerFrame
            };
            maxCellStreamingContainerChildren.children.Add(new DebugUI.IntField
            {
                displayName = "Loaded Cells Per Frame",
                tooltip = "Determines the maximum number of Cells Unity streams per frame. Loading more Cells per frame can impact performance.",
                getter = () => instance.numberOfCellsLoadedPerFrame,
                setter = value => instance.SetNumberOfCellsLoadedPerFrame(value),
                min = () => 1,
                max = () => kMaxCellLoadedPerFrame
            });
            streamingContainer.children.Add(maxCellStreamingContainerChildren);

            // Those are mostly for internal dev purpose
            if (Debug.isDebugBuild)
            {
				streamingContainer.children.Add(new DebugUI.BoolField
				{
					displayName = "Display Index Fragmentation",
					getter = () => probeVolumeDebug.displayIndexFragmentation,
					setter = value => probeVolumeDebug.displayIndexFragmentation = value
				});
				var indexDefragContainerChildren = new DebugUI.Container()
				{
					isHiddenCallback = () => !probeVolumeDebug.displayIndexFragmentation
				};
				indexDefragContainerChildren.children.Add(new DebugUI.Value
				{
					displayName = "Index Fragmentation Rate",
					getter = () => instance.indexFragmentationRate
				});
				streamingContainer.children.Add(indexDefragContainerChildren);
				streamingContainer.children.Add(new DebugUI.BoolField
				{
					displayName = "Verbose Log",
					getter = () => probeVolumeDebug.verboseStreamingLog,
					setter = value => probeVolumeDebug.verboseStreamingLog = value
				});
				streamingContainer.children.Add(new DebugUI.BoolField
				{
					displayName = "Debug Streaming",
					getter = () => probeVolumeDebug.debugStreaming,
					setter = value => probeVolumeDebug.debugStreaming = value
				});
            }

			widgetList.Add(streamingContainer);

			if(supportScenarioBlending && m_CurrentBakingSet != null)
			{
				var blendingContainer = new DebugUI.Container()
				{
					displayName = "Scenario Blending"
				};
				blendingContainer.children.Add(new DebugUI.IntField
				{
					displayName = "Number Of Cells Blended Per Frame",
					getter = () => instance.numberOfCellsBlendedPerFram,
					setter = value => instance.numberOfCellsBlendedPerFram = value,
					min = () => 0
				});
				blendingContainer.children.Add(new DebugUI.FloatField
				{
					displayName = "Turnover Rate",
					getter = () => instance.turnoverRate,
					setter = value => instance.turnoverRate = value,
					min = () => 0,
					max = () => 1
				});

				void RefreshScenarioNames(string guid)
				{
					HashSet<string> allScenarios = new();
					foreach(var set in Resources.FindObjectsOfTypeAll<ProbeVolumeBakingSet>())
					{
						if (!set.sceneGUIDs.Contains(guid))
							continue;
						foreach (var scenario in set.lightingScenarios)
							allScenarios.Add(scenario);
					}

					allScenarios.Remove(m_CurrentBakingSet.lightingScenario);
					if(m_DebugActiveSceneGUID == guid && allScenarios.Count + 1 == m_DebugScenarioNames.Length && m_DebugActiveScenario == m_CurrentBakingSet.lightingScenario)
					{
						return;
					}

					int i = 0;
					ArrayExtensions.ResizeArray(ref m_DebugScenarioNames, allScenarios.Count + 1);
					ArrayExtensions.ResizeArray(ref m_DebugScenarioValues, allScenarios.Count + 1);
					m_DebugScenarioNames[0] = new GUIContent("None");
					m_DebugScenarioValues[0] = 0;
					foreach(var scenario in allScenarios)
					{
						i++;
						m_DebugScenarioNames[i] = new GUIContent(scenario);
						m_DebugScenarioValues[i] = i;
					}

					m_DebugActiveSceneGUID = guid;
					m_DebugActiveScenario = m_CurrentBakingSet.lightingScenario;
					m_DebugScenarioField.enumNames = m_DebugScenarioNames;
					m_DebugScenarioField.enumValues = m_DebugScenarioValues;
					if (probeVolumeDebug.otherStateIndex >= m_DebugScenarioNames.Length)
						probeVolumeDebug.otherStateIndex = 0;
				}

				m_DebugScenarioField = new DebugUI.EnumField
				{
					displayName = "Scenario Blend Target",
					tooltip = "Select another lighting scenario to blend with the active lighting scenario",
					enumNames = m_DebugScenarioNames,
					enumValues = m_DebugScenarioValues,
					getIndex = () =>
					{
						if (m_CurrentBakingSet == null)
							return 0;
						RefreshScenarioNames(GetSceneGUID(SceneManager.GetActiveScene()));

						probeVolumeDebug.otherStateIndex = 0;
						if (!string.IsNullOrEmpty(m_CurrentBakingSet.otherScenario))
						{
							for (int i = 1; i < m_DebugScenarioNames.Length; i++)
							{
								if (m_DebugScenarioNames[i].text == m_CurrentBakingSet.otherScenario)
								{
									probeVolumeDebug.otherStateIndex = i;
									break;
								}
							}
						}
						return probeVolumeDebug.otherStateIndex;
					},
					setIndex = value =>
					{
						string other = value == 0 ? null : m_DebugScenarioNames[value].text;
						m_CurrentBakingSet.BlendLightingScenario(other, m_CurrentBakingSet.scenarioBlendingFactor);
						probeVolumeDebug.otherStateIndex = value;
					},
					getter = () => probeVolumeDebug.otherStateIndex,
					setter = value => probeVolumeDebug.otherStateIndex = value
				};

				blendingContainer.children.Add(m_DebugScenarioField);
				blendingContainer.children.Add(new DebugUI.FloatField
				{
					displayName = "Scenario Blending Factor",
					tooltip = "Blend between lighting scenarios by adjusting this slider",
					getter = () => instance.scenarioBlendingFactor,
					setter = value => instance.scenarioBlendingFactor = value,
					min = () => 0.0f,
					max = () => 1.0f
				});

				widgetList.Add(blendingContainer);
			}

			if(widgetList.Count > 0)
			{
				m_DebugItems = widgetList.ToArray();
				var panel = DebugManager.instance.GetPanel(k_DebugPanelName, true);
				panel.children.Add(m_DebugItems);
			}

			DebugManager debugManager = DebugManager.instance;
			debugManager.RegisterData(probeVolumeDebug);
        }

		void UnregisterDebug(bool destoryPanel)
		{
			if (destoryPanel)
				DebugManager.instance.RemovePanel(k_DebugPanelName);
			else
				DebugManager.instance.GetPanel(k_DebugPanelName, false).children.Remove(m_DebugItems);
		}

		private void ClearDebugData()
		{
			realtimeSubdivisionInfo.Clear();
		}

		private void OnClearLightingdata()
		{
			ClearDebugData();
		}

		class RenderFragmentationOverlayPassData
		{
			public Material debugFragmentationMaterial;
			public DebugOverlay debugOverlay;
			public int chunkCount;
			public ComputeBuffer debugFragmentationData;
			public TextureHandle colorBuffer;
			public TextureHandle depthBuffer;
		}

		/// <summary>
		/// Render a debug view showing fragmentation of the GPU memory
		/// </summary>
		/// <param name="renderGraph">The RenderGraph responsible for executing this pass</param>
		/// <param name="colorBuffer">The color buffer where the overlay will be rendered</param>
		/// <param name="depthBuffer">The depth buffer used for depth-testing the overly</param>
		/// <param name="debugOverlay">The debug overlay manager to orchestrate multiple overlays</param>
		public void RenderFragmentationOverlay(RenderGraph renderGraph, TextureHandle colorBuffer, TextureHandle depthBuffer, DebugOverlay debugOverlay)
		{
			if (!m_ProbeReferenceVolumeInit || !probeVolumeDebug.displayIndexFragmentation)
				return;

			using(var builder = renderGraph.AddRenderPass<RenderFragmentationOverlayPassData>("APVFragmentationOverlay", out var passData))
			{
				passData.debugOverlay = debugOverlay;
				passData.debugFragmentationMaterial = m_DebugFragmentationMaterial;
				passData.colorBuffer = builder.UseColorBuffer(colorBuffer, 0);
				passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);
				passData.debugFragmentationData = m_Index.GetDebugFragmentationBuffer();
				passData.chunkCount = passData.debugFragmentationData.count;

				builder.SetRenderFunc(
					(RenderFragmentationOverlayPassData data, RenderGraphContext ctx) =>
					{
						var mpb = ctx.renderGraphPool.GetTempMaterialPropertyBlock();

						data.debugOverlay.SetViewport(ctx.cmd);
						mpb.SetInt("_ChunkCount", data.chunkCount);
						mpb.SetBuffer("_DebugFragmentation", data.debugFragmentationData);
						ctx.cmd.DrawProcedural(Matrix4x4.identity, data.debugFragmentationMaterial, 0, MeshTopology.Triangles, 3, 1, mpb);
						data.debugOverlay.Next();
					});
			}
		}

		private bool ShouldCullCell(Vector3 cellPosition, Transform cameraTransform, Plane[] frustumPlanes)
		{
			var volumeAABB = GetCellBounds(cellPosition);
			var cellSize = MaxBrickSize();

			// We do coarse culling with cell, finer culling later
			float distanceRoundedUpWithCellSize = Mathf.CeilToInt(probeVolumeDebug.probeCullingDistance / cellSize) * cellSize;

			if (Vector3.Distance(cameraTransform.position, volumeAABB.center) > distanceRoundedUpWithCellSize)
				return true;

			return !GeometryUtility.TestPlanesAABB(frustumPlanes, volumeAABB);
		}


		private Bounds GetCellBounds(Vector3 cellPosition)
		{
			var cellSize = MaxBrickSize();
			var cellOffset = ProbeOffset() + ProbeVolumeDebug.currentOffset;
			Vector3 cellCenterWS = cellOffset + cellPosition * cellSize + Vector3.one * (cellSize * 0.5f);

			return new Bounds(cellCenterWS, cellSize * Vector3.one);
		}

		private static Vector4[] s_BoundsArray = new Vector4[16 * 3];

		private static void UpdateDebugFromSelection(ref Vector4[] _AdjustmentVolumeBounds, ref int _AdjustmentVolumeCount)
		{
			if (ProbeVolumeDebug.s_ActiveAdjustmentVolumes == 0)
				return;

#if UNITY_EDITOR
			foreach(var touchup in Selection.GetFiltered<ProbeAdjustmentVolume>(SelectionMode.Unfiltered))
			{
				if (!touchup.isActiveAndEnabled) continue;
			}
#endif
		}



		private void DrawProbeDebug(Camera camera, Texture exposureTexture)
        {

        }
	}
}
