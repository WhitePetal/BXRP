using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    class MoveGeometryInputAction : IGraphDataAction
    {
        void MoveShaderInput(GraphData graphData)
        {
            Assert.IsNotNull(graphData, "GraphData is null while carrying out MoveGeometryInputAction");
            Assert.IsNotNull(geometryInputReference, "GeometryInputReference is null while carrying out MoveGeometryInputAction");
            graphData.owner.RegisterCompleteObjectUndo("Move Graph Input");
            graphData.MoveItemInCategory(geometryInputReference, newIndexValue, associatedCategoryGuid);
        }

        public Action<GraphData> modifyGraphDataAction => MoveShaderInput;

        internal string associatedCategoryGuid { get; set; }

        // Reference to the shader input being modified
        internal GeometryInput geometryInputReference { get; set; }

        internal int newIndexValue { get; set; }
    }

    class DeleteCategoryAction : IGraphDataAction
    {
        void RemoveCategory(GraphData graphData)
        {
            Assert.IsNotNull(graphData, "GraphData is null while carrying out DeleteCategoryAction");
            Assert.IsNotNull(categoriesToRemoveGuids, "CategoryToRemove is null while carrying out DeleteCategoryAction");

            // This is called by MaterialGraphView currently, no need to repeat it here, though ideally it would live here
            //graphData.owner.RegisterCompleteObjectUndo("Delete Category");

            foreach (var categoryGUID in categoriesToRemoveGuids)
            {
                graphData.RemoveCategory(categoryGUID);
            }
        }

        public Action<GraphData> modifyGraphDataAction => RemoveCategory;

        // Reference to the guid(s) of categories being deleted
        public HashSet<string> categoriesToRemoveGuids { get; set; } = new HashSet<string>();
    }

    class ChangeCategoryIsExpandedAction : IGraphDataAction
    {
        internal const string kEditorPrefKey = ".isCategoryExpanded";

        void ChangeIsExpanded(GraphData graphData)
        {
            Assert.IsNotNull(graphData, "GraphData is null while carrying out ChangeIsExpanded on Category");
            foreach (var catid in categoryGuids)
            {
                var key = $"{editorPrefsBaseKey}.{catid}.{kEditorPrefKey}";
                var currentValue = EditorPrefs.GetBool(key, true);

                if (currentValue != isExpanded)
                {
                    EditorPrefs.SetBool(key, isExpanded);
                }
            }
        }

        public string editorPrefsBaseKey;
        public List<string> categoryGuids { get; set; }
        public bool isExpanded { get; set; }

        public Action<GraphData> modifyGraphDataAction => ChangeIsExpanded;
    }

    class ChangeCategoryNameAction : IGraphDataAction
    {
        void ChangeCategoryName(GraphData graphData)
        {
            Assert.IsNotNull(graphData, "GraphData is null while carrying out ChangeCategoryNameAction");
            graphData.owner.RegisterCompleteObjectUndo("Change Category Name");
            graphData.ChangeCategoryName(categoryGuid, newCategoryNameValue);
        }

        public Action<GraphData> modifyGraphDataAction => ChangeCategoryName;

        // Guid of the category being modified
        public string categoryGuid { get; set; }

        internal string newCategoryNameValue { get; set; }
    }

    class BlackboardCategoryController : GGViewController<CategoryData, BlackboardCategoryViewModel>, IDisposable
    {
        internal GGBlackboardCategory blackboardCategoryView => m_BlackboardCategoryView;
        GGBlackboardCategory m_BlackboardCategoryView;
        Dictionary<string, GeometryInputViewController> m_BlackboardItemControllers = new Dictionary<string, GeometryInputViewController>();
        GGBlackboard blackboard { get; set; }

        Action m_UnregisterAll;

        internal BlackboardCategoryController(CategoryData categoryData, BlackboardCategoryViewModel categoryViewModel, DataStore<GraphData> dataStore)
            : base(categoryData, categoryViewModel, dataStore)
        {
            m_BlackboardCategoryView = new GGBlackboardCategory(categoryViewModel, this);
            blackboard = categoryViewModel.parentView as GGBlackboard;
            if (blackboard == null)
                return;

            blackboard.Add(m_BlackboardCategoryView);

            var dragActionCancelCallback = new EventCallback<MouseUpEvent>((evt) =>
            {
                m_BlackboardCategoryView.OnDragActionCanceled();
            });

            m_UnregisterAll += () => blackboard.UnregisterCallback(dragActionCancelCallback);

            // These make sure that the drag indicators are disabled whenever a drag action is cancelled without completing a drop
            blackboard.RegisterCallback(dragActionCancelCallback);
            blackboard.hideDragIndicatorAction += m_BlackboardCategoryView.OnDragActionCanceled;

            foreach (var categoryItem in categoryData.Children)
            {
                if (categoryItem == null)
                {
                    throw new AssertionException("Failed to insert blackboard row into category due to shader input being null.", null);
                    //continue;
                }
                InsertBlackboardRow(categoryItem);
            }
        }

        protected override void RequestModelChange(IGraphDataAction changeAction)
        {
            DataStore.Dispatch(changeAction);
        }

        // Called by GraphDataStore.Subscribe after the model has been changed
        protected override void ModelChanged(GraphData graphData, IGraphDataAction changeAction)
        {
            // If categoryData associated with this controller is removed by an operation, destroy controller and views associated
            if (graphData.ContainsCategory(Model) == false)
            {
                Dispose();
                return;
            }

            switch (changeAction)
            {
                case AddGeometryInputAction addBlackboardItemAction:
                    if (addBlackboardItemAction.geometryInputReference != null && IsInputInCategory(addBlackboardItemAction.geometryInputReference))
                    {
                        var blackboardRow = FindBlackboardRow(addBlackboardItemAction.geometryInputReference);
                        if (blackboardRow == null)
                            blackboardRow = InsertBlackboardRow(addBlackboardItemAction.geometryInputReference);
                        // Rows should auto-expand when an input is first added
                        // blackboardRow.expanded = true;
                        var propertyView = blackboardRow.Q<GGBlackboardField>();
                        if (addBlackboardItemAction.addInputActionType == AddGeometryInputAction.AddActionSource.AddMenu)
                            propertyView.OpenTextEditor();
                    }
                    break;

                case CopyGeometryInputAction copyGeometryInputAction:
                    // In the specific case of only-one keywords like Material Quality and Raytracing, they can get copied, but because only one can exist, the output copied value is null
                    if (copyGeometryInputAction.copiedGeometryInput != null && IsInputInCategory(copyGeometryInputAction.copiedGeometryInput))
                    {
                        var blackboardRow = InsertBlackboardRow(copyGeometryInputAction.copiedGeometryInput, copyGeometryInputAction.insertIndex);
                        if (blackboardRow != null)
                        {
                            var graphView = ViewModel.parentView.GetFirstAncestorOfType<GeometryGraphView>();
                            var propertyView = blackboardRow.Q<GGBlackboardField>();
                            graphView?.AddToSelectionNoUndoRecord(propertyView);
                        }
                    }
                    break;

                case AddItemToCategoryAction addItemToCategoryAction:
                    // If item was added to category that this controller manages, then add blackboard row to represent that item
                    if (addItemToCategoryAction.itemToAdd != null && addItemToCategoryAction.categoryGuid == ViewModel.associatedCategoryGuid)
                    {
                        InsertBlackboardRow(addItemToCategoryAction.itemToAdd, addItemToCategoryAction.indexToAddItemAt);
                    }
                    else
                    {
                        // If the added input has been added to a category other than this one, and it used to belong to this category,
                        // Then cleanup the controller and view that used to represent that input
                        foreach (var key in m_BlackboardItemControllers.Keys)
                        {
                            var blackboardItemController = m_BlackboardItemControllers[key];
                            if (blackboardItemController.Model == addItemToCategoryAction.itemToAdd)
                            {
                                RemoveBlackboardRow(addItemToCategoryAction.itemToAdd);
                                break;
                            }
                        }
                    }
                    break;

                case DeleteCategoryAction deleteCategoryAction:
                    if (deleteCategoryAction.categoriesToRemoveGuids.Contains(ViewModel.associatedCategoryGuid))
                    {
                        this.Dispose();
                        return;
                    }

                    break;

                case ChangeCategoryIsExpandedAction changeIsExpandedAction:
                    if (changeIsExpandedAction.categoryGuids.Contains(ViewModel.associatedCategoryGuid))
                    {
                        ViewModel.isExpanded = changeIsExpandedAction.isExpanded;
                        m_BlackboardCategoryView.TryDoFoldout(changeIsExpandedAction.isExpanded);
                    }
                    break;

                case ChangeCategoryNameAction changeCategoryNameAction:
                    if (changeCategoryNameAction.categoryGuid == ViewModel.associatedCategoryGuid)
                    {
                        ViewModel.name = Model.name;
                        m_BlackboardCategoryView.title = ViewModel.name;
                    }
                    break;
            }
        }

        internal bool IsInputInCategory(GeometryInput shaderInput)
        {
            return Model != null && Model.IsItemInCategory(shaderInput);
        }

        internal GGBlackboardRow FindBlackboardRow(GeometryInput shaderInput)
        {
            m_BlackboardItemControllers.TryGetValue(shaderInput.objectId, out var associatedController);
            return associatedController?.BlackboardItemView;
        }

        // Creates controller, view and view model for a blackboard item and adds the view to the specified index in the category
        // By default adds it to the end of the list if no insertionIndex specified
        internal GGBlackboardRow InsertBlackboardRow(GeometryInput geometryInput, int insertionIndex = -1)
        {
            var geometryInputViewModel = new GeometryInputViewModel()
            {
                model = geometryInput,
                parentView = blackboardCategoryView,
            };
            var blackboardItemController = new GeometryInputViewController(geometryInput, geometryInputViewModel, DataStore);

            m_BlackboardItemControllers.TryGetValue(geometryInput.objectId, out var existingItemController);
            if (existingItemController == null)
            {
                m_BlackboardItemControllers.Add(geometryInput.objectId, blackboardItemController);
                // If no index specified, or if trying to insert at last index, add to end of category
                if (insertionIndex == -1 || insertionIndex == m_BlackboardItemControllers.Count() - 1)
                    blackboardCategoryView.Add(blackboardItemController.BlackboardItemView);
                else
                    blackboardCategoryView.Insert(insertionIndex, blackboardItemController.BlackboardItemView);

                blackboardCategoryView.MarkDirtyRepaint();

                return blackboardItemController.BlackboardItemView;
            }
            else
            {
                throw new AssertionException("Tried to add blackboard item that already exists to category.", null);
            }
        }

        internal void RemoveBlackboardRow(GeometryInput geometryInput)
        {
            m_BlackboardItemControllers.TryGetValue(geometryInput.objectId, out var associatedBlackboardItemController);
            if (associatedBlackboardItemController != null)
            {
                associatedBlackboardItemController.Dispose();
                m_BlackboardItemControllers.Remove(geometryInput.objectId);
            }
            else
                throw new AssertionException("Failed to find associated blackboard item controller for shader input that was just deleted. Cannot clean up view associated with input.", null);
        }

        void ClearBlackboardRows()
        {
            foreach (var geometryInputViewController in m_BlackboardItemControllers.Values)
                geometryInputViewController.Dispose();

            m_BlackboardItemControllers.Clear();
        }

        public override void Dispose()
        {
            if (blackboard == null)
                return;

            base.Dispose();
            Cleanup();
            ClearBlackboardRows();
            m_UnregisterAll?.Invoke();

            blackboard = null;
            m_BlackboardCategoryView?.Dispose();
            m_BlackboardCategoryView?.Clear();
            m_BlackboardCategoryView = null;
        }
    }
}
