using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BXGraphing;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace BXGeometryGraph
{
    [SerializeField]
    public abstract class AbstructGeometryGraph : IGraph, ISerializationCallbackReceiver, IGenerateProperties
    {
        static List<IEdge> s_TempEdges = new List<IEdge>();

        public IGraphObject owner { get; set; }

        [NonSerialized]
        private List<IGeometryProperty> m_Properties = new List<IGeometryProperty>();

        public IEnumerable<IGeometryProperty> properties
        {
            get { return m_Properties; }
        }

        [SerializeField]
        private List<SerializationHelper.JSONSerializedElement> m_SerializedProperties = new List<SerializationHelper.JSONSerializedElement>();

        [NonSerialized]
        private List<IGeometryProperty> m_AddedProperties = new List<IGeometryProperty>();

        public IEnumerable<IGeometryProperty> addedProperties
        {
            get { return m_AddedProperties; }
        }

        [NonSerialized]
        private List<Guid> m_RemovedProperties = new List<Guid>();

        public IEnumerable<Guid> removedProperties
        {
            get { return m_RemovedProperties; }
        }

        private List<IGeometryProperty> m_MovedProperties = new List<IGeometryProperty>();

        public IEnumerable<IGeometryProperty> movedProperties
        {
            get { return m_MovedProperties; }
        }

        [SerializeField]
        private SerializableGuid m_GUID = new SerializableGuid();

        public Guid guid
        {
            get { return m_GUID.guid; }
        }


        [NonSerialized]
        private Stack<Identifier> m_FreeNodeTempIds = new Stack<Identifier>();

        [NonSerialized]
        private List<AbstractGeometryNode> m_Nodes = new List<AbstractGeometryNode>();

        [NonSerialized]
        private Dictionary<Guid, INode> m_NodeDictionary = new Dictionary<Guid, INode>();

        public IEnumerable<T> GetNodes<T>() where T : INode
        {
            return m_Nodes.Where(x => x != null).OfType<T>();
        }

        [SerializeField]
        private List<SerializationHelper.JSONSerializedElement> m_SerializableNodes = new List<SerializationHelper.JSONSerializedElement>();

        [NonSerialized]
        private List<INode> m_AddedNodes = new List<INode>();

        public IEnumerable<INode> addedNodes
        {
            get { return m_AddedNodes; }
        }

        [NonSerialized]
        private List<INode> m_RemovedNodes = new List<INode>();

        public IEnumerable<INode> removedNodes
        {
            get { return m_RemovedNodes; }
        }

        [NonSerialized]
        private List<INode> m_PasteNodes = new List<INode>();

        public IEnumerable<INode> pastedNodes
        {
            get { return m_PasteNodes; }
        }


        [NonSerialized]
        private List<IEdge> m_Edges = new List<IEdge>();

        public IEnumerable<IEdge> edges
        {
            get { return m_Edges; }
        }

        [SerializeField]
        private List<SerializationHelper.JSONSerializedElement> m_SerializableEdges = new List<SerializationHelper.JSONSerializedElement>();

        [NonSerialized]
        private Dictionary<Guid, List<IEdge>> m_NodeEdges = new Dictionary<Guid, List<IEdge>>();

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


        [SerializeField]
        private string m_Path;

        public string path
        {
            get { return m_Path; }
            set
            {
                if (m_Path == value) return;
                m_Path = value;
                owner.RegisterCompleteObjectUndo("Change Path");
            }
        }

        public void ClearChanges()
        {
            m_AddedNodes.Clear();
            m_RemovedNodes.Clear();
            m_PasteNodes.Clear();
            m_AddedEdges.Clear();
            m_RemovedEdges.Clear();
            m_AddedProperties.Clear();
            m_RemovedProperties.Clear();
            m_MovedProperties.Clear();
        }

        public virtual void AddNode(INode node)
        {
            if(node is AbstractGeometryNode)
            {
                AddNodeNoValidate(node);
                ValidateGraph();
            }
            else
            {
                Debug.LogWarningFormat("Trying to add node {0} to Geometry graph, but it is not a {1}", node, typeof(AbstractGeometryNode));
            }
        }

        private void AddNodeNoValidate(INode node)
        {
            var gepmetrylNode = (AbstractGeometryNode)node;
            gepmetrylNode.owner = this;
            if (m_FreeNodeTempIds.Any())
            {
                var id = m_FreeNodeTempIds.Pop();
                id.IncrementVersion();
                gepmetrylNode.tempId = id;
                m_Nodes[id.index] = gepmetrylNode;
            }
            else
            {
                var id = new Identifier(m_Nodes.Count);
                gepmetrylNode.tempId = id;
                m_Nodes.Add(gepmetrylNode);
            }
            m_NodeDictionary.Add(gepmetrylNode.guid, gepmetrylNode);
            m_AddedNodes.Add(gepmetrylNode);
        }

        public void RemoveNode(INode node)
        {
            if (!node.canDeleteNode)
                return;

            RemoveNodeNoValidate(node);
            ValidateGraph();
        }

        private void RemoveNodeNoValidate(INode node)
        {
            var geometryNode = (AbstractGeometryNode)node;
            if (!geometryNode.canDeleteNode)
                return;

            m_Nodes[geometryNode.tempId.index] = null;
            m_FreeNodeTempIds.Push(geometryNode.tempId);
            m_NodeDictionary.Remove(geometryNode.guid);
            m_RemovedNodes.Add(geometryNode);
        }

        private void AddEdgeToNodeEdges(IEdge edge)
        {
            List<IEdge> inputEdges;
            if (!m_NodeEdges.TryGetValue(edge.inputSlot.nodeGuid, out inputEdges))
                m_NodeEdges[edge.inputSlot.nodeGuid] = inputEdges = new List<IEdge>();

            inputEdges.Add(edge);

            List<IEdge> outputEdges;
            if (!m_NodeEdges.TryGetValue(edge.outputSlot.nodeGuid, out outputEdges))
                m_NodeEdges[edge.outputSlot.nodeGuid] = outputEdges = new List<IEdge>();

            outputEdges.Add(edge);
        }

        private IEdge ConnectNoValidate(SlotReference fromSlotRef, SlotReference toSlotRef)
        {
            var fromNode = GetNodeFromGuid(fromSlotRef.nodeGuid);
            var toNode = GetNodeFromGuid(toSlotRef.nodeGuid);

            if (fromNode == null || toNode == null)
                return null;

            // if fromNode is already connected to toNode
            // do now allow a connection as toNode will then
            // have an edge to fromNode creating a cycle.
            // if this is parsed it will lead to an infinite loop.
            var dependentNodes = new List<INode>();
            NodeUtils.CollectNodesNodeFeedsInto(dependentNodes, toNode);
            if (dependentNodes.Contains(fromNode))
                return null;

            var fromSlot = fromNode.FindSlot<ISlot>(fromSlotRef.slotId);
            var toSlot = toNode.FindSlot<ISlot>(toSlotRef.slotId);

            if (fromSlot.isOutputSlot == toSlot.isOutputSlot)
                return null;

            var outputSlot = fromSlot.isOutputSlot ? fromSlotRef : toSlotRef;
            var inputSlot = fromSlot.isInputSlot ? fromSlotRef : toSlotRef;

            s_TempEdges.Clear();
            GetEdges(inputSlot, s_TempEdges);

            // remove any inputs that exits before adding
            foreach (var edge in s_TempEdges)
            {
                RemoveEdgeNoValidate(edge);
            }

            var newEdge = new Edge(outputSlot, inputSlot);
            m_Edges.Add(newEdge);
            m_AddedEdges.Add(newEdge);
            AddEdgeToNodeEdges(newEdge);

            //Debug.LogFormat("Connected edge: {0} -> {1} ({2} -> {3})\n{4}", newEdge.outputSlot.nodeGuid, newEdge.inputSlot.nodeGuid, fromNode.name, toNode.name, Environment.StackTrace);
            return newEdge;
        }

        protected void RemoveEdgeNoValidate(IEdge e)
        {
            e = m_Edges.FirstOrDefault(x => x.Equals(e));
            if (e == null)
                throw new ArgumentException("Trying to remove an edge that does not exist.", "e");
            m_Edges.Remove(e);

            List<IEdge> inputNodeEdges;
            if (m_NodeEdges.TryGetValue(e.inputSlot.nodeGuid, out inputNodeEdges))
                inputNodeEdges.Remove(e);

            List<IEdge> outputNodeEdges;
            if (m_NodeEdges.TryGetValue(e.outputSlot.nodeGuid, out outputNodeEdges))
                outputNodeEdges.Remove(e);

            m_RemovedEdges.Add(e);
        }

        public virtual IEdge Connect(SlotReference fromSlotRef, SlotReference toSlotRef)
        {
            var newEdge = ConnectNoValidate(fromSlotRef, toSlotRef);
            ValidateGraph();
            return newEdge;
        }

        public virtual void RemoveEdge(IEdge e)
        {
            RemoveEdgeNoValidate(e);
            ValidateGraph();
        }

        public void RemoveElements(IEnumerable<INode> nodes, IEnumerable<IEdge> edges)
        {
            foreach (var edge in edges.ToArray())
                RemoveEdgeNoValidate(edge);

            foreach (var serializableNode in nodes.ToArray())
                RemoveNodeNoValidate(serializableNode);

            ValidateGraph();
        }

        public INode GetNodeFromGuid(Guid guid)
        {
            INode node;
            m_NodeDictionary.TryGetValue(guid, out node);
            return node;
        }

        public T GetNodeFromGuid<T>(Guid guid) where T : INode
        {
            var node = GetNodeFromGuid(guid);
            if (node is T)
                return (T)node;

            return default(T);
        }

        public INode GetNodeFromTemId(Identifier tempId)
        {
            if(tempId.index > m_Nodes.Count)
                throw new ArgumentException("Trying to retrieve a node using an identifier that does not exist.");

            var node = m_Nodes[tempId.index];
            if(node == null)
                throw new Exception("Trying to retrieve a node using an identifier that does not exist.");
            if(node.tempId.version != tempId.version)
                throw new Exception("Trying to retrieve a node that was removed from the graph.");
            return node;
        }

        public bool ContainsNodeGuid(Guid guid)
        {
            return m_NodeDictionary.ContainsKey(guid);
        }

        public void GetEdges(SlotReference s, List<IEdge> foundEdges)
        {
            var node = GetNodeFromGuid(s.nodeGuid);
            if(node == null)
            {
                Debug.LogWarning("Node does not exist");
                return;
            }
            ISlot slot = node.FindSlot<ISlot>(s.slotId);

            List<IEdge> candidateEdges;
            if (!m_NodeEdges.TryGetValue(s.nodeGuid, out candidateEdges))
                return;

            foreach(var edge in candidateEdges)
            {
                var cs = slot.isInputSlot ? edge.inputSlot : edge.outputSlot;
                if (cs.nodeGuid == s.nodeGuid && cs.slotId == s.slotId)
                    foundEdges.Add(edge);
            }
        }

        public virtual void CollectGeometryProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            foreach (var prop in properties)
                collector.AddGeometryProperty(prop);
        }

        public void AddGeometryProperty(IGeometryProperty property)
        {
            if (property == null)
                return;

            if (m_Properties.Contains(property))
                return;

            m_Properties.Add(property);
            m_AddedProperties.Add(property);
        }

        public string SanitizePropertyName(string displayName, Guid guid = default(Guid))
        {
            displayName = displayName.Trim();
            return GraphUtil.SanitizeName(m_Properties.Where(p => p.guid != guid).Select(p => p.displayName), "{0} ({1})", displayName);
        }

        public string SanitizePropertyReferenceName(string referenceName, Guid guid = default(Guid))
        {
            referenceName = referenceName.Trim();

            if (string.IsNullOrEmpty(referenceName))
                return null;

            if (!referenceName.StartsWith("_"))
                referenceName = "_" + referenceName;

            referenceName = Regex.Replace(referenceName, @"(?:[^A-Za-z_0-9])|(?:\s)", "_");

            return GraphUtil.SanitizeName(m_Properties.Where(p => p.guid != guid).Select(p => p.referenceName), "{0}_{1}", referenceName);
        }

        public void RemoveGeometryProperty(Guid guid)
        {
            var propertyNodes = GetNodes<PropertyNode>().Where(x => x.propertyGuid == guid).ToList();
            foreach (var propNode in propertyNodes)
                ReplacePropertyNodeWithConcreteNodeNoValidate(propNode);

            RemoveGeometryPropertyNoValidate(guid);

            ValidateGraph();
        }

        public void MoveGeometryProperty(IGeometryProperty property, int newIndex)
        {
            if (newIndex > m_Properties.Count || newIndex < 0)
                throw new ArgumentException("New index is not within properties list.");
            var currentIndex = m_Properties.IndexOf(property);
            if (currentIndex == -1)
                throw new ArgumentException("Property is not in graph.");
            if (newIndex == currentIndex)
                return;
            m_Properties.RemoveAt(currentIndex);
            if (newIndex > currentIndex)
                newIndex--;
            var isLast = newIndex == m_Properties.Count;
            if (isLast)
                m_Properties.Add(property);
            else
                m_Properties.Insert(newIndex, property);
            if (!m_MovedProperties.Contains(property))
                m_MovedProperties.Add(property);
        }

        public int GetGeometryPropertyIndex(IGeometryProperty property)
        {
            return m_Properties.IndexOf(property);
        }

        private void RemoveGeometryPropertyNoValidate(Guid guid)
        {
            if (m_Properties.RemoveAll(x => x.guid == guid) > 0)
            {
                m_RemovedProperties.Add(guid);
                m_AddedProperties.RemoveAll(x => x.guid == guid);
                m_MovedProperties.RemoveAll(x => x.guid == guid);
            }
        }

        public void ReplacePropertyNodeWithConcreteNode(PropertyNode propertyNode)
        {
            ReplacePropertyNodeWithConcreteNodeNoValidate(propertyNode);
            ValidateGraph();
        }

        private void ReplacePropertyNodeWithConcreteNodeNoValidate(PropertyNode propertyNode)
        {
            var property = properties.FirstOrDefault(x => x.guid == propertyNode.propertyGuid);
            if (property == null)
                return;

            var node = property.ToConcreteNode();
            if (!(node is AbstractGeometryNode))
                return;

            var slot = propertyNode.FindOutputSlot<GeometrySlot>(PropertyNode.OutputSlotId);
            var newSlot = node.GetOutputSlots<GeometrySlot>().FirstOrDefault(s => s.valueType == slot.valueType);
            if (newSlot == null)
                return;

            node.drawState = propertyNode.drawState;
            AddNodeNoValidate(node);

            foreach (var edge in this.GetEdges(slot.slotReference))
                ConnectNoValidate(newSlot.slotReference, edge.inputSlot);

            RemoveNodeNoValidate(propertyNode);
        }

        public void ValidateGraph()
        {
            var propertyNodes = GetNodes<PropertyNode>().Where(n => !m_Properties.Any(p => p.guid == n.propertyGuid)).ToArray();
            foreach (var pNode in propertyNodes)
                ReplacePropertyNodeWithConcreteNodeNoValidate(pNode);

            //First validate edges, remove any
            //orphans. This can happen if a user
            //manually modifies serialized data
            //of if they delete a node in the inspector
            //debug view.
            foreach (var edge in edges.ToArray())
            {
                var outputNode = GetNodeFromGuid(edge.outputSlot.nodeGuid);
                var inputNode = GetNodeFromGuid(edge.inputSlot.nodeGuid);

                GeometrySlot outputSlot = null;
                GeometrySlot inputSlot = null;
                if (outputNode != null && inputNode != null)
                {
                    outputSlot = outputNode.FindOutputSlot<GeometrySlot>(edge.outputSlot.slotId);
                    inputSlot = inputNode.FindInputSlot<GeometrySlot>(edge.inputSlot.slotId);
                }

                if (outputNode == null
                    || inputNode == null
                    || outputSlot == null
                    || !outputSlot.IsCompatibleWith(inputSlot))
                {
                    //orphaned edge
                    RemoveEdgeNoValidate(edge);
                }
            }

            foreach (var node in GetNodes<INode>())
                node.ValidateNode();

            foreach(var edge in m_AddedEdges.ToList())
            {
                if(!ContainsNodeGuid(edge.outputSlot.nodeGuid) || !ContainsNodeGuid(edge.inputSlot.nodeGuid))
                {
                    Debug.LogWarningFormat("Added edge is invalid: {0} -> {1}\n{2}", edge.outputSlot.nodeGuid, edge.inputSlot.nodeGuid, Environment.StackTrace);
                    m_AddedEdges.Remove(edge);
                }
            }
        }

        public void ReplaceWith(IGraph other)
        {
            var otherGg = other as AbstructGeometryGraph;
            if(otherGg == null)
                throw new ArgumentException("Can only replace with another AbstractGeometryGraph", "other");

            using(var removedPropertiesPooledObject = ListPool<Guid>.GetDisposable())
            {
                var removedPropertyGuids = removedPropertiesPooledObject.value;
                foreach (var property in m_Properties)
                    removedPropertyGuids.Add(property.guid);
                foreach (var propertyGuid in removedPropertyGuids)
                    RemoveGeometryPropertyNoValidate(propertyGuid);
            }
            foreach(var otherProperty in otherGg.properties)
            {
                if (!properties.Any(p => p.guid == otherProperty.guid))
                    AddGeometryProperty(otherProperty);
            }

            other.ValidateGraph();
            ValidateGraph();

            foreach (var node in other.GetNodes<INode>())
                AddNodeNoValidate(node);
            foreach (var edge in other.edges)
                ConnectNoValidate(edge.outputSlot, edge.inputSlot);

            ValidateGraph();
        }

        internal void PasteGraph(CopyPasteGraph graphToPaste, List<INode> remappedNodes, List<IEdge> remappedEdges)
        {
            var nodeGuidMap = new Dictionary<Guid, Guid>();
            foreach(var node in graphToPaste.GetNodes<INode>())
            {
                INode pastedNode = node;

                var oldGuid = node.guid;
                var newGuid = node.RewriteGuid();
                nodeGuidMap[oldGuid] = newGuid;

                // Check if the property nodes need to be made into a concrete node.
                if(node is PropertyNode)
                {
                    PropertyNode propertyNode = (PropertyNode)node;

                    // If the property is not in the current graph, do check if the
                    // property can be made into a concrete node.
                    if(!m_Properties.Select(x => x.guid).Contains(propertyNode.propertyGuid))
                    {
                        // If the property is in the serialized paste graph, make the property node into a property node.
                        var pastedGraphMetaProperties = graphToPaste.metaProperties.Where(x => x.guid == propertyNode.propertyGuid);
                        if (pastedGraphMetaProperties.Any())
                        {
                            pastedNode = pastedGraphMetaProperties.FirstOrDefault().ToConcreteNode();
                            pastedNode.drawState = node.drawState;
                            nodeGuidMap[oldGuid] = pastedNode.guid;
                        }
                    }
                }

                var drawState = node.drawState;
                var position = drawState.position;
                position.x += 30;
                position.y += 30;
                drawState.position = position;
                node.drawState = drawState;
                remappedNodes.Add(pastedNode);
                AddNode(pastedNode);

                // add the node to the pasted node list
                m_PasteNodes.Add(pastedNode);
            }

            // only connect edges within pasted elements, discard
            // external edges.
            foreach(var edge in graphToPaste.edges)
            {
                var outputSlot = edge.outputSlot;
                var inputSlot = edge.inputSlot;

                Guid remappedOutputNodeGuid;
                Guid remappedInputNodeGuid;
                if(nodeGuidMap.TryGetValue(outputSlot.nodeGuid, out remappedOutputNodeGuid)
                    && nodeGuidMap.TryGetValue(inputSlot.nodeGuid, out remappedInputNodeGuid))
                {
                    var outputSlotRef = new SlotReference(remappedOutputNodeGuid, outputSlot.slotId);
                    var inputSlotRef = new SlotReference(remappedInputNodeGuid, inputSlot.slotId);
                    remappedEdges.Add(Connect(outputSlotRef, inputSlotRef));
                }
            }

            ValidateGraph();
        }

        public void OnBeforeSerialize()
        {
            m_SerializableNodes = SerializationHelper.Serialize(GetNodes<INode>());
            m_SerializableEdges = SerializationHelper.Serialize<IEdge>(m_Edges);
            m_SerializedProperties = SerializationHelper.Serialize<IGeometryProperty>(m_Properties);
        }

        public void OnAfterDeserialize()
        {
            // have to deserialize 'globals' before nodes
            m_Properties = SerializationHelper.Deserialize<IGeometryProperty>(m_SerializedProperties, GraphUtil.GetLegacyTypeRemapping());
            var nodes = SerializationHelper.Deserialize<INode>(m_SerializableNodes, GraphUtil.GetLegacyTypeRemapping());
            m_Nodes = new List<AbstractGeometryNode>(nodes.Count);
            m_NodeDictionary = new Dictionary<Guid, INode>(nodes.Count);
            foreach (var node in nodes.OfType<AbstractGeometryNode>())
            {
                node.owner = this;
                node.UpdateNodeAfterDeserialization();
                node.tempId = new Identifier(m_Nodes.Count);
                m_Nodes.Add(node);
                m_NodeDictionary.Add(node.guid, node);
            }

            m_SerializableNodes = null;

            m_Edges = SerializationHelper.Deserialize<IEdge>(m_SerializableEdges, GraphUtil.GetLegacyTypeRemapping());
            m_SerializableEdges = null;
            foreach (var edge in m_Edges)
                AddEdgeToNodeEdges(edge);
        }

        public void OnEnable()
        {
            foreach (var node in GetNodes<INode>().OfType<IOnAssetEnabled>())
            {
                node.OnEnable();
            }
        }
    }

    [Serializable]
    public class InspectorPreviewData
    {
        // TODO
        //public SerializableMesh serializedMesh = new SerializableMesh();

        [NonSerialized]
        public Quaternion rotation = Quaternion.identity;

        [NonSerialized]
        public float scale = 1f;
    }
}