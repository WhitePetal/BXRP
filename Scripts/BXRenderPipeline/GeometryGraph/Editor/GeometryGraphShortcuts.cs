using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    static class GeometryGraphShortcuts
    {
        static GeometryGraphEditWindow GetFocusedGeometryGraphEditorWindow()
        {
            return EditorWindow.focusedWindow as GeometryGraphEditWindow;
        }

        static GraphEditorView GetGraphEditorView()
        {
            return GetFocusedGeometryGraphEditorWindow().graphEditorView;
        }

        static GeometryGraphView GetGraphView()
        {
            return GetGraphEditorView().graphView;
        }

        static bool GetMousePositionIsInGraphView(out Vector2 pos)
        {
            pos = default;
            var graphView = GetGraphView();
            var windowRoot = GetFocusedGeometryGraphEditorWindow().rootVisualElement;
            var windowMousePosition = windowRoot.ChangeCoordinatesTo(windowRoot.parent, graphView.cachedMousePosition);

            if (!graphView.worldBound.Contains(windowMousePosition))
                return false; // don't create nodes if they aren't on the graph view.

            pos = graphView.contentViewContainer.WorldToLocal(graphView.cachedMousePosition);
            return true;
        }

        static void CreateNode<T>() where T : AbstractGeometryNode
        {
            if (!GetMousePositionIsInGraphView(out var graphMousePosition))
                return;

            var positionRect = new Rect(graphMousePosition, Vector2.zero);

            var graphView = GetGraphView();
            var graph = graphView.graph;
            AbstractGeometryNode node = Activator.CreateInstance<T>();

            var drawState = node.drawState;
            drawState.position = positionRect;
            node.drawState = drawState;

            graph.owner.RegisterCompleteObjectUndo("Add " + node.name);
            graphView.graph.AddNode(node);
        }

        static HashSet<(KeyCode key, ShortcutModifiers modifier)> reservedShortcuts = new HashSet<(KeyCode key, ShortcutModifiers modifier)> {
                (KeyCode.A, ShortcutModifiers.None), // Frame All
                (KeyCode.F, ShortcutModifiers.None), // Frame Selection
                (KeyCode.Space, ShortcutModifiers.None), // Summon Searcher (for node creation)
                (KeyCode.C, ShortcutModifiers.Action), // Copy
                (KeyCode.X, ShortcutModifiers.Action), // cut
                (KeyCode.V, ShortcutModifiers.Action), // Paste
                (KeyCode.Z, ShortcutModifiers.Action), // Undo
                (KeyCode.Y, ShortcutModifiers.Action), // Redo
                (KeyCode.D, ShortcutModifiers.Action), // Duplicate
            };

        static void CheckBindings(string name)
        {
            if (!ShortcutManager.instance.IsShortcutOverridden(name))
                return;

            var customBinding = ShortcutManager.instance.GetShortcutBinding(name);

            foreach (var keyCombo in customBinding.keyCombinationSequence)
            {
                if (reservedShortcuts.Contains((keyCombo.keyCode, keyCombo.modifiers)))
                {
                    throw new Exception($"The binding for {name} ({keyCombo}) conflicts with a built-in shortcut. Please go to Edit->Shortcuts... and change the binding.");
                }
            }
        }

        internal static string GetKeycodeForContextMenu(string id)
        {
            const string kKeycodePrefixAlt = "&";
            const string kKeycodePrefixShift = "#";
            const string kKeycodePrefixAction = "%";
            const string kKeycodePrefixControl = "^";
            const string kKeycodePrefixNoModifier = "_";

            var binding = ShortcutManager.instance.GetShortcutBinding(id);
            foreach (var keyCombo in binding.keyCombinationSequence)
            {
                var sb = new StringBuilder();

                if (keyCombo.alt) sb.Append(kKeycodePrefixAlt);
                if (keyCombo.shift) sb.Append(kKeycodePrefixShift);
                if (keyCombo.action) sb.Append(kKeycodePrefixAction);
                if (keyCombo.control) sb.Append(kKeycodePrefixControl);
                if (keyCombo.modifiers == ShortcutModifiers.None) sb.Append(kKeycodePrefixNoModifier);

                sb.Append(keyCombo.keyCode);
                return sb.ToString();
            }

            return "";
        }

        [Shortcut("GeometryGraph/File: Save", typeof(GeometryGraphEditWindow), KeyCode.S, ShortcutModifiers.Action)]
        static void Save(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            GetFocusedGeometryGraphEditorWindow().SaveAsset();
        }

        [Shortcut("GeometryGraph/File: Save As...", typeof(GeometryGraphEditWindow), KeyCode.S, ShortcutModifiers.Action | ShortcutModifiers.Shift)]
        static void SaveAs(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            GetFocusedGeometryGraphEditorWindow().SaveAs();
        }

        [Shortcut("GeometryGraph/File: Close Tab", typeof(GeometryGraphEditWindow), KeyCode.F4, ShortcutModifiers.Action)]
        static void CloseTab(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            var editorWindow = GetFocusedGeometryGraphEditorWindow();
            if (editorWindow.PromptSaveIfDirtyOnQuit())
                editorWindow.Close();
        }

        [Shortcut("GeometryGraph/Toolbar: Toggle Blackboard", typeof(GeometryGraphEditWindow), KeyCode.Alpha1, ShortcutModifiers.Shift)]
        static void ToggleBlackboard(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            var graphEditor = GetGraphEditorView();
            graphEditor.viewSettings.isBlackboardVisible = !graphEditor.viewSettings.isBlackboardVisible;
            graphEditor.UserViewSettingsChangeCheck(graphEditor.colorManager.activeIndex);
        }

        [Shortcut("GeometryGraph/Toolbar: Toggle Inspector", typeof(GeometryGraphEditWindow), KeyCode.Alpha2, ShortcutModifiers.Shift)]
        static void ToggleInspector(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            var graphEditor = GetGraphEditorView();
            graphEditor.viewSettings.isInspectorVisible = !graphEditor.viewSettings.isInspectorVisible;
            graphEditor.UserViewSettingsChangeCheck(graphEditor.colorManager.activeIndex);
        }

        [Shortcut("GeometryGraph/Toolbar: Toggle Main Preview", typeof(GeometryGraphEditWindow), KeyCode.Alpha3, ShortcutModifiers.Shift)]
        static void ToggleMainPreview(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            var graphEditor = GetGraphEditorView();
            graphEditor.viewSettings.isPreviewVisible = !graphEditor.viewSettings.isPreviewVisible;
            graphEditor.UserViewSettingsChangeCheck(graphEditor.colorManager.activeIndex);
        }

        [Shortcut("GeometryGraph/Toolbar: Cycle Color Mode", typeof(GeometryGraphEditWindow), KeyCode.Alpha4, ShortcutModifiers.Shift)]
        static void CycleColorMode(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            var graphEditor = GetGraphEditorView();

            var nextIndex = graphEditor.colorManager.activeIndex + 1;
            if (nextIndex >= graphEditor.colorManager.providersCount)
                nextIndex = 0;

            graphEditor.UserViewSettingsChangeCheck(nextIndex);
        }

        internal const string summonDocumentationShortcutID = "GeometryGraph/Selection: Summon Documentation";
        [Shortcut(summonDocumentationShortcutID, typeof(GeometryGraphEditWindow), KeyCode.F1)]
        static void Documentation(ShortcutArguments args)
        {
            CheckBindings(summonDocumentationShortcutID);
            foreach (var selected in GetGraphView().selection)
                if (selected is IGeometryNodeView nodeView && nodeView.node.documentationURL != null)
                {
                    System.Diagnostics.Process.Start(nodeView.node.documentationURL);
                    break;
                }
        }

        internal const string nodeGroupShortcutID = "GeometryGraph/Selection: Node Group";
        [Shortcut(nodeGroupShortcutID, typeof(GeometryGraphEditWindow), KeyCode.G, ShortcutModifiers.Action)]
        static void Group(ShortcutArguments args)
        {
            CheckBindings(nodeGroupShortcutID);
            var graphView = GetGraphView();
            foreach (var selected in graphView.selection)
                if ((selected is IGeometryNodeView nodeView && nodeView.node is AbstractGeometryNode)
                    || selected.GetType() == typeof(StickyNote))
                {
                    graphView.GroupSelection();
                    break;
                }
        }

        internal const string nodeUnGroupShortcutID = "GeometryGraph/Selection: Node Ungroup";
        [Shortcut(nodeUnGroupShortcutID, typeof(GeometryGraphEditWindow), KeyCode.U, ShortcutModifiers.Action)]
        static void UnGroup(ShortcutArguments args)
        {
            CheckBindings(nodeUnGroupShortcutID);
            var graphView = GetGraphView();
            foreach (var selected in graphView.selection)
                if ((selected is IGeometryNodeView nodeView && nodeView.node is AbstractGeometryNode)
                    || selected.GetType() == typeof(StickyNote))
                {
                    graphView.RemoveFromGroupNode();
                    break;
                }
        }

        internal const string nodeDeleteShortcutID = "GeometryGraph/Selection: Delete";
        [Shortcut(nodeGroupShortcutID, typeof(GeometryGraphEditWindow), KeyCode.X, ShortcutModifiers.None)]
        static void Delete(ShortcutArguments args)
        {
            CheckBindings(nodeGroupShortcutID);
            var graphView = GetGraphView();
            foreach (var selected in graphView.selection)
                if ((selected is IGeometryNodeView nodeView && nodeView.node is AbstractGeometryNode)
                    || selected.GetType() == typeof(StickyNote) || selected is UnityEditor.Experimental.GraphView.Edge)
                {
                    graphView.DeleteSelection();
                    break;
                }
        }

        internal const string nodePreviewShortcutID = "GeometryGraph/Selection: Toggle Node Previews";
        [Shortcut(nodePreviewShortcutID, typeof(GeometryGraphEditWindow), KeyCode.T, ShortcutModifiers.Action)]
        static void ToggleNodePreviews(ShortcutArguments args)
        {
            CheckBindings(nodePreviewShortcutID);
            bool shouldHide = false;
            // Toggle all node previews if none are selected. Otherwise, update only the selected node previews.
            var selection = GetGraphView().selection;
            if (selection.Count == 0)
            {
                var graph = GetGraphView().graph;
                var nodes = graph.GetNodes<AbstractGeometryNode>();
                foreach (AbstractGeometryNode node in nodes)
                    if (node.previewExpanded && node.hasPreview)
                    {
                        shouldHide = true;
                        break;
                    }

                graph.owner.RegisterCompleteObjectUndo("Toggle Previews");
                foreach (AbstractGeometryNode node in nodes)
                    node.previewExpanded = !shouldHide;
            }
            else
            {
                foreach (var selected in selection)
                    if (selected is IGeometryNodeView nodeView)
                    {
                        if (nodeView.node.previewExpanded && nodeView.node.hasPreview)
                        {
                            shouldHide = true;
                            break;
                        }
                    }
                GetGraphView().SetPreviewExpandedForSelectedNodes(!shouldHide);
            }
        }

        internal const string nodeCollapsedShortcutID = "GeometryGraph/Selection: Toggle Node Collapsed";
        [Shortcut(nodeCollapsedShortcutID, typeof(GeometryGraphEditWindow), KeyCode.P, ShortcutModifiers.Action)]
        static void ToggleNodeCollapsed(ShortcutArguments args)
        {
            CheckBindings(nodeCollapsedShortcutID);
            bool shouldCollapse = false;
            foreach (var selected in GetGraphView().selection)
                if (selected is GeometryNodeView nodeView)
                {
                    if (nodeView.expanded && nodeView.CanToggleNodeExpanded())
                    {
                        shouldCollapse = true;
                        break;
                    }
                }
            GetGraphView().SetNodeExpandedForSelectedNodes(!shouldCollapse);
        }

        internal const string createRedirectNodeShortcutID = "GeometryGraph/Selection: Insert Redirect";
        [Shortcut(createRedirectNodeShortcutID, typeof(GeometryGraphEditWindow), KeyCode.R, ShortcutModifiers.Action)]
        static void InsertRedirect(ShortcutArguments args)
        {
            CheckBindings(createRedirectNodeShortcutID);

            if (!GetMousePositionIsInGraphView(out var graphMousePosition))
                return;

            foreach (var selected in GetGraphView().selection)
            {
                if (selected is UnityEditor.Experimental.GraphView.Edge edge)
                {
                    int weight = 1;
                    var pos = graphMousePosition * weight;
                    int count = weight;
                    foreach (var cp in edge.edgeControl.controlPoints)
                    {
                        pos += cp;
                        count++;
                    }
                    pos /= count;
                    pos = GetGraphView().contentViewContainer.LocalToWorld(pos);
                    GetGraphView().CreateRedirectNode(pos, edge);
                }
            }
        }

        // TODO: LerpNode
        //[Shortcut("GeometryGraph/Add Node: Lerp", typeof(GeometryGraphEditWindow), KeyCode.L, ShortcutModifiers.Alt)]
        //static void CreateLerp(ShortcutArguments args)
        //{
        //    CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
        //    CreateNode<LerpNode>();
        //}

        // TODO: MultiplyNode
        //[Shortcut("GeometryGraph/Add Node: Multiply", typeof(GeometryGraphEditWindow), KeyCode.M, ShortcutModifiers.Alt)]
        //static void CreateMultiply(ShortcutArguments args)
        //{
        //    CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
        //    CreateNode<MultiplyNode>();
        //}

        // TODO: AddNode
        //[Shortcut("GeometryGraph/Add Node: Add", typeof(GeometryGraphEditWindow), KeyCode.A, ShortcutModifiers.Alt)]
        //static void CreateAdd(ShortcutArguments args)
        //{
        //    CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
        //    CreateNode<AddNode>();
        //}

        // TODO: SampleTexture2DNode
        //[Shortcut("GeometryGraph/Add Node: Sample Texture 2D", typeof(GeometryGraphEditWindow), KeyCode.X, ShortcutModifiers.Alt)]
        //static void CreateSampleTexture2D(ShortcutArguments args)
        //{
        //    CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
        //    CreateNode<SampleTexture2DNode>();
        //}

        [Shortcut("GeometryGraph/Add Node: Float", typeof(GeometryGraphEditWindow), KeyCode.Alpha1, ShortcutModifiers.Alt)]
        static void CreateFloat(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<Vector1Node>();
        }

        [Shortcut("GeometryGraph/Add Node: Vector2", typeof(GeometryGraphEditWindow), KeyCode.Alpha2, ShortcutModifiers.Alt)]
        static void CreateVec2(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<Vector2Node>();
        }

        [Shortcut("GeometryGraph/Add Node: Vector3", typeof(GeometryGraphEditWindow), KeyCode.Alpha3, ShortcutModifiers.Alt)]
        static void CreateVec3(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<Vector3Node>();
        }

        [Shortcut("GeometryGraph/Add Node: Vector4", typeof(GeometryGraphEditWindow), KeyCode.Alpha4, ShortcutModifiers.Alt)]
        static void CreateVec4(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<Vector4Node>();
        }

        // TODO: SplitNode
        //[Shortcut("GeometryGraph/Add Node: Split", typeof(GeometryGraphEditWindow), KeyCode.E, ShortcutModifiers.Alt)]
        //static void CreateSplit(ShortcutArguments args)
        //{
        //    CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
        //    CreateNode<SplitNode>();
        //}

        // TODO: TillingAndOffsetNode
        //[Shortcut("GeometryGraph/Add Node: Tiling and Offset", typeof(GeometryGraphEditWindow), KeyCode.O, ShortcutModifiers.Alt)]
        //static void CreateTilingAndOffset(ShortcutArguments args)
        //{
        //    CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
        //    CreateNode<TilingAndOffsetNode>();
        //}

        // TODO: TimeNode
        //[Shortcut("GeometryGraph/Add Node: Time", typeof(GeometryGraphEditWindow), KeyCode.T, ShortcutModifiers.Alt)]
        //static void CreateTime(ShortcutArguments args)
        //{
        //    CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
        //    CreateNode<TimeNode>();
        //}

        // TODO: PositionNode
        //[Shortcut("GeometryGraph/Add Node: Position", typeof(GeometryGraphEditWindow), KeyCode.V, ShortcutModifiers.Alt)]
        //static void CreatePosition(ShortcutArguments args)
        //{
        //    CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
        //    CreateNode<PositionNode>();
        //}

        // TODO: SubtractNode
        //[Shortcut("GeometryGraph/Add Node: Subtract", typeof(GeometryGraphEditWindow), KeyCode.S, ShortcutModifiers.Alt)]
        //static void CreateSubtract(ShortcutArguments args)
        //{
        //    CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
        //    CreateNode<SubtractNode>();
        //}

        // TODO: UVNode
        //[Shortcut("GeometryGraph/Add Node: UV", typeof(GeometryGraphEditWindow), KeyCode.U, ShortcutModifiers.Alt)]
        //static void CreateUV(ShortcutArguments args)
        //{
        //    CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
        //    CreateNode<UVNode>();
        //}

        // TODO: OneMinusNode
        //[Shortcut("GeometryGraph/Add Node: One Minus", typeof(GeometryGraphEditWindow), KeyCode.I, ShortcutModifiers.Alt)]
        //static void CreateOneMinus(ShortcutArguments args)
        //{
        //    CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
        //    CreateNode<OneMinusNode>();
        //}

        // TODO: BranchNode
        //[Shortcut("GeometryGraph/Add Node: Branch", typeof(GeometryGraphEditWindow), KeyCode.Y, ShortcutModifiers.Alt)]
        //static void CreateBranch(ShortcutArguments args)
        //{
        //    CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
        //    CreateNode<BranchNode>();
        //}

        // TODO: DivideNode
        //[Shortcut("GeometryGraph/Add Node: Divide", typeof(GeometryGraphEditWindow), KeyCode.D, ShortcutModifiers.Alt)]
        //static void CreateDivide(ShortcutArguments args)
        //{
        //    CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
        //    CreateNode<DivideNode>();
        //}

        // TODO: CombineNode
        //[Shortcut("GeometryGraph/Add Node: Combine", typeof(GeometryGraphEditWindow), KeyCode.K, ShortcutModifiers.Alt)]
        //static void CreateCombine(ShortcutArguments args)
        //{
        //    CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
        //    CreateNode<CombineNode>();
        //}

        // TODO: PowerNode
        //[Shortcut("GeometryGraph/Add Node: Power", typeof(GeometryGraphEditWindow), KeyCode.P, ShortcutModifiers.Alt)]
        //static void CreatePower(ShortcutArguments args)
        //{
        //    CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
        //    CreateNode<PowerNode>();
        //}

        // TODO: SaturateNode
        //[Shortcut("GeometryGraph/Add Node: Saturate", typeof(GeometryGraphEditWindow), KeyCode.Q, ShortcutModifiers.Alt)]
        //static void CreateSaturate(ShortcutArguments args)
        //{
        //    CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
        //    CreateNode<SaturateNode>();
        //}

        // TODO: RemapNode
        //[Shortcut("GeometryGraph/Add Node: Remap", typeof(GeometryGraphEditWindow), KeyCode.R, ShortcutModifiers.Alt)]
        //static void CreateRemap(ShortcutArguments args)
        //{
        //    CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
        //    CreateNode<RemapNode>();
        //}

        // TODO: NormalVectorNode
        //[Shortcut("GeometryGraph/Add Node: Normal Vector", typeof(GeometryGraphEditWindow), KeyCode.N, ShortcutModifiers.Alt)]
        //static void CreateNormalVector(ShortcutArguments args)
        //{
        //    CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
        //    CreateNode<NormalVectorNode>();
        //}

        // TODO: ColorNode
        //[Shortcut("GeometryGraph/Add Node: Color", typeof(GeometryGraphEditWindow), KeyCode.C, ShortcutModifiers.Alt)]
        //static void CreateColor(ShortcutArguments args)
        //{
        //    CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
        //    CreateNode<ColorNode>();
        //}

        // TODO: BlendNode
        //[Shortcut("GeometryGraph/Add Node: Blend", typeof(GeometryGraphEditWindow), KeyCode.B, ShortcutModifiers.Alt)]
        //static void CreateBlend(ShortcutArguments args)
        //{
        //    CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
        //    CreateNode<BlendNode>();
        //}

        // TODO: StepNode
        //[Shortcut("GeometryGraph/Add Node: Step", typeof(GeometryGraphEditWindow), KeyCode.J, ShortcutModifiers.Alt)]
        //static void CreateStep(ShortcutArguments args)
        //{
        //    CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
        //    CreateNode<StepNode>();
        //}

        // TODO: ClampNode
        //[Shortcut("GeometryGraph/Add Node: Clamp", typeof(GeometryGraphEditWindow), KeyCode.Equals, ShortcutModifiers.Alt)]
        //static void CreateClamp(ShortcutArguments args)
        //{
        //    CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
        //    CreateNode<ClampNode>();
        //}

        // TODO: SmoothstepNode
        //[Shortcut("GeometryGraph/Add Node: Smoothstep", typeof(GeometryGraphEditWindow), KeyCode.BackQuote, ShortcutModifiers.Alt)]
        //static void CreateSmoothstep(ShortcutArguments args)
        //{
        //    CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
        //    CreateNode<SmoothstepNode>();
        //}

        // TODO: FresnelNode
        //[Shortcut("GeometryGraph/Add Node: Fresnel", typeof(GeometryGraphEditWindow), KeyCode.F, ShortcutModifiers.Alt)]
        //static void CreateFresnel(ShortcutArguments args)
        //{
        //    CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
        //    CreateNode<FresnelNode>();
        //}

        // TODO: CustomFunctionNode
        //[Shortcut("GeometryGraph/Add Node: Custom Function", typeof(GeometryGraphEditWindow), KeyCode.Semicolon, ShortcutModifiers.Alt)]
        //static void CreateCFN(ShortcutArguments args)
        //{
        //    CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
        //    CreateNode<CustomFunctionNode>();
        //}

        // TODO: DotProductNode
        //[Shortcut("GeometryGraph/Add Node: Dot Product", typeof(GeometryGraphEditWindow), KeyCode.Period, ShortcutModifiers.Alt)]
        //static void CreateDotProduct(ShortcutArguments args)
        //{
        //    CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
        //    CreateNode<DotProductNode>();
        //}

        // TODO: NormalizedNode
        //[Shortcut("GeometryGraph/Add Node: Normalize", typeof(GeometryGraphEditWindow), KeyCode.Z, ShortcutModifiers.Alt)]
        //static void CreateNormalize(ShortcutArguments args)
        //{
        //    CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
        //    CreateNode<NormalizeNode>();
        //}

        // TODO: AbsolutedNode
        //[Shortcut("GeometryGraph/Add Node: Absolute", typeof(GeometryGraphEditWindow), KeyCode.Backslash, ShortcutModifiers.Alt)]
        //static void CreateAbsolute(ShortcutArguments args)
        //{
        //    CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
        //    CreateNode<AbsoluteNode>();
        //}

        // TODO: NegateNode
        //[Shortcut("GeometryGraph/Add Node: Negate", typeof(GeometryGraphEditWindow), KeyCode.Minus, ShortcutModifiers.Alt)]
        //static void CreateNegate(ShortcutArguments args)
        //{
        //    CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
        //    CreateNode<NegateNode>();
        //}

        // TODO: FractionNOde
        //[Shortcut("GeometryGraph/Add Node: Fraction", typeof(GeometryGraphEditWindow), KeyCode.Slash, ShortcutModifiers.Alt)]
        //static void CreateFraction(ShortcutArguments args)
        //{
        //    CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
        //    CreateNode<FractionNode>();
        //}

        // TODO: SwizzleNode
        //[Shortcut("GeometryGraph/Add Node: Swizzle", typeof(GeometryGraphEditWindow), KeyCode.W, ShortcutModifiers.Alt)]
        //static void CreateSwizzle(ShortcutArguments args)
        //{
        //    CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
        //    CreateNode<SwizzleNode>();
        //}

        // TODO: GradientNode
        //[Shortcut("GeometryGraph/Add Node: Gradient", typeof(GeometryGraphEditWindow), KeyCode.G, ShortcutModifiers.Alt)]
        //static void CreateGradient(ShortcutArguments args)
        //{
        //    CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
        //    CreateNode<GradientNode>();
        //}

        // TODO: CrossProductNode
        //[Shortcut("GeometryGraph/Add Node: Cross Product", typeof(GeometryGraphEditWindow), KeyCode.H, ShortcutModifiers.Alt)]
        //static void CreateCrossProduct(ShortcutArguments args)
        //{
        //    CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
        //    CreateNode<CrossProductNode>();
        //}

        // TODO: BooleanNode
        //[Shortcut("GeometryGraph/Add Node: Boolean", typeof(GeometryGraphEditWindow), KeyCode.Alpha0, ShortcutModifiers.Alt)]
        //static void CreateBoolean(ShortcutArguments args)
        //{
        //    CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
        //    CreateNode<BooleanNode>();
        //}

        // TODO: FloorNode
        //[Shortcut("GeometryGraph/Add Node: Floor", typeof(GeometryGraphEditWindow), KeyCode.LeftBracket, ShortcutModifiers.Alt)]
        //static void CreateFloor(ShortcutArguments args)
        //{
        //    CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
        //    CreateNode<FloorNode>();
        //}

        // TODO: CeilingNode
        //[Shortcut("GeometryGraph/Add Node: Ceiling", typeof(GeometryGraphEditWindow), KeyCode.RightBracket, ShortcutModifiers.Alt)]
        //static void CreateCeiling(ShortcutArguments args)
        //{
        //    CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
        //    CreateNode<CeilingNode>();
        //}
    }
}
