
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace BXGeometryGraph
{
	delegate void OnPrimaryMasterChanged();

	class PreviewManager : IDisposable
    {
        private GraphData m_Graph;
        private MessageManager m_Messenger;

        private MaterialPropertyBlock m_SharedPreviewPropertyBlock; // stores preview properties (shared among ALL preview nodes)

        private Dictionary<AbstractGeometryNode, PreviewRenderData> m_RenderDatas = new Dictionary<AbstractGeometryNode, PreviewRenderData>();  // stores all of the PreviewRendererData, mapped by node
        private PreviewRenderData m_MasterRenderData;                                                               // ref to preview renderer data for the master node

        private int m_MaxPreviewsCompiling = 2;

        // state trackers
        private HashSet<AbstractGeometryNode> m_NodesShaderChanged = new HashSet<AbstractGeometryNode>();           // nodes whose shader code has changed, this node and nodes that read from it are put into NeedRecompile
        private HashSet<AbstractGeometryNode> m_NodesPropertyChanged = new HashSet<AbstractGeometryNode>();         // nodes whose property values have changed, the properties will need to be updated and all nodes that use that property re-rendered

        private HashSet<PreviewRenderData> m_PreviewsNeedsRecompile = new HashSet<PreviewRenderData>();             // previews we need to recompile the preview shader
        private HashSet<PreviewRenderData> m_PreviewsCompiling = new HashSet<PreviewRenderData>();                  // previews currently being compiled
        private HashSet<PreviewRenderData> m_PreviewsToDraw = new HashSet<PreviewRenderData>();                     // previews to re-render the texture (either because shader compile changed or property changed)
        private HashSet<PreviewRenderData> m_TimedPreviews = new HashSet<PreviewRenderData>();

        private double m_LastTimedUpdateTime = 0.0f;

        private bool m_TopologyDirty;                                                                               // indicates topology changed, used to rebuild timed node list and preview type (2D/3D) inheritance.

        private HashSet<BlockNode> m_MasterNodeTempBlocks = new HashSet<BlockNode>();                               // temp blocks used by the most recent master node preview generation.

        // used to detect when texture assets have been modified
        private HashSet<string> m_PreviewTextureGUIDs = new HashSet<string>();
        private PreviewSceneResources m_SceneResources;
        private Texture2D m_ErrorTexture;
        private Vector2? m_NewMasterPreviewSize;

        private const AbstractGeometryNode kMasterProxyNode = null;

        public PreviewRenderData masterRenderData
        {
            get { return m_MasterRenderData; }
        }

        public PreviewManager(GraphData graph, MessageManager messenger)
        {
            m_SharedPreviewPropertyBlock = new MaterialPropertyBlock();
            m_Graph = graph;
            m_Messenger = messenger;
            m_ErrorTexture = GenerateFourSquare(Color.magenta, Color.black);
            m_SceneResources = new PreviewSceneResources();

            foreach (var node in m_Graph.GetNodes<AbstractGeometryNode>())
                AddPreview(node);

            AddMasterPreview();
        }

        static Texture2D GenerateFourSquare(Color c1, Color c2)
        {
            var tex = new Texture2D(2, 2);
            tex.SetPixel(0, 0, c1);
            tex.SetPixel(0, 1, c2);
            tex.SetPixel(1, 0, c2);
            tex.SetPixel(1, 1, c1);
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            return tex;
        }

        public void ResizeMasterPreview(Vector2 newSize)
        {
            m_NewMasterPreviewSize = newSize;
        }

        public PreviewRenderData GetPreviewRenderData(AbstractGeometryNode node)
        {
            PreviewRenderData result = null;
            if (node == kMasterProxyNode ||
                node is BlockNode ||
                node == m_Graph.outputNode) // the outputNode, if it exists, is mapped to master
            {
                result = m_MasterRenderData;
            }
            else
            {
                m_RenderDatas.TryGetValue(node, out result);
            }

            return result;
        }

        void AddMasterPreview()
        {
            m_MasterRenderData = new PreviewRenderData
            {
                previewName = "Master Preview",
                renderTexture =
                    new RenderTexture(400, 400, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default)
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    },
                previewMode = PreviewMode.Preview3D,
            };

            m_MasterRenderData.renderTexture.Create();

            var shaderData = new PreviewGeometryData
            {
                // even though a SubGraphOutputNode can be directly mapped to master (via m_Graph.outputNode)
                // we always keep master node associated with kMasterProxyNode instead
                // just easier if the association is always dynamic
                node = kMasterProxyNode,
                passesCompiling = 0,
                isOutOfDate = true,
                hasError = false,
            };
            m_MasterRenderData.shaderData = shaderData;

            m_PreviewsNeedsRecompile.Add(m_MasterRenderData);
            m_PreviewsToDraw.Add(m_MasterRenderData);
            m_TopologyDirty = true;
        }

        public void UpdateMasterPreview(ModificationScope scope)
        {
            if (scope == ModificationScope.Topological ||
                scope == ModificationScope.Graph)
            {
                // mark the master preview for recompile if it exists
                // if not, no need to do it here, because it is always marked for recompile on creation
                if (m_MasterRenderData != null)
                    m_PreviewsNeedsRecompile.Add(m_MasterRenderData);
                m_TopologyDirty = true;
            }
            else if (scope == ModificationScope.Node)
            {
                if (m_MasterRenderData != null)
                    m_PreviewsToDraw.Add(m_MasterRenderData);
            }
        }

        void AddPreview(AbstractGeometryNode node)
        {
            Assert.IsNotNull(node);

            // BlockNodes have no preview for themselves, but are mapped to the "Master" preview
            // SubGraphOutput nodes have their own previews, but will use the "Master" preview if they are the m_Graph.outputNode
            if (node is BlockNode)
            {
                node.RegisterCallback(OnNodeModified);
                UpdateMasterPreview(ModificationScope.Topological);
                m_NodesPropertyChanged.Add(node);
                return;
            }

            var renderData = new PreviewRenderData
            {
                previewName = node.name ?? "UNNAMED NODE",
                renderTexture =
                    new RenderTexture(200, 200, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default)
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    }
            };

            renderData.renderTexture.Create();

            var shaderData = new PreviewGeometryData
            {
                node = node,
                passesCompiling = 0,
                isOutOfDate = true,
                hasError = false,
            };
            renderData.shaderData = shaderData;

            m_RenderDatas.Add(node, renderData);
            node.RegisterCallback(OnNodeModified);

            m_PreviewsNeedsRecompile.Add(renderData);
            m_NodesPropertyChanged.Add(node);
            m_TopologyDirty = true;
        }

        void OnNodeModified(AbstractGeometryNode node, ModificationScope scope)
        {
            Assert.IsNotNull(node);

            if (scope == ModificationScope.Topological ||
                scope == ModificationScope.Graph)
            {
                m_NodesShaderChanged.Add(node);     // shader code for this node changed, this will trigger m_PreviewsShaderChanged for all nodes downstream
                m_NodesPropertyChanged.Add(node);   // properties could also have changed at the same time and need to be re-collected
                m_TopologyDirty = true;
            }
            else if (scope == ModificationScope.Node)
            {
                // if we only changed a constant on the node, we don't have to recompile the shader for it, just re-render it with the updated constant
                // should instead flag m_NodesConstantChanged
                m_NodesPropertyChanged.Add(node);
            }
        }

        // temp structures that are kept around statically to avoid GC churn (not thread safe)
        static Stack<AbstractGeometryNode> m_TempNodeWave = new Stack<AbstractGeometryNode>();
        static HashSet<AbstractGeometryNode> m_TempAddedToNodeWave = new HashSet<AbstractGeometryNode>();

        // cache the Action to avoid GC
        static Action<AbstractGeometryNode> AddNextLevelNodesToWave =
            nextLevelNode =>
            {
                if (!m_TempAddedToNodeWave.Contains(nextLevelNode))
                {
                    m_TempNodeWave.Push(nextLevelNode);
                    m_TempAddedToNodeWave.Add(nextLevelNode);
                }
            };

        internal enum PropagationDirection
        {
            Upstream,
            Downstream
        }

        // ADDs all nodes in sources, and all nodes in the given direction relative to them, into result
        // sources and result can be the same HashSet
        private static readonly ProfilerMarker PropagateNodesMarker = new ProfilerMarker("PropagateNodes");
        internal static void PropagateNodes(HashSet<AbstractGeometryNode> sources, PropagationDirection dir, HashSet<AbstractGeometryNode> result)
        {
            using (PropagateNodesMarker.Auto())
                if (sources.Count > 0)
                {
                    // NodeWave represents the list of nodes we still have to process and add to result
                    m_TempNodeWave.Clear();
                    m_TempAddedToNodeWave.Clear();
                    foreach (var node in sources)
                    {
                        m_TempNodeWave.Push(node);
                        m_TempAddedToNodeWave.Add(node);
                    }

                    while (m_TempNodeWave.Count > 0)
                    {
                        var node = m_TempNodeWave.Pop();
                        if (node == null)
                            continue;

                        result.Add(node);

                        // grab connected nodes in propagation direction, add them to the node wave
                        ForeachConnectedNode(node, dir, AddNextLevelNodesToWave);
                    }

                    // clean up any temp data
                    m_TempNodeWave.Clear();
                    m_TempAddedToNodeWave.Clear();
                }
        }

        static void ForeachConnectedNode(AbstractGeometryNode node, PropagationDirection dir, Action<AbstractGeometryNode> action)
        {
            using (var tempEdges = PooledList<IEdge>.Get())
            using (var tempSlots = PooledList<GeometrySlot>.Get())
            {
                // Loop through all nodes that the node feeds into.
                if (dir == PropagationDirection.Downstream)
                    node.GetOutputSlots(tempSlots);
                else
                    node.GetInputSlots(tempSlots);

                foreach (var slot in tempSlots)
                {
                    // get the edges out of each slot
                    tempEdges.Clear();                            // and here we serialize another list, ouch!
                    node.owner.GetEdges(slot.slotReference, tempEdges);
                    foreach (var edge in tempEdges)
                    {
                        // We look at each node we feed into.
                        var connectedSlot = (dir == PropagationDirection.Downstream) ? edge.inputSlot : edge.outputSlot;
                        var connectedNode = connectedSlot.node;

                        action(connectedNode);
                    }
                }
            }

            // Custom Interpolator Blocks have implied connections to their Custom Interpolator Nodes...
            //if (dir == PropagationDirection.Downstream && node is BlockNode bnode && bnode.isCustomBlock)
            //{
            //    foreach (var cin in CustomInterpolatorUtils.GetCustomBlockNodeDependents(bnode))
            //    {
            //        action(cin);
            //    }
            //}
            // ... Just as custom Interpolator Nodes have implied connections to their custom interpolator blocks
            //if (dir == PropagationDirection.Upstream && node is CustomInterpolatorNode ciNode && ciNode.e_targetBlockNode != null)
            //{
            //    action(ciNode.e_targetBlockNode);
            //}
        }

        public void ReloadChangedFiles(string ChangedFileDependencyGUIDs)
        {
            if (m_PreviewTextureGUIDs.Contains(ChangedFileDependencyGUIDs))
            {
                // have to setup the textures on the MaterialPropertyBlock again
                // easiest is to just mark everything as needing property update
                m_NodesPropertyChanged.UnionWith(m_RenderDatas.Keys);
            }
        }

        public void HandleGraphChanges()
        {
            foreach (var node in m_Graph.addedNodes)
            {
                AddPreview(node);
                m_TopologyDirty = true;
            }

            foreach (var edge in m_Graph.addedEdges)
            {
                var node = edge.inputSlot.node;
                if (node != null)
                {
                    if ((node is BlockNode) || (node is SubGraphOutputNode))
                        UpdateMasterPreview(ModificationScope.Topological);
                    else
                        m_NodesShaderChanged.Add(node);
                    m_TopologyDirty = true;
                }
            }

            foreach (var node in m_Graph.removedNodes)
            {
                DestroyPreview(node);
                m_TopologyDirty = true;
            }

            foreach (var edge in m_Graph.removedEdges)
            {
                var node = edge.inputSlot.node;
                if ((node is BlockNode) || (node is SubGraphOutputNode))
                {
                    UpdateMasterPreview(ModificationScope.Topological);
                }

                m_NodesShaderChanged.Add(node);
                //When an edge gets deleted, if the node had the edge on creation, the properties would get out of sync and no value would get set.
                //Fix for https://fogbugz.unity3d.com/f/cases/1284033/
                m_NodesPropertyChanged.Add(node);

                m_TopologyDirty = true;
            }

            foreach (var edge in m_Graph.addedEdges)
            {
                var node = edge.inputSlot.node;
                if (node != null)
                {
                    if ((node is BlockNode) || (node is SubGraphOutputNode))
                    {
                        UpdateMasterPreview(ModificationScope.Topological);
                    }

                    m_NodesShaderChanged.Add(node);
                    m_TopologyDirty = true;
                }
            }

            // remove the nodes from the state trackers
            m_NodesShaderChanged.ExceptWith(m_Graph.removedNodes);
            m_NodesPropertyChanged.ExceptWith(m_Graph.removedNodes);

            m_Messenger.ClearNodesFromProvider(this, m_Graph.removedNodes);
        }

        private static readonly ProfilerMarker CollectPreviewPropertiesMarker = new ProfilerMarker("CollectPreviewProperties");
        void CollectPreviewProperties(IEnumerable<AbstractGeometryNode> nodesToCollect, PooledList<PreviewProperty> perMaterialPreviewProperties)
        {
            using (CollectPreviewPropertiesMarker.Auto())
            using (var tempPreviewProps = PooledList<PreviewProperty>.Get())
            {
                // collect from all of the changed nodes
                foreach (var propNode in nodesToCollect)
                    propNode.CollectPreviewGeometryProperties(tempPreviewProps);

                // also grab all graph properties (they are updated every frame)
                foreach (var prop in m_Graph.properties)
                    tempPreviewProps.Add(prop.GetPreviewGeometryProperty());

                foreach (var previewProperty in tempPreviewProps)
                {
                    previewProperty.SetValueOnGeometryPropertyBlock(m_SharedPreviewPropertyBlock);

                    // record guids for any texture properties
                    if ((previewProperty.propType >= PropertyType.Texture2D) && (previewProperty.propType <= PropertyType.Cubemap))
                    {

                        if (previewProperty.propType != PropertyType.Cubemap)
                        {
                            if (previewProperty.textureValue != null)
                                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(previewProperty.textureValue, out string guid, out long localID))
                                {
                                    // Note, this never gets cleared, so we accumulate texture GUIDs over time, if the user keeps changing textures
                                    m_PreviewTextureGUIDs.Add(guid);
                                }
                        }
                        else
                        {
                            if (previewProperty.cubemapValue != null)
                                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(previewProperty.cubemapValue, out string guid, out long localID))
                                {
                                    // Note, this never gets cleared, so we accumulate texture GUIDs over time, if the user keeps changing textures
                                    m_PreviewTextureGUIDs.Add(guid);
                                }
                        }

                    }
                    // virtual texture assignments must be pushed to the materials themselves (MaterialPropertyBlocks not supported)
                    //if ((previewProperty.propType == PropertyType.VirtualTexture) &&
                    //    (previewProperty.vtProperty?.value?.layers != null))
                    //{
                    //    perMaterialPreviewProperties.Add(previewProperty);
                    //}
                }
            }
        }

        void AssignPerMaterialPreviewProperties(Material mat, List<PreviewProperty> perMaterialPreviewProperties)
        {
            foreach (var prop in perMaterialPreviewProperties)
            {
                switch (prop.propType)
                {
                    case PropertyType.VirtualTexture:

                        // setup the VT textures on the material
//                        bool setAnyTextures = false;
//                        var vt = prop.vtProperty.value;
//                        for (int layer = 0; layer < vt.layers.Count; layer++)
//                        {
//                            var texture = vt.layers[layer].layerTexture?.texture;
//                            int propIndex = mat.shader.FindPropertyIndex(vt.layers[layer].layerRefName);
//                            if (propIndex != -1)
//                            {
//                                mat.SetTexture(vt.layers[layer].layerRefName, texture);
//                                setAnyTextures = true;
//                            }
//                        }
//                        // also put in a request for the VT tiles, since preview rendering does not have feedback enabled
//                        if (setAnyTextures)
//                        {
//#if ENABLE_VIRTUALTEXTURES
//                            int stackPropertyId = Shader.PropertyToID(prop.vtProperty.referenceName);
//                            try
//                            {
//                                // Ensure we always request the mip sized 256x256
//                                int width, height;
//                                UnityEngine.Rendering.VirtualTexturing.Streaming.GetTextureStackSize(mat, stackPropertyId, out width, out height);
//                                int textureMip = (int)Math.Max(Mathf.Log(width, 2f), Mathf.Log(height, 2f));
//                                const int baseMip = 8;
//                                int mip = Math.Max(textureMip - baseMip, 0);
//                                UnityEngine.Rendering.VirtualTexturing.Streaming.RequestRegion(mat, stackPropertyId, new Rect(0.0f, 0.0f, 1.0f, 1.0f), mip, UnityEngine.Rendering.VirtualTexturing.System.AllMips);
//                            }
//                            catch (InvalidOperationException)
//                            {
//                                // This gets thrown when the system is in an indeterminate state (like a material with no textures assigned which can obviously never have a texture stack streamed).
//                                // This is valid in this case as we're still authoring the material.
//                            }
//#endif // ENABLE_VIRTUALTEXTURES
//                        }
                        break;
                }
            }
        }

        bool TimedNodesShouldUpdate(EditorWindow editorWindow)
        {
            // get current screen FPS, clamp to what we consider a valid range
            // this is probably not accurate for multi-monitor.. but should be relevant to at least one of the monitors
            double monitorFPS = Screen.currentResolution.refreshRateRatio.value;
            if (Double.IsInfinity(monitorFPS) || Double.IsNaN(monitorFPS))
                monitorFPS = 60.0f;
            monitorFPS = Math.Min(monitorFPS, 144.0);
            monitorFPS = Math.Max(monitorFPS, 30.0);

            var curTime = EditorApplication.timeSinceStartup;
            var deltaTime = curTime - m_LastTimedUpdateTime;
            bool isFocusedWindow = (EditorWindow.focusedWindow == editorWindow);

            // we throttle the update rate, based on whether the window is focused and if unity is active
            const double k_AnimatedFPS_WhenNotFocused = 10.0;
            const double k_AnimatedFPS_WhenInactive = 2.0;
            double maxAnimatedFPS =
                (UnityEditorInternal.InternalEditorUtility.isApplicationActive ?
                    (isFocusedWindow ? monitorFPS : k_AnimatedFPS_WhenNotFocused) :
                    k_AnimatedFPS_WhenInactive);

            bool update = (deltaTime > (1.0 / maxAnimatedFPS));
            if (update)
                m_LastTimedUpdateTime = curTime;
            return update;
        }

        private static readonly ProfilerMarker RenderPreviewsMarker = new ProfilerMarker("RenderPreviews");
        private static int k_spriteProps = Shader.PropertyToID("unity_SpriteProps");
        private static int k_spriteColor = Shader.PropertyToID("unity_SpriteColor");
        private static int k_rendererColor = Shader.PropertyToID("_RendererColor");
        public void RenderPreviews(EditorWindow editorWindow, bool requestShaders = true)
        {
            // TODO
        }

        private void ForEachNodesPreview(
           IEnumerable<AbstractGeometryNode> nodes,
           Action<PreviewRenderData> action)
        {
            foreach (var node in nodes)
            {
                var preview = GetPreviewRenderData(node);
                if (preview != null)    // some output nodes may have no preview
                    action(preview);
            }
        }

        void DestroyRenderData(PreviewRenderData renderData)
        {
            if (renderData.shaderData != null)
            {
                if (renderData.shaderData.mat != null)
                {
                    UnityEngine.Object.DestroyImmediate(renderData.shaderData.mat, true);
                }
                if (renderData.shaderData.shader != null)
                {
                    ShaderUtil.ClearShaderMessages(renderData.shaderData.shader);
                    UnityEngine.Object.DestroyImmediate(renderData.shaderData.shader, true);
                }
            }

            // Clear render textures
            if (renderData.renderTexture != null)
                UnityEngine.Object.DestroyImmediate(renderData.renderTexture, true);
            if (renderData.texture != null)
                UnityEngine.Object.DestroyImmediate(renderData.texture, true);

            // Clear callbacks
            renderData.onPreviewChanged = null;
            if (renderData.shaderData != null && renderData.shaderData.node != null)
                renderData.shaderData.node.UnregisterCallback(OnNodeModified);
        }

        void DestroyPreview(AbstractGeometryNode node)
        {
            if (node is BlockNode)
            {
                // block nodes don't have preview render data
                Assert.IsFalse(m_RenderDatas.ContainsKey(node));
                node.UnregisterCallback(OnNodeModified);
                UpdateMasterPreview(ModificationScope.Topological);
                return;
            }

            if (!m_RenderDatas.TryGetValue(node, out var renderData))
            {
                return;
            }

            m_PreviewsNeedsRecompile.Remove(renderData);
            m_PreviewsCompiling.Remove(renderData);
            m_PreviewsToDraw.Remove(renderData);
            m_TimedPreviews.Remove(renderData);

            DestroyRenderData(renderData);
            m_RenderDatas.Remove(node);
        }

        void ReleaseUnmanagedResources()
        {
            if (m_ErrorTexture != null)
            {
                UnityEngine.Object.DestroyImmediate(m_ErrorTexture);
                m_ErrorTexture = null;
            }
            if (m_SceneResources != null)
            {
                m_SceneResources.Dispose();
                m_SceneResources = null;
            }
            foreach (var renderData in m_RenderDatas.Values)
                DestroyRenderData(renderData);
            m_RenderDatas.Clear();
            m_SharedPreviewPropertyBlock.Clear();
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~PreviewManager()
        {
            throw new Exception("PreviewManager was not disposed of properly.");
        }
    }

    delegate void OnPreviewChanged();

    class PreviewGeometryData
    {
        public AbstractGeometryNode node;
        public Shader shader;
        public Material mat;
        public string shaderString;
        public int passesCompiling;
        public bool isOutOfDate;
        public bool hasError;
    }

    class PreviewRenderData
    {
        public string previewName;
        public PreviewGeometryData shaderData;
        public RenderTexture renderTexture;
        public Texture texture;
        public PreviewMode previewMode;
        public OnPreviewChanged onPreviewChanged;

        public void NotifyPreviewChanged()
        {
            if (onPreviewChanged != null)
                onPreviewChanged();
        }
    }
}
