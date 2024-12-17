using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace BXGeometryGraph
{
    struct BlackboardGeometryInputOrder
    {
        public bool isKeyword;
        public bool isDropdown;
        //public KeywordType keywordType;
        public ShaderKeyword builtInKeyword;
        public string deprecatedPropertyName;
        public int version;
    }

    class BlackboardGeometryInputFactory
    {
        static public GeometryInput GetShaderInput(BlackboardGeometryInputOrder order)
        {
            GeometryInput output;
            if (order.isKeyword)
            {
                if (order.builtInKeyword == null)
                {
                    //output = new ShaderKeyword(order.keywordType);
                    output = null;
                }
                else
                {
                    output = order.builtInKeyword;
                }
            }
            else if (order.isDropdown)
            {
                output = new GeometryDropdown();
            }
            else
            {
                switch (order.deprecatedPropertyName)
                {
                    case "Color":
                        output = null;
                        //output = new ColorGeometryProperty(order.version);
                        break;
                    default:
                        output = null;
                        throw new AssertionException("BlackboardShaderInputFactory: Unknown deprecated property type.", null);
                }
            }

            return output;
        }
    }

    class AddGeometryInputAction : IGraphDataAction
    {
        public enum AddActionSource
        {
            Default,
            AddMenu
        }

        void AddGeometryInput(GraphData graphData)
        {
            Assert.IsNotNull(graphData, "GraphData is null while carrying out AddShaderInputAction");

            // If type property is valid, create instance of that type
            if (blackboardItemType != null && blackboardItemType.IsSubclassOf(typeof(BlackboardItem)))
            {
                geometryInputReference = (BlackboardItem)Activator.CreateInstance(blackboardItemType, true);
            }
            else if (m_GeometryInputReferenceGetter != null)
            {
                geometryInputReference = m_GeometryInputReferenceGetter();
            }
            // If type is null a direct override object must have been provided or else we are in an error-state
            else if (geometryInputReference == null)
            {
                throw new AssertionException("BlackboardController: Unable to complete Add Shader Input action.", null);
                return;
            }

            geometryInputReference.generatePropertyBlock = geometryInputReference.isExposable;

            if (graphData.owner != null)
                graphData.owner.RegisterCompleteObjectUndo("Add Shader Input");
            else
                throw new AssertionException("GraphObject is null while carrying out AddShaderInputAction", null);

            graphData.AddGraphInput(geometryInputReference);

            // If no categoryToAddItemToGuid is provided, add the input to the default category
            if (categoryToAddItemToGuid == String.Empty)
            {
                var defaultCategory = graphData.categories.FirstOrDefault();
                Assert.IsNotNull(defaultCategory, "Default category reference is null.");
                if (defaultCategory != null)
                {
                    var addItemToCategoryAction = new AddItemToCategoryAction();
                    addItemToCategoryAction.categoryGuid = defaultCategory.categoryGuid;
                    addItemToCategoryAction.itemToAdd = geometryInputReference;
                    graphData.owner.graphDataStore.Dispatch(addItemToCategoryAction);
                }
            }
            else
            {
                var addItemToCategoryAction = new AddItemToCategoryAction();
                addItemToCategoryAction.categoryGuid = categoryToAddItemToGuid;
                addItemToCategoryAction.itemToAdd = geometryInputReference;
                graphData.owner.graphDataStore.Dispatch(addItemToCategoryAction);
            }
        }

        public static AddGeometryInputAction AddDeprecatedPropertyAction(BlackboardGeometryInputOrder order)
        {
            return new() { geometryInputReference = BlackboardGeometryInputFactory.GetShaderInput(order), addInputActionType = AddGeometryInputAction.AddActionSource.AddMenu };
        }

        public static AddGeometryInputAction AddDropdownAction(BlackboardGeometryInputOrder order)
        {
            return new() { shaderInputReference = BlackboardGeometryInputFactory.GetShaderInput(order), addInputActionType = AddGeometryInputAction.AddActionSource.AddMenu };
        }

        public static AddGeometryInputAction AddKeywordAction(BlackboardGeometryInputOrder order)
        {
            return new() { shaderInputReference = BlackboardGeometryInputFactory.GetShaderInput(order), addInputActionType = AddGeometryInputAction.AddActionSource.AddMenu };
        }

        public static AddGeometryInputAction AddPropertyAction(Type shaderInputType)
        {
            return new() { blackboardItemType = shaderInputType, addInputActionType = AddGeometryInputAction.AddActionSource.AddMenu };
        }

        public Action<GraphData> modifyGraphDataAction => AddGeometryInput;
        // If this is a subclass of ShaderInput and is not null, then an object of this type is created to add to blackboard
        // If the type field above is null and this is provided, then it is directly used as the item to add to blackboard
        public BlackboardItem geometryInputReference { get; set; }
        public AddActionSource addInputActionType { get; set; }
        public string categoryToAddItemToGuid { get; set; } = String.Empty;

        Type blackboardItemType { get; set; }

        Func<BlackboardItem> m_GeometryInputReferenceGetter = null;
    }

    class ChangeGraphPathAction : IGraphDataAction
    {
        void ChangeGraphPath(GraphData graphData)
        {
            Assert.IsNotNull(graphData, "GraphData is null while carrying out ChangeGraphPathAction");
            graphData.path = NewGraphPath;
        }

        public Action<GraphData> modifyGraphDataAction => ChangeGraphPath;

        public string NewGraphPath { get; set; }
    }

    class CopyGeometryInputAction : IGraphDataAction
    {
        void CopyShaderInput(GraphData graphData)
        {
            Assert.IsNotNull(graphData, "GraphData is null while carrying out CopyShaderInputAction");
            Assert.IsNotNull(geometryInputToCopy, "GeometryInputToCopy is null while carrying out CopyShaderInputAction");

            // Don't handle undo here as there are different contexts in which this action is used, that define the undo action
            // TODO: Perhaps a sign that each of those need to be made their own actions instead of conflating intent into a single action

            switch (geometryInputToCopy)
            {
                case AbstractGeometryProperty property:

                    insertIndex = Mathf.Clamp(insertIndex, -1, graphData.properties.Count() - 1);
                    var copiedProperty = (AbstractGeometryProperty)graphData.AddCopyOfGeometryInput(property, insertIndex);
                    if (copiedProperty != null) // some property types cannot be duplicated (unknown types)
                    {
                        // Update the property nodes that depends on the copied node
                        foreach (var node in dependentNodeList)
                        {
                            if (node is PropertyNode propertyNode)
                            {
                                propertyNode.owner = graphData;
                                propertyNode.property = copiedProperty;
                            }
                        }
                    }


                    copiedGeometryInput = copiedProperty;
                    break;
                //case ShaderKeyword shaderKeyword:
                //    // InsertIndex gets passed in relative to the blackboard position of an item overall,
                //    // and not relative to the array sizes of the properties/keywords/dropdowns
                //    var keywordInsertIndex = insertIndex - graphData.properties.Count();

                //    keywordInsertIndex = Mathf.Clamp(keywordInsertIndex, -1, graphData.keywords.Count() - 1);

                //    // Don't duplicate built-in keywords within the same graph
                //    if (shaderKeyword.isBuiltIn && graphData.keywords.Any(p => p.referenceName == shaderInputToCopy.referenceName))
                //        return;

                //    var copiedKeyword = (ShaderKeyword)graphData.AddCopyOfGeometryInput(shaderKeyword, keywordInsertIndex);

                //    // Update the keyword nodes that depends on the copied node
                //    foreach (var node in dependentNodeList)
                //    {
                //        if (node is KeywordNode propertyNode)
                //        {
                //            propertyNode.owner = graphData;
                //            propertyNode.keyword = copiedKeyword;
                //        }
                //    }

                //    copiedShaderInput = copiedKeyword;
                //    break;
                //case GeometryDropdown geometryDropdown:
                //    // InsertIndex gets passed in relative to the blackboard position of an item overall,
                //    // and not relative to the array sizes of the properties/keywords/dropdowns
                //    var dropdownInsertIndex = insertIndex - graphData.properties.Count() - graphData.keywords.Count();

                //    dropdownInsertIndex = Mathf.Clamp(dropdownInsertIndex, -1, graphData.dropdowns.Count() - 1);

                //    var copiedDropdown = (GeometryDropdown)graphData.AddCopyOfGeometryInput(geometryDropdown, dropdownInsertIndex);

                //    // Update the dropdown nodes that depends on the copied node
                //    foreach (var node in dependentNodeList)
                //    {
                //        if (node is DropdownNode propertyNode)
                //        {
                //            propertyNode.owner = graphData;
                //            propertyNode.dropdown = copiedDropdown;
                //        }
                //    }

                //    copiedGeometryInput = copiedDropdown;
                //    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (copiedGeometryInput != null)
            {
                // If specific category to copy to is provided, find and use it
                foreach (var category in graphData.categories)
                {
                    if (category.categoryGuid == containingCategoryGuid)
                    {
                        // Ensures that the new item gets added after the item it was duplicated from
                        insertIndex += 1;
                        // If the source item was already the last item in list, just add to end of list
                        if (insertIndex >= category.childCount)
                            insertIndex = -1;
                        graphData.InsertItemIntoCategory(category.objectId, copiedGeometryInput, insertIndex);
                        return;
                    }
                }

                // Else, add to default category
                graphData.categories.First().InsertItemIntoCategory(copiedGeometryInput);
            }
        }

        public Action<GraphData> modifyGraphDataAction => CopyShaderInput;

        public IEnumerable<AbstractGeometryNode> dependentNodeList { get; set; } = new List<AbstractGeometryNode>();

        public BlackboardItem geometryInputToCopy { get; set; }

        public BlackboardItem copiedGeometryInput { get; set; }

        public string containingCategoryGuid { get; set; }

        public int insertIndex { get; set; } = -1;
    }

    class AddCategoryAction : IGraphDataAction
    {
        void AddCategory(GraphData graphData)
        {
            Assert.IsNotNull(graphData, "GraphData is null while carrying out AddCategoryAction");
            graphData.owner.RegisterCompleteObjectUndo("Add Category");
            // If categoryDataReference is not null, directly add it to graphData
            if (categoryDataReference == null)
                categoryDataReference = new CategoryData(categoryName, childObjects);
            graphData.AddCategory(categoryDataReference);
        }

        public Action<GraphData> modifyGraphDataAction => AddCategory;

        // Direct reference to the categoryData to use if it is specified
        public CategoryData categoryDataReference { get; set; }
        public string categoryName { get; set; } = String.Empty;
        public List<GeometryInput> childObjects { get; set; }
    }

    class MoveCategoryAction : IGraphDataAction
    {
        void MoveCategory(GraphData graphData)
        {
            Assert.IsNotNull(graphData, "GraphData is null while carrying out MoveCategoryAction");
            graphData.owner.RegisterCompleteObjectUndo("Move Category");
            // Handling for out of range moves is slightly different, but otherwise we need to reverse for insertion order.
            var guids = newIndexValue >= graphData.categories.Count() ? categoryGuids : categoryGuids.Reverse<string>();
            foreach (var guid in categoryGuids)
            {
                var cat = graphData.categories.FirstOrDefault(c => c.categoryGuid == guid);
                graphData.MoveCategory(cat, newIndexValue);
            }
        }

        public Action<GraphData> modifyGraphDataAction => MoveCategory;

        // Reference to the shader input being modified
        internal List<string> categoryGuids { get; set; }

        internal int newIndexValue { get; set; }
    }

    class AddItemToCategoryAction : IGraphDataAction
    {
        public enum AddActionSource
        {
            Default,
            DragDrop
        }

        void AddItemsToCategory(GraphData graphData)
        {
            Assert.IsNotNull(graphData, "GraphData is null while carrying out AddItemToCategoryAction");
            graphData.owner.RegisterCompleteObjectUndo("Add Item to Category");
            graphData.InsertItemIntoCategory(categoryGuid, itemToAdd, indexToAddItemAt);
        }

        public Action<GraphData> modifyGraphDataAction => AddItemsToCategory;

        public string categoryGuid { get; set; }

        public GeometryInput itemToAdd { get; set; }

        // By default an item is always added to the end of a category, if this value is set to something other than -1, will insert the item at that position within the category
        public int indexToAddItemAt { get; set; } = -1;

        public AddActionSource addActionSource { get; set; }
    }

    class CopyCategoryAction : IGraphDataAction
    {
        void CopyCategory(GraphData graphData)
        {
            Assert.IsNotNull(graphData, "GraphData is null while carrying out CopyCategoryAction");
            Assert.IsNotNull(categoryToCopyReference, "CategoryToCopyReference is null while carrying out CopyCategoryAction");

            // This is called by MaterialGraphView currently, no need to repeat it here, though ideally it would live here
            //graphData.owner.RegisterCompleteObjectUndo("Copy Category");

            newCategoryDataReference = graphData.CopyCategory(categoryToCopyReference);
        }

        // Reference to the new category created as a copy
        public CategoryData newCategoryDataReference { get; set; }

        // After category has been copied, store reference to it
        public CategoryData categoryToCopyReference { get; set; }

        public Action<GraphData> modifyGraphDataAction => CopyCategory;
    }

    class ShaderVariantLimitAction : IGraphDataAction
    {
        public int currentVariantCount { get; set; } = 0;
        public int maxVariantCount { get; set; } = 0;

        public ShaderVariantLimitAction(int currentVariantCount, int maxVariantCount)
        {
            this.maxVariantCount = maxVariantCount;
            this.currentVariantCount = currentVariantCount;
        }

        // There's no action actually performed on the graph, but we need to implement this as a valid function
        public Action<GraphData> modifyGraphDataAction => Empty;

        void Empty(GraphData graphData)
        {
        }
    }

    class BlackboardController : GGViewController<GraphData, BlackboardViewModel>
    {
        static BlackboardController()
        {
            var shaderInputTypes = TypeCache.GetTypesWithAttribute<BlackboardInputInfo>().ToList();
            // Sort the ShaderInput by priority using the BlackboardInputInfo attribute
            shaderInputTypes.Sort((s1, s2) =>
            {
                var info1 = Attribute.GetCustomAttribute(s1, typeof(BlackboardInputInfo)) as BlackboardInputInfo;
                var info2 = Attribute.GetCustomAttribute(s2, typeof(BlackboardInputInfo)) as BlackboardInputInfo;

                if (info1.priority == info2.priority)
                    return (info1.name ?? s1.Name).CompareTo(info2.name ?? s2.Name);
                else
                    return info1.priority.CompareTo(info2.priority);
            });

            s_GeometryInputTypes = shaderInputTypes.ToList();
        }

        internal BlackboardController(GraphData model, BlackboardViewModel inViewModel, GraphDataStore graphDataStore)
            : base(model, inViewModel, graphDataStore)
        {
            // TODO: hide this more generically for category types.
            bool useDropdowns = model.isSubGraph;
            InitializeViewModel(useDropdowns);

            blackboard = new GGBlackboard(ViewModel, this);

            // Add default category at the top of the blackboard (create it if it doesn't exist already)
            var existingDefaultCategory = DataStore.State.categories.FirstOrDefault();
            if (existingDefaultCategory != null && existingDefaultCategory.IsNamedCategory() == false)
            {
                AddBlackboardCategory(graphDataStore, existingDefaultCategory);
            }
            else
            {
                // Any properties that don't already have a category (for example, if this graph is being loaded from an older version that doesn't have category data)
                var uncategorizedBlackboardItems = new List<ShaderInput>();
                foreach (var shaderProperty in DataStore.State.properties)
                    if (IsInputUncategorized(shaderProperty))
                        uncategorizedBlackboardItems.Add(shaderProperty);

                foreach (var shaderKeyword in DataStore.State.keywords)
                    if (IsInputUncategorized(shaderKeyword))
                        uncategorizedBlackboardItems.Add(shaderKeyword);

                if (useDropdowns)
                {
                    foreach (var shaderDropdown in DataStore.State.dropdowns)
                        if (IsInputUncategorized(shaderDropdown))
                            uncategorizedBlackboardItems.Add(shaderDropdown);
                }

                var addCategoryAction = new AddCategoryAction();
                addCategoryAction.categoryDataReference = CategoryData.DefaultCategory(uncategorizedBlackboardItems);
                graphDataStore.Dispatch(addCategoryAction);
            }

            // Get the reference to default category controller after its been added
            m_DefaultCategoryController = m_BlackboardCategoryControllers.Values.FirstOrDefault();
            AssertHelpers.IsNotNull(m_DefaultCategoryController, "Failed to instantiate default category.");

            // Handle loaded-in categories from graph first, skipping the first/default category
            foreach (var categoryData in ViewModel.categoryInfoList.Skip(1))
            {
                AddBlackboardCategory(graphDataStore, categoryData);
            }
        }

        protected override void ModelChanged(GraphData graphData, IGraphDataAction changeAction)
        {
            throw new NotImplementedException();
        }

        protected override void RequestModelChange(IGraphDataAction changeAction)
        {
            throw new NotImplementedException();
        }
    }
}
