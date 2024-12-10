using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BXGraphing;
using UnityEngine;
using UnityEngine.Assertions;

namespace BXGeometryGraph
{
    [Serializable]
    [FormerName("BXGeometryGraph.GeometryGraph")]
    [FormerName("BXGeometryGraph.SubGraph")]
    [FormerName("BXGeometryGraph.AbstructGeometryGraph")]
    sealed class GraphData : JsonObject
    {
        public override int latestVersion => 3;

        public GraphObject owner { get; set; }

        [NonSerialized]
        internal bool graphIsConcretizing = false;

        [SerializeField]
        List<JsonData<AbstractGeometryProperty>> m_Properties = new List<JsonData<AbstractGeometryProperty>>();

        public DataValueEnumerable<AbstractGeometryProperty> properties => m_Properties.SelectValue();

        //[SerializeField]
        //private List<JsonData<ShaderKeyword>> m_Keywords = new List<JsonData<ShaderKeyword>>();

        //public DataValueEnumerable<ShaderKeyword> keywords => m_Keywords.SelectValue();

        //[SerializeField]
        //private List<JsonData<GeometryDropDown>> m_Dropdowns = new List<JsonData<GeometryDropDown>>();

        //public DataValueEnumerable<GeometryDropDown> dropdowns => m_Dropdowns.SelectValue();

        [NonSerialized]
        private List<GeometryInput> m_AddedInputs = new List<GeometryInput>();

        public IEnumerable<GeometryInput> addedInputs
        {
            get { return m_AddedInputs; }
        }

        [NonSerialized]
        private List<GeometryInput> m_RemovedInputs = new List<GeometryInput>();

        public IEnumerable<GeometryInput> removedInputs
        {
            get { return m_RemovedInputs; }
        }

        [NonSerialized]
        List<GeometryInput> m_MovedInputs = new List<GeometryInput>();

        public IEnumerable<GeometryInput> movedInpus
        {
            get { return m_MovedInputs; }
        }

        [NonSerialized]
        private List<CategoryData> m_AddedCategories = new List<CategoryData>();

        public IEnumerable<CategoryData> addedCategoires
        {
            get { return m_AddedCategories; }
        }

        [NonSerialized]
        private List<CategoryData> m_RemovedCategories = new List<CategoryData>();

        public IEnumerable<CategoryData> removedCategories
        {
            get { return m_RemovedCategories; }
        }

        [NonSerialized]
        List<CategoryData> m_MovedCategories = new List<CategoryData>();

        public IEnumerable<CategoryData> movedCategories
        {
            get { return m_MovedCategories; }
        }

        [NonSerialized]
        private bool m_MovedContexts = false;
        public bool movedContexts => m_MovedContexts;

        public string assetGuid { get; set; }

        [SerializeField]
        private List<JsonData<CategoryData>> m_CategoryData = new List<JsonData<CategoryData>>();

        public DataValueEnumerable<CategoryData> categories => m_CategoryData.SelectValue();

        [SerializeField]
        private List<JsonData<AbstractGeometryNode>> m_Nodes = new List<JsonData<AbstractGeometryNode>>();

        [NonSerialized]
        Dictionary<string, AbstractGeometryNode> m_NodeDictionary = new Dictionary<string, AbstractGeometryNode>();

        [NonSerialized]
        Dictionary<string, AbstractGeometryNode> m_LegacyUpdateDictionary = new Dictionary<string, AbstractGeometryNode>();

        public IEnumerable<T> GetNodes<T>()
        {
            return m_Nodes.SelectValue().OfType<T>();
        }

        [NonSerialized]
        private List<AbstractGeometryNode> m_AddedNodes = new List<AbstractGeometryNode>();

        public IEnumerable<AbstractGeometryNode> addedNodes
        {
            get { return m_AddedNodes; }
        }

        [NonSerialized]
        private List<AbstractGeometryNode> m_RemovedNodes = new List<AbstractGeometryNode>();

        public IEnumerable<AbstractGeometryNode> removedNodes
        {
            get { return m_RemovedNodes; }
        }

        [NonSerialized]
        private List<AbstractGeometryNode> m_PastedNodes = new List<AbstractGeometryNode>();

        public IEnumerable<AbstractGeometryNode> pastedNodes
        {
            get { return m_PastedNodes; }
        }

        [SerializeField]
        private List<JsonData<GroupData>> m_GroupDatas = new List<JsonData<GroupData>>();

        public DataValueEnumerable<GroupData> groups
        {
            get { return m_GroupDatas.SelectValue(); }
        }

        [NonSerialized]
        private List<GroupData> m_AddedGroups = new List<GroupData>();

        public IEnumerable<GroupData> addedGroups
        {
            get { return m_AddedGroups; }
        }

        [NonSerialized]
        private List<GroupData> m_RemovedGroups = new List<GroupData>();

        public IEnumerable<GroupData> removedGroups
        {
            get { return m_RemovedGroups; }
        }

        [NonSerialized]
        private List<GroupData> m_PastedGroups = new List<GroupData>();

        public IEnumerable<GroupData> pastedGroups
        {
            get { return m_PastedGroups; }
        }

        [NonSerialized]
        private List<ParentGroupChange> m_ParentGroupChanges = new List<ParentGroupChange>();

        public IEnumerable<ParentGroupChange> parentGroupChanges
        {
            get { return m_ParentGroupChanges; }
        }

        [NonSerialized]
        private GroupData m_MostRecentlyCreatedGroup;

        public GroupData mostRecentlyCreatedGroup => m_MostRecentlyCreatedGroup;

        [NonSerialized]
        private Dictionary<JsonRef<GroupData>, List<IGroupItem>> m_GroupItems = new Dictionary<JsonRef<GroupData>, List<IGroupItem>>();

        public IEnumerable<IGroupItem> GetItemsInGroup(GroupData groupData)
        {
            if (m_GroupItems.TryGetValue(groupData, out var nodes))
            {
                return nodes;
            }
            return Enumerable.Empty<IGroupItem>();
        }

        [SerializeField]
        private List<JsonData<StickyNoteData>> m_StickyNoteData = new List<JsonData<StickyNoteData>>();

        public DataValueEnumerable<StickyNoteData> stickyNotes => m_StickyNoteData.SelectValue();

        [NonSerialized]
        private List<StickyNoteData> m_AddedStickyNotes = new List<StickyNoteData>();

        public List<StickyNoteData> addedStickyNotes => m_AddedStickyNotes;

        [NonSerialized]
        private List<StickyNoteData> m_RemovedNotes = new List<StickyNoteData>();

        public List<StickyNoteData> removedNotes => m_RemovedNotes;

        [NonSerialized]
        private List<StickyNoteData> m_PastedStickyNotes = new List<StickyNoteData>();

        public IEnumerable<StickyNoteData> pastedStickyNotes => m_PastedStickyNotes;

        [SerializeField]
        private List<Edge> m_Edges = new List<Edge>();

        public IEnumerable<Edge> edges => m_Edges;

        [NonSerialized]
        private Dictionary<string, List<IEdge>> m_NodeEdges = new Dictionary<string, List<IEdge>>();

        [NonSerialized]
        private List<IEdge> m_AddedEdges = new List<IEdge>();

        public IEnumerable<IEdge> addedEdges
        {
            get { return m_AddedEdges; }
        }

        [NonSerialized]
        private List<IEdge> m_RemovedEdges = new List<IEdge>();

        public IEnumerable<IEdge> removedEdges
        {
            get { return m_RemovedEdges; }
        }

        private ContextData m_GepmetryContext;

        // We build this once and cache it as it uses reflection
        // This list is used to build the Create Node menu entries for Blocks
        // as well as when deserializing descriptor fields on serialized Blocks
        [NonSerialized]
        private List<BlockFieldDescriptor> m_BlockFieldDescriptors;

        public ContextData geometryContext => m_GepmetryContext;
        public List<BlockFieldDescriptor> blockFieldDescriptors => m_BlockFieldDescriptors;

        [SerializeField]
        InspectorPreviewData m_PreviewData = new InspectorPreviewData();

        public InspectorPreviewData previewData
        {
            get { return m_PreviewData; }
            set { m_PreviewData = value; }
        }

        [SerializeField]
        private string m_Path;

        public string path
        {
            get { return m_Path; }
            set
            {
                if (m_Path == value)
                    return;
                m_Path = value;
                if (owner != null)
                    owner.RegisterCompleteObjectUndo("Change Path");
            }
        }

        public MessageManager messageManager { get; set; }
        public bool isSubGraph { get; set; }

        // we default this to Graph for subgraphs
        // but for shadergraphs, this will get replaced with Single
        [SerializeField]
        private GraphPrecision m_GraphPrecision = GraphPrecision.Graph;

        public GraphPrecision graphDefaultPrecision
        {
            get
            {
                // shader graphs are not allowed to have graph precision
                // we force them to Single if they somehow get set to graph
                if ((!isSubGraph) && (m_GraphPrecision == GraphPrecision.Graph))
                    return GraphPrecision.Single;

                return m_GraphPrecision;
            }
        }

        public ConcretePrecision graphDefaultConcretePrecision
        {
            get
            {
                // when in "Graph switchable" mode, we choose Half as the default concrete precision
                // so you can visualize the worst-case
                return graphDefaultPrecision.ToConcrete(ConcretePrecision.Half);
            }
        }

        // Some state has been changed that requires checking for the auto add/removal of blocks.
        // This needs to be checked at a later point in time so actions like replace (remove + add) don't remove blocks.
        internal bool checkAutoAddRemoveBlocks { get; set; }

        public void SetGraphDefaultPrecision(GraphPrecision newGraphDefaultPrecision)
        {
            if ((!isSubGraph) && (newGraphDefaultPrecision == GraphPrecision.Graph))
            {
                // shader graphs can't be set to "Graph", only subgraphs can
                Debug.LogError("Cannot set GeometryGraph to a default precision of Graph");
            }
            else
            {
                m_GraphPrecision = newGraphDefaultPrecision;
            }
        }

        // NOTE: having preview mode default to 3D preserves the old behavior of pre-existing subgraphs
        // if we change this, we would have to introduce a versioning step if we want to maintain the old behavior
        [SerializeField]
        private PreviewMode m_PreviewMode = PreviewMode.Preview3D;

        public PreviewMode previewMode
        {
            get => m_PreviewMode;
            set => m_PreviewMode = value;
        }

        [SerializeField]
        JsonRef<AbstractGeometryNode> m_OutputNode;

        public AbstractGeometryNode outputNode
        {
            get => m_OutputNode;
            set => m_OutputNode = value;
        }

        internal delegate void SaveGraphDelegate(Geometry shader, object context);
        internal static SaveGraphDelegate onSaveGraph;

        [SerializeField]
        internal List<JsonData<AbstractGeometryGraphDataExtension>> m_SubDatas = new List<JsonData<AbstractGeometryGraphDataExtension>>();
        public DataValueEnumerable<AbstractGeometryGraphDataExtension> SubDatas => m_SubDatas.SelectValue();

        // only ignores names matching ignoreName on properties matching ignoreGuid
        public List<string> BuildPropertyDisplayNameList(AbstractGeometryProperty ignoreProperty, string ignoreName)
        {
            List<String> result = new List<String>();
            foreach (var p in properties)
            {
                int before = result.Count;
                p.GetPropertyDisplayNames(result);

                if ((p == ignoreProperty) && (ignoreName != null))
                {
                    // remove ignoreName, if it was just added
                    for (int i = before; i < result.Count; i++)
                    {
                        if (result[i] == ignoreName)
                        {
                            result.RemoveAt(i);
                            break;
                        }
                    }
                }
            }

            return result;
        }

        public string SanitizeGraphInputName(GeometryInput input, string desiredName)
        {
            string currentName = input.displayName;
            string sanitizedName = desiredName.Trim();
            switch (input)
            {
                case AbstractGeometryProperty property:
                    sanitizedName = GraphUtil.SanitizeName(BuildPropertyDisplayNameList(property, currentName), "{0} ({1})", sanitizedName);
                    break;
                //case ShaderKeyword keyword:
                //sanitizedName = GraphUtil.SanitizeName(keywords.Where(p => p != input).Select(p => p.displayName), "{0} ({1})", sanitizedName);
                //break;
                //case ShaderDropdown dropdown:
                //sanitizedName = GraphUtil.SanitizeName(dropdowns.Where(p => p != input).Select(p => p.displayName), "{0} ({1})", sanitizedName);
                //break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return sanitizedName;
        }

        [SerializeField]
        List<JsonData<ShaderKeyword>> m_Keywords = new List<JsonData<ShaderKeyword>>();

        public DataValueEnumerable<ShaderKeyword> keywords => m_Keywords.SelectValue();

        [SerializeField]
        List<JsonData<GeometryDropdown>> m_Dropdowns = new List<JsonData<GeometryDropdown>>();

        public DataValueEnumerable<GeometryDropdown> dropdowns => m_Dropdowns.SelectValue();

        public string SanitizeGraphInputReferenceName(GeometryInput input, string desiredName)
        {
            var sanitizedName = NodeUtils.ConvertToValidHLSLIdentifier(desiredName /**, (desiredName) => (NodeUtils.IsShaderLabKeyWord(desiredName) || NodeUtils.IsShaderGraphKeyWord(desiredName)) **/);

            switch (input)
            {
                case AbstractGeometryProperty property:
                    {
                        // must deduplicate ref names against keywords, dropdowns, and properties, as they occupy the same name space
                        var existingNames = properties.Where(p => p != property).Select(p => p.referenceName).Union(keywords.Select(p => p.referenceName)).Union(dropdowns.Select(p => p.referenceName));
                        sanitizedName = GraphUtil.DeduplicateName(existingNames, "{0}_{1}", sanitizedName);
                    }
                    break;
                //case ShaderKeyword keyword:
                //    {
                //        // must deduplicate ref names against keywords, dropdowns, and properties, as they occupy the same name space
                //        sanitizedName = sanitizedName.ToUpper();
                //        var existingNames = properties.Select(p => p.referenceName).Union(keywords.Where(p => p != input).Select(p => p.referenceName)).Union(dropdowns.Select(p => p.referenceName));
                //        sanitizedName = GraphUtil.DeduplicateName(existingNames, "{0}_{1}", sanitizedName);
                //    }
                //    break;
                //case ShaderDropdown dropdown:
                //    {
                //        // must deduplicate ref names against keywords, dropdowns, and properties, as they occupy the same name space
                //        var existingNames = properties.Select(p => p.referenceName).Union(keywords.Select(p => p.referenceName)).Union(dropdowns.Where(p => p != input).Select(p => p.referenceName));
                //        sanitizedName = GraphUtil.DeduplicateName(existingNames, "{0}_{1}", sanitizedName);
                //    }
                //    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return sanitizedName;
        }

        // copies the GeometryInput, and adds it to the graph with proper name sanitization, returning the copy
        public GeometryInput AddCopyOfGeometryInput(GeometryInput source, int insertIndex = -1)
        {
            GeometryInput copy = source.Copy();

            // some GeometryInputs cannot be copied (unknown types)
            if (copy == null)
                return null;

            // copy common properties that should always be copied over
            copy.generatePropertyBlock = source.generatePropertyBlock;      // the exposed toggle

            if ((source is AbstractGeometryProperty sourceProp) && (copy is AbstractGeometryProperty copyProp))
            {
                copyProp.hidden = sourceProp.hidden;
                copyProp.precision = sourceProp.precision;
                copyProp.overrideHLSLDeclaration = sourceProp.overrideHLSLDeclaration;
                copyProp.hlslDeclarationOverride = sourceProp.hlslDeclarationOverride;
                copyProp.useCustomSlotLabel = sourceProp.useCustomSlotLabel;
            }

            // sanitize the display name (we let the .Copy() function actually copy the display name over)
            copy.SetDisplayNameAndSanitizeForGraph(this);

            // copy and sanitize the reference name (must do this after the display name, so the default is correct)
            if (source.IsUsingNewDefaultRefName())
            {
                // if source was using new default, we can just rely on the default for the copy we made.
                // the code above has already handled collisions properly for the default,
                // and it will assign the same name as the source if there are no collisions.
                // Also it will result better names chosen when there are collisions.
            }
            else
            {
                // when the source is using an old default, we set it as an override
                copy.SetReferenceNameAndSanitizeForGraph(this, source.referenceName);
            }

            copy.OnBeforePasteIntoGraph(this);
            AddGraphInputNoSanitization(copy, insertIndex);

            return copy;
        }

        public void RemoveGraphInput(GeometryInput input)
        {
            switch (input)
            {
                case AbstractGeometryProperty property:
                    var propertyNodes = GetNodes<PropertyNode>().Where(x => x.property == input).ToList();
                    foreach (var propertyNode in propertyNodes)
                        ReplacePropertyNodeWithConcreteNodeNoValidate(propertyNode);
                    break;
            }

            // Also remove this input from any category it existed in
            foreach (var categoryData in categories)
            {
                if (categoryData.IsItemInCategory(input))
                {
                    categoryData.RemoveItemFromCategory(input);
                    break;
                }
            }

            RemoveGraphInputNoValidate(input);
            ValidateGraph();
        }

        public void MoveCategory(CategoryData category, int newIndex)
        {
            if (newIndex > m_CategoryData.Count || newIndex < 0)
            {
                throw new AssertionException("New index is not within categories list.", null);
            }

            var currentIndex = m_CategoryData.IndexOf(category);
            if (currentIndex == -1)
            {
                throw new AssertionException("Category is not in graph.", null);
            }

            if (newIndex == currentIndex)
                return;

            m_CategoryData.RemoveAt(currentIndex);
            if (newIndex > currentIndex)
                newIndex--;
            var isLast = newIndex == m_CategoryData.Count;
            if (isLast)
                m_CategoryData.Add(category);
            else
                m_CategoryData.Insert(newIndex, category);
            if (!m_MovedCategories.Contains(category))
                m_MovedCategories.Add(category);
        }

        public void MoveItemInCategory(GeometryInput itemToMove, int newIndex, string associatedCategoryGuid)
        {
            foreach (var categoryData in categories)
            {
                if (categoryData.categoryGuid == associatedCategoryGuid && categoryData.IsItemInCategory(itemToMove))
                {
                    // Validate new index to move the item to
                    if (newIndex < -1 || newIndex >= categoryData.childCount)
                    {
                        throw new AssertionException("Provided invalid index input to MoveItemInCategory.", null);
                    }

                    categoryData.MoveItemInCategory(itemToMove, newIndex);
                    break;
                }
            }
        }

        public int GetGraphInputIndex(GeometryInput input)
        {
            switch (input)
            {
                case AbstractGeometryProperty property:
                    return m_Propertes.IndexOf(property);
                case ShaderKeyword keyword:
                    return m_Keywords.IndexOf(keyword);
                case GeometryDropdown dropdown:
                    return m_Dropdowns.IndexOf(dropdown);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void RemoveGraphInputNoValidate(GeometryInput geometryInput)
        {
            if (geometryInput is AbstractGeometryProperty property && m_Properties.Remove(property) ||
                geometryInput is ShaderKeyword keyword && m_Keywords.Remove(keyword) ||
                geometryInput is GeometryDropdown dropdown && m_Dropdowns.Remove(dropdown))
            {
                m_RemovedInputs.Add(geometryInput);
                m_AddedInputs.Remove(geometryInput);
                m_MovedInputs.Remove(geometryInput);
            }
        }

        private static List<IEdge> s_TempEdges = new List<IEdge>();

        public void ReplacePropertyNodeWithConcreteNode(PropertyNode propertyNode)
        {
            ReplacePropertyNodeWithConcreteNodeNoValidate(propertyNode);
            ValidateGraph();
        }

        private void ReplacePropertyNodeWithConcreteNodeNoValidate(PropertyNode propertyNode, bool deleteNodeIfNoConcreteFormExists = true)
        {
            var property = properties.FirstOrDefault(x => x == propertyNode.property) ?? propertyNode.property;
            if (property == null)
                return;

            var node = property.ToConcreteNode() as AbstractGeometryNode;
            if (node == null)   // Some nodes have no concrete form
            {
                if (deleteNodeIfNoConcreteFormExists)
                    RemoveNodeNoValidate(propertyNode);
                return;
            }

            var slot = propertyNode.FindOutputSlot<GeometrySlot>(PropertyNode.OutputSlotId);
            var newSlot = node.GetOutputSlots<GeometrySlot>().FirstOrDefault(s => s.valueType == slot.valueType);
            if (newSlot == null)
                return;

            node.drawState = propertyNode.drawState;
            node.group = propertyNode.group;
            AddNodeNoValidate(node);

            foreach (var edge in this.GetEdges(slot.slotReference))
                ConnectNoValidate(newSlot.slotReference, edge.inputSlot);

            RemoveNodeNoValidate(propertyNode);
        }

        public void AddCategory(CategoryData categoryDataReference)
        {
            m_CategoryData.Add(categoryDataReference);
            m_AddedCategories.Add(categoryDataReference);
        }

        public string FindCategoryForInput(GeometryInput input)
        {
            foreach (var categoryData in categories)
            {
                if (categoryData.IsItemInCategory(input))
                {
                    return categoryData.categoryGuid;
                }
            }

            throw new AssertionException("Attempted to find category for an input that doesn't exist in the graph.", null);
        }

        public void ChangeCategoryName(string categoryGUID, string newName)
        {
            foreach (var categoryData in categories)
            {
                if (categoryData.categoryGuid == categoryGUID)
                {
                    var sanitizedCategoryName = GraphUtil.SanitizeCategoryName(newName);
                    categoryData.name = sanitizedCategoryName;
                    return;
                }
            }

            throw new AssertionException("Attempted to change name of a category that does not exist in the graph.", null);
        }

        public void InsertItemIntoCategory(string categoryGUID, GeometryInput itemToAdd, int insertionIndex = -1)
        {
            foreach (var categoryData in categories)
            {
                if (categoryData.categoryGuid == categoryGUID)
                {
                    categoryData.InsertItemIntoCategory(itemToAdd, insertionIndex);
                }
                // Also make sure to remove this items guid from an existing category if it exists within one
                else if (categoryData.IsItemInCategory(itemToAdd))
                {
                    categoryData.RemoveItemFromCategory(itemToAdd);
                }
            }
        }

        public void RemoveItemFromCategory(string categoryGUID, GeometryInput itemToRemove)
        {
            foreach (var categoryData in categories)
            {
                if (categoryData.categoryGuid == categoryGUID)
                {
                    categoryData.RemoveItemFromCategory(itemToRemove);
                    return;
                }
            }

            throw new AssertionException("Attempted to remove item from a category that does not exist in the graph.", null);
        }

        public void RemoveCategory(string categoryGUID)
        {
            var existingCategory = categories.FirstOrDefault(category => category.categoryGuid == categoryGUID);
            if (existingCategory != null)
            {
                m_CategoryData.Remove(existingCategory);
                m_RemovedCategories.Add(existingCategory);

                // Whenever a category is removed, also remove any inputs within that category
                foreach (var shaderInput in existingCategory.Children)
                    RemoveGraphInput(shaderInput);
            }
            else
                throw new AssertionException("Attempted to remove a category that does not exist in the graph.", null);
        }

        // This differs from the rest of the category handling functions due to how categories can be copied between graphs
        // Since we have no guarantee of us owning the categories, we need a direct reference to the category to copy
        public CategoryData CopyCategory(CategoryData categoryToCopy)
        {
            var copiedCategory = new CategoryData(categoryToCopy);
            AddCategory(copiedCategory);
            // Whenever a category is copied, also copy over all the inputs within that category
            foreach (var childInputToCopy in categoryToCopy.Children)
            {
                var newGeometryInput = AddCopyOfGeometryInput(childInputToCopy);
                copiedCategory.InsertItemIntoCategory(newGeometryInput);
            }

            return copiedCategory;
        }

        public void OnKeywordChanged()
        {
            OnKeywordChangedNoValidate();
            ValidateGraph();
        }

        public void OnKeywordChangedNoValidate()
        {
            DirtyAll<AbstractGeometryNode>(ModificationScope.Topological);
        }

        public void OnDropdownChanged()
        {
            OnDropdownChangedNoValidate();
            ValidateGraph();
        }

        public void OnDropdownChangedNoValidate()
        {
            DirtyAll<AbstractGeometryNode>(ModificationScope.Topological);
        }

        public void CleanupGraph()
        {
            //First validate edges, remove any
            //orphans. This can happen if a user
            //manually modifies serialized data
            //of if they delete a node in the inspector
            //debug view.
            foreach (var edge in edges.ToArray())
            {
                var outputNode = edge.outputSlot.node;
                var inputNode = edge.inputSlot.node;

                if (ContainsNode(outputNode) && ContainsNode(inputNode))
                {
                    outputSlot = outputNode.FindOutputSlot<GeometrySlot>(edge.outputSlot.slotId);
                    inputSlot = inputNode.FindInputSlot<GeometrySlot>(edge.inputSlot.slotId);
                }

                if (outputNode == null
                    || inputNode == null
                    || outputSlot == null
                    || inputSlot == null)
                {
                    //orphaned edge
                    RemoveEdgeNoValidate(edge, false);
                }
            }
        }

        private void DirtyAll<T>(ModificationScope modificationScope) where T : AbstractGeometryNode
        {
            graphIsConcretizing = true;
            try
            {
                var allNodes = GetNodes<T>();
                foreach (var node in allNodes)
                {
                    node.Dirty(modificationScope);
                    node.ValidateNode();
                }
            }
            catch (System.Exception e)
            {
                graphIsConcretizing = false;
                throw e;
            }
            graphIsConcretizing = false;
        }

        public void ValidateGraph()
        {
            messageManager?.ClearAllFromProvider(this);
            CleanupGraph();
            GraphSetup.SetupGraph(this);
            GraphConcretization.ConcretizeGraph(this);
            GraphValidation.ValidateGraph(this);

            for(int i = 0; i < m_AddedEdges.Count; ++i)
            {
                var edge = m_AddedEdges[i];
                if(!ContainsNode(edge.outputSlot.node) || !ContainsNode(edge.inputSlot.node))
                {
                    Debug.LogWarningFormat("Added edge is invalid: {0} -> {1}\n{2}", edge.outputSlot.node.objectId, edge.inputSlot.node.objectId, Environment.StackTrace);
                    m_AddedEdges.Remove(edge);
                }
            }

            for(int i = 0; i < m_ParentGroupChanges.Count; ++i)
            {
                var groupChange = m_ParentGroupChanges[i];
                switch (groupChange.groupItem)
                {
                    case AbstractGeometryNode node when !ContainsNode(node):
                    case StickyNoteData stickyNote when !m_StickyNoteDatas.Contains(stickyNote):
                        m_ParentGroupChanges.Remove(groupChange);
                        break;
                }
            }

            var existingDefaultCategory = categories.FirstOrDefault();
            if(existingDefaultCategory?.childCount == 0 && categories.Count() == 1 && (properties.Count() != 0 || keywords.Count() != 0 || dropdowns.Count() != 0))
            {
                // Have a graph with category data in invalid state
                // there is only one category, the default category, and all geometry inputs should belong to it
                // Clear category data as it will get reconstructed in the BlackboardController constructor
                m_CategoryData.Clear();
            }

            ValidateCustomBlockLimit();
            ValidateContextBlocks();
        }

        public void AddValidationError(string id, string errorMessage,
           GeometryCompilerMessageSeverity severity = GeometryCompilerMessageSeverity.Error)
        {
            messageManager?.AddOrAppendError(this, id, new GeometryMessage("Validation: " + errorMessage, severity));
        }

        public void AddSetupError(string id, string errorMessage,
           GeometryCompilerMessageSeverity severity = GeometryCompilerMessageSeverity.Error)
        {
            messageManager?.AddOrAppendError(this, id, new GeometryMessage("Setup: " + errorMessage, severity));
        }

        public void AddConcretizationError(string id, string errorMessage,
          GeometryCompilerMessageSeverity severity = GeometryCompilerMessageSeverity.Error)
        {
            messageManager?.AddOrAppendError(this, id, new GeometryMessage("Concretization: " + errorMessage, severity));
        }

        public void ClearErrorsForNode(AbstractGeometryNode node)
        {
            messageManager?.ClearNodesFromProvider(this, node.ToEnumerable());
        }

        internal bool replaceInProgress = false;
        public void ReplaceWith(GraphData other)
        {
        }
    }
}
