using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    class ChangeExposedFlagAction : IGraphDataAction
    {
        internal ChangeExposedFlagAction(GeometryInput shaderInput, bool newIsExposed)
        {
            this.shaderInputReference = shaderInput;
            this.newIsExposedValue = newIsExposed;
            this.oldIsExposedValue = shaderInput.generatePropertyBlock;
        }

        void ChangeExposedFlag(GraphData graphData)
        {
            Assert.IsNotNull(graphData, "GraphData is null while carrying out ChangeExposedFlagAction");
            Assert.IsNotNull(shaderInputReference, "GeometryInputReference is null while carrying out ChangeExposedFlagAction");
            // The Undos are currently handled in ShaderInputPropertyDrawer but we want to move that out from there and handle here
            //graphData.owner.RegisterCompleteObjectUndo("Change Exposed Toggle");
            shaderInputReference.generatePropertyBlock = newIsExposedValue;
        }

        public Action<GraphData> modifyGraphDataAction => ChangeExposedFlag;

        // Reference to the shader input being modified
        internal GeometryInput shaderInputReference { get; private set; }

        // New value of whether the shader input should be exposed to the material inspector
        internal bool newIsExposedValue { get; private set; }
        internal bool oldIsExposedValue { get; private set; }
    }

    class ChangePropertyValueAction : IGraphDataAction
    {
        void ChangePropertyValue(GraphData graphData)
        {
            Assert.IsNotNull(graphData, "GraphData is null while carrying out ChangePropertyValueAction");
            Assert.IsNotNull(geometryInputReference, "ShaderPropertyReference is null while carrying out ChangePropertyValueAction");
            // The Undos are currently handled in ShaderInputPropertyDrawer but we want to move that out from there and handle here
            //graphData.owner.RegisterCompleteObjectUndo("Change Property Value");
            //var material = graphData.owner.materialArtifact;
            switch (geometryInputReference)
            {
                //case BooleanShaderProperty booleanProperty:
                    //booleanProperty.value = ((ToggleData)newShaderInputValue).isOn;
                    //if (material) material.SetFloat(shaderInputReference.referenceName, booleanProperty.value ? 1.0f : 0.0f);
                    //break;
                case Vector1GeometryProperty vector1Property:
                    vector1Property.value = (float)newGeometryInputValue;
                    //if (material) material.SetFloat(shaderInputReference.referenceName, vector1Property.value);
                    break;
                case Vector2GeometryProperty vector2Property:
                    vector2Property.value = (Vector2)newGeometryInputValue;
                    //if (material) material.SetVector(shaderInputReference.referenceName, vector2Property.value);
                    break;
                case Vector3GeometryProperty vector3Property:
                    vector3Property.value = (Vector3)newGeometryInputValue;
                    //if (material) material.SetVector(shaderInputReference.referenceName, vector3Property.value);
                    break;
                case Vector4GeometryProperty vector4Property:
                    vector4Property.value = (Vector4)newGeometryInputValue;
                    //if (material) material.SetVector(shaderInputReference.referenceName, vector4Property.value);
                    break;
                //case ColorShaderProperty colorProperty:
                //colorProperty.value = (Color)newGeometryInputValue;
                //if (material) material.SetColor(shaderInputReference.referenceName, colorProperty.value);
                //break;
                //case Texture2DShaderProperty texture2DProperty:
                //texture2DProperty.value.texture = (Texture)newGeometryInputValue;
                //if (material) material.SetTexture(shaderInputReference.referenceName, texture2DProperty.value.texture);
                //break;
                //case Texture2DArrayShaderProperty texture2DArrayProperty:
                //texture2DArrayProperty.value.textureArray = (Texture2DArray)newGeometryInputValue;
                //if (material) material.SetTexture(shaderInputReference.referenceName, texture2DArrayProperty.value.textureArray);
                //break;
                //case Texture3DShaderProperty texture3DProperty:
                //    texture3DProperty.value.texture = (Texture3D)newGeometryInputValue;
                //    if (material) material.SetTexture(shaderInputReference.referenceName, texture3DProperty.value.texture);
                //    break;
                //case CubemapShaderProperty cubemapProperty:
                //    cubemapProperty.value.cubemap = (Cubemap)newGeometryInputValue;
                //    if (material) material.SetTexture(shaderInputReference.referenceName, cubemapProperty.value.cubemap);
                //    break;
                //case Matrix2ShaderProperty matrix2Property:
                //    matrix2Property.value = (Matrix4x4)newGeometryInputValue;
                //    break;
                //case Matrix3ShaderProperty matrix3Property:
                //    matrix3Property.value = (Matrix4x4)newGeometryInputValue;
                //    break;
                //case Matrix4ShaderProperty matrix4Property:
                //    matrix4Property.value = (Matrix4x4)newGeometryInputValue;
                //    break;
                //case SamplerStateShaderProperty samplerStateProperty:
                //    samplerStateProperty.value = (TextureSamplerState)newGeometryInputValue;
                //    break;
                //case GradientShaderProperty gradientProperty:
                //    gradientProperty.value = (Gradient)newGeometryInputValue;
                //    break;
                //case GeometryGeometryProperty geometryProperty:
                    //geometryProperty.va
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public Action<GraphData> modifyGraphDataAction => ChangePropertyValue;

        // Reference to the shader input being modified
        internal GeometryInput geometryInputReference { get; set; }

        // New value of the shader property

        internal object newGeometryInputValue { get; set; }
    }

    class ChangeDisplayNameAction : IGraphDataAction
    {
        void ChangeDisplayName(GraphData graphData)
        {
            Assert.IsNotNull(graphData, "GraphData is null while carrying out ChangeDisplayNameAction");
            Assert.IsNotNull(geometryInputReference, "GeometryInputReference is null while carrying out ChangeDisplayNameAction");
            graphData.owner.RegisterCompleteObjectUndo("Change Display Name");
            if (newDisplayNameValue != geometryInputReference.displayName)
            {
                geometryInputReference.SetDisplayNameAndSanitizeForGraph(graphData, newDisplayNameValue);
            }
        }

        public Action<GraphData> modifyGraphDataAction => ChangeDisplayName;

        // Reference to the shader input being modified
        internal GeometryInput geometryInputReference { get; set; }

        internal string newDisplayNameValue { get; set; }
    }

    class ChangeReferenceNameAction : IGraphDataAction
    {
        void ChangeReferenceName(GraphData graphData)
        {
            Assert.IsNotNull(graphData, "GraphData is null while carrying out ChangeReferenceNameAction");
            Assert.IsNotNull(geometryInputReference, "GeometryInputReference is null while carrying out ChangeReferenceNameAction");
            // The Undos are currently handled in ShaderInputPropertyDrawer but we want to move that out from there and handle here
            //graphData.owner.RegisterCompleteObjectUndo("Change Reference Name");
            if (newReferenceNameValue != geometryInputReference.overrideReferenceName)
            {
                graphData.SanitizeGraphInputReferenceName(geometryInputReference, newReferenceNameValue);
            }
        }

        public Action<GraphData> modifyGraphDataAction => ChangeReferenceName;

        // Reference to the shader input being modified
        internal GeometryInput geometryInputReference { get; set; }

        internal string newReferenceNameValue { get; set; }
    }

    class ResetReferenceNameAction : IGraphDataAction
    {
        void ResetReferenceName(GraphData graphData)
        {
            Assert.IsNotNull(graphData, "GraphData is null while carrying out ResetReferenceNameAction");
            Assert.IsNotNull(geometryInputReference, "GeometryInputReference is null while carrying out ResetReferenceNameAction");
            graphData.owner.RegisterCompleteObjectUndo("Reset Reference Name");
            geometryInputReference.overrideReferenceName = null;
        }

        public Action<GraphData> modifyGraphDataAction => ResetReferenceName;

        // Reference to the shader input being modified
        internal GeometryInput geometryInputReference { get; set; }
    }

    class DeleteGeometryInputAction : IGraphDataAction
    {
        void DeleteShaderInput(GraphData graphData)
        {
            Assert.IsNotNull(graphData, "GraphData is null while carrying out DeleteShaderInputAction");
            Assert.IsNotNull(geometryInputsToDelete, "GeometryInputsToDelete is null while carrying out DeleteShaderInputAction");
            // This is called by MaterialGraphView currently, no need to repeat it here, though ideally it would live here
            //graphData.owner.RegisterCompleteObjectUndo("Delete Graph Input(s)");

            foreach (var geometryInput in geometryInputsToDelete)
            {
                graphData.RemoveGraphInput(geometryInput);
            }
        }

        public Action<GraphData> modifyGraphDataAction => DeleteShaderInput;

        // Reference to the shader input(s) being deleted
        internal IList<GeometryInput> geometryInputsToDelete { get; set; } = new List<GeometryInput>();
    }

    class GeometryInputViewController : GGViewController<GeometryInput, GeometryInputViewModel>
    {
        // Exposed for PropertyView
        internal GraphData graphData => DataStore.State;

        internal GeometryInputViewController(GeometryInput shaderInput, GeometryInputViewModel inViewModel, DataStore<GraphData> graphDataStore)
            : base(shaderInput, inViewModel, graphDataStore)
        {
            InitializeViewModel();

            m_ggBlackboardField = new GGBlackboardField(ViewModel);
            m_ggBlackboardField.controller = this;

            m_BlackboardRowView = new GGBlackboardRow(m_ggBlackboardField, null);
            m_BlackboardRowView.expanded = SessionState.GetBool($"Unity.ShaderGraph.Input.{shaderInput.objectId}.isExpanded", false);
        }

        void InitializeViewModel()
        {
            if (Model == null)
            {
                throw new AssertionException("Could not initialize shader input view model as shader input was null.", null);
            }
            ViewModel.model = Model;
            ViewModel.isSubGraph = DataStore.State.isSubGraph;
            ViewModel.isInputExposed = (DataStore.State.isSubGraph || Model.isExposed);
            ViewModel.inputName = Model.displayName;
            switch (Model)
            {
                case AbstractGeometryProperty geometryProperty:
                    ViewModel.inputTypeName = geometryProperty.GetPropertyTypeString();
                    // Handles upgrade fix for deprecated old Color property
                    geometryProperty.onBeforeVersionChange += (_) => graphData.owner.RegisterCompleteObjectUndo($"Change {geometryProperty.displayName} Version");
                    break;
                //case ShaderKeyword shaderKeyword:
                    //ViewModel.inputTypeName = shaderKeyword.keywordType + " Keyword";
                    //ViewModel.inputTypeName = shaderKeyword.isBuiltIn ? "Built-in " + ViewModel.inputTypeName : ViewModel.inputTypeName;
                    //break;
                case GeometryDropdown geometryDropdown:
                    ViewModel.inputTypeName = "Dropdown";
                    break;
            }

            ViewModel.requestModelChangeAction = this.RequestModelChange;
        }

        GGBlackboardRow m_BlackboardRowView;
        GGBlackboardField m_ggBlackboardField;

        internal GGBlackboardRow BlackboardItemView => m_BlackboardRowView;

        protected override void RequestModelChange(IGraphDataAction changeAction)
        {
            DataStore.Dispatch(changeAction);
        }

        // Called by GraphDataStore.Subscribe after the model has been changed
        protected override void ModelChanged(GraphData graphData, IGraphDataAction changeAction)
        {
            switch (changeAction)
            {
                case ChangeExposedFlagAction changeExposedFlagAction:
                    // ModelChanged is called overzealously on everything
                    // but we only care if the action pertains to our Model
                    if (changeExposedFlagAction.shaderInputReference == Model)
                    {
                        ViewModel.isInputExposed = Model.generatePropertyBlock;
                        if (changeExposedFlagAction.oldIsExposedValue != changeExposedFlagAction.newIsExposedValue)
                            DirtyNodes(ModificationScope.Graph);
                        m_ggBlackboardField.UpdateFromViewModel();
                    }
                    break;

                case ChangePropertyValueAction changePropertyValueAction:
                    if (changePropertyValueAction.geometryInputReference == Model)
                    {
                        DirtyNodes(ModificationScope.Graph);
                        m_ggBlackboardField.MarkDirtyRepaint();
                    }
                    break;

                case ResetReferenceNameAction resetReferenceNameAction:
                    if (resetReferenceNameAction.geometryInputReference == Model)
                    {
                        DirtyNodes(ModificationScope.Graph);
                    }
                    break;

                case ChangeReferenceNameAction changeReferenceNameAction:
                    if (changeReferenceNameAction.geometryInputReference == Model)
                    {
                        DirtyNodes(ModificationScope.Graph);
                    }
                    break;

                case ChangeDisplayNameAction changeDisplayNameAction:
                    if (changeDisplayNameAction.geometryInputReference == Model)
                    {
                        ViewModel.inputName = Model.displayName;
                        DirtyNodes(ModificationScope.Layout);
                        m_ggBlackboardField.UpdateFromViewModel();
                    }
                    break;
            }
        }

        // TODO: This should communicate to node controllers instead of searching for the nodes themselves everytime, but that's going to take a while...
        internal void DirtyNodes(ModificationScope modificationScope = ModificationScope.Node)
        {
            foreach (var observer in Model.InputObservers)
            {
                observer.OnGeometryInputUpdated(modificationScope);
            }

            switch (Model)
            {
                case AbstractGeometryProperty property:
                    var graphEditorView = m_BlackboardRowView.GetFirstAncestorOfType<GraphEditorView>();
                    if (graphEditorView == null)
                        return;
                    var colorManager = graphEditorView.colorManager;
                    var nodes = graphEditorView.graphView.Query<GeometryNodeView>().ToList();

                    colorManager.SetNodesDirty(nodes);
                    colorManager.UpdateNodeViews(nodes);
                    break;
                case ShaderKeyword keyword:
                    // Cant determine if Sub Graphs contain the keyword so just update them
                    foreach (var node in DataStore.State.GetNodes<SubGraphNode>())
                    {
                        node.Dirty(modificationScope);
                    }

                    break;
                case GeometryDropdown dropdown:
                    // Cant determine if Sub Graphs contain the dropdown so just update them
                    foreach (var node in DataStore.State.GetNodes<SubGraphNode>())
                    {
                        node.Dirty(modificationScope);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override void Dispose()
        {
            if (m_ggBlackboardField == null)
                return;

            Model?.ClearObservers();

            base.Dispose();
            Cleanup();

            BlackboardItemView.Dispose();
            m_ggBlackboardField.Dispose();

            m_BlackboardRowView = null;
            m_ggBlackboardField = null;
        }
    }
}
