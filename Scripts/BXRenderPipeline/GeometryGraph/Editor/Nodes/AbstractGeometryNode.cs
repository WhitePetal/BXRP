using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BXGraphing;
using System;
using System.Linq;
using Unity.Collections;

namespace BXGeometryGraph
{
    [System.Serializable]
    public abstract class AbstractGeometryNode : INode, ISerializationCallbackReceiver, IGenerateProperties
    {
        protected static List<GeometrySlot> s_TempSlots = new List<GeometrySlot>();
        protected static List<IEdge> s_TempEdges = new List<IEdge>();
        protected static List<PreviewProperty> s_TempPreviewProperties = new List<PreviewProperty>();

        public enum OutputPrecision
        {
            @fixed,
            @half,
            @float
        }

        [NonSerialized]
        protected Guid m_Guid;

        [SerializeField]
        protected string m_GuidSerialized;

        [SerializeField]
        protected string m_Name;

        [SerializeField]
        protected DrawState m_DrawState;

        [SerializeField]
        protected List<ISlot> m_Slots = new List<ISlot>();

        [SerializeField]
        protected List<SerializationHelper.JSONSerializedElement> m_SerializableSlots = new List<SerializationHelper.JSONSerializedElement>();

        [NonSerialized]
        protected bool m_HasError;

        public Identifier tempId { get; set; }

        public IGraph owner { get; set; }

        protected OnNodeModified m_OnModified;

        public void RegisterCallback(OnNodeModified callback)
        {
            m_OnModified += callback;
        }

        public void UnregisterCallback(OnNodeModified callback)
        {
            m_OnModified -= callback;
        }

        public void Dirty(ModificationScope scope)
        {
            if (m_OnModified != null)
                m_OnModified(this, scope);
        }

        public Guid guid
        {
            get { return m_Guid; }
        }

        public string name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        public virtual string documentationURL
        {
            get { return null; }
        }

        public virtual bool canDeleteNode
        {
            get { return true; }
        }

        public DrawState drawState
        {
            get { return m_DrawState; }
            set
            {
                m_DrawState = value;
                Dirty(ModificationScope.Node);
            }
        }

        protected OutputPrecision m_OutputPrecision = OutputPrecision.@float;

        public OutputPrecision precision
        {
            get { return m_OutputPrecision; }
            set { m_OutputPrecision = value; }
        }

        [SerializeField]
        protected bool m_PreviewExpanded = true;

        public bool previewExpanded
        {
            get { return m_PreviewExpanded; }
            set
            {
                if (previewExpanded == value)
                    return;
                m_PreviewExpanded = value;
                Dirty(ModificationScope.Node);
            }
        }

        // Nodes that want to have a preview area can override this and return true
        public virtual bool hasPreview
        {
            get { return false; }
        }

        public virtual PreviewMode previewMode
        {
            get { return PreviewMode.Preview2D; }
        }

        public virtual bool allowedInSubGraph
        {
            get { return true; }
        }

        public virtual bool allowedInMainGraph
        {
            get { return true; }
        }

        public virtual bool allowedInLayerGraph
        {
            get { return true; }
        }

        public bool hasError
        {
            get { return m_HasError; }
            protected set { m_HasError = value; }
        }

        protected string m_DefaultVariableName;
        protected string m_NameForDefaultVariableName;
        protected Guid m_GuidForDefaultVariableName;

        protected string defaultVariableName
        {
            get
            {
                if(m_NameForDefaultVariableName != null || m_GuidForDefaultVariableName != guid)
                {
                    m_DefaultVariableName = string.Format("{0}_{1}", NodeUtils.GetHLSLSafeName(name ?? "node"), GuidEncoder.Encode(guid));
                    m_NameForDefaultVariableName = name;
                    m_GuidForDefaultVariableName = guid;
                }
                return m_DefaultVariableName;
            }
        }

        public int version { get; set; }

        //True if error
        protected virtual bool CalculateNodeHasError()
        {
            return false;
        }

        protected AbstractGeometryNode()
        {
            m_DrawState.expanded = true;
            m_Guid = Guid.NewGuid();
            version = 0;
        }

        public Guid RewriteGuid()
        {
            m_Guid = Guid.NewGuid();
            return m_Guid;
        }

        public void GetInputSlots<T>(List<T> foundSlots) where T : ISlot
        {
            foreach(var slot in m_Slots)
            {
                if (slot.isInputSlot && slot is T)
                    foundSlots.Add((T)slot);
            }
        }

        public void GetOutputSlots<T>(List<T> foundSlots) where T : ISlot
        {
            foreach(var slot in m_Slots)
            {
                if (slot.isOutputSlot && slot is T)
                    foundSlots.Add((T)slot);
            }
        }

        public void GetSlots<T>(List<T> foundSlots) where T : ISlot
        {
            foreach(var slot in m_Slots)
            {
                if (slot is T)
                    foundSlots.Add((T)slot);
            }
        }

        public virtual void CollectGeometryProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            foreach(var inputSlot in this.GetInputSlots<GeometrySlot>())
            {
                var edges = owner.GetEdges(inputSlot.slotReference);
                if (edges.Any())
                    continue;

                inputSlot.AddDefaultProperty(properties, generationMode);
            }
        }

        public string GetSlotValue(int inputSlotId, GenerationMode generationMode)
        {
            var inputSlot = FindSlot<GeometrySlot>(inputSlotId);
            if (inputSlot == null)
                return string.Empty;

            var edges = owner.GetEdges(inputSlot.slotReference).ToArray();

            if (edges.Any())
            {
                var fromSocketRef = edges[0].outputSlot;
                var fromNode = owner.GetNodeFromGuid<AbstractGeometryNode>(fromSocketRef.nodeGuid);
                if (fromNode == null)
                    return string.Empty;

                var slot = fromNode.FindOutputSlot<GeometrySlot>(fromSocketRef.slotId);
                if (slot == null)
                    return string.Empty;

                return GeometryGenerator.AdaptNodeOutput(fromNode, slot.id, inputSlot.concreteValueType);
            }

            return inputSlot.GetDefaultValue(generationMode);
        }

        public static bool ImplicitConversionExists(ConcreteSlotValueType from, ConcreteSlotValueType to)
        {
            if (from == to)
                return true;

            var fromCount = SlotValueHelper.GetChannelCount(from);
            var toCount = SlotValueHelper.GetChannelCount(to);

            if (toCount > 0 && fromCount > 0)
                return true;

            return false;
        }

        public virtual ConcreteSlotValueType ConvertDynamicInputTypeToConcrete(IEnumerable<ConcreteSlotValueType> inputTypes)
        {
            var concreteSlotValueTypes = inputTypes as IList<ConcreteSlotValueType> ?? inputTypes.ToList();

            var inputTypesDistinct = concreteSlotValueTypes.Distinct().ToList();
            switch (inputTypesDistinct.Count)
            {
                case 0:
                    return ConcreteSlotValueType.Vector1;
                case 1:
                    return inputTypesDistinct.FirstOrDefault();
                default:
                    // find the 'minumum' channel width excluding 1 as it can promote
                    inputTypesDistinct.RemoveAll(x => x == ConcreteSlotValueType.Vector1);
                    var ordered = inputTypesDistinct.OrderByDescending(x => x);
                    if (ordered.Any())
                        return ordered.FirstOrDefault();
                    break;
            }
            return ConcreteSlotValueType.Vector1;
        }

        public virtual ConcreteSlotValueType ConvertDynamicMatrixInputTypeToConcrete(IEnumerable<ConcreteSlotValueType> inputTypes)
        {
            var concreteSlotValueTypes = inputTypes as IList<ConcreteSlotValueType> ?? inputTypes.ToList();

            var inputTypesDistinct = concreteSlotValueTypes.Distinct().ToList();
            switch (inputTypesDistinct.Count)
            {
                case 0:
                    return ConcreteSlotValueType.Matrix2;
                case 1:
                    return inputTypesDistinct.FirstOrDefault();
                default:
                    var ordered = inputTypesDistinct.OrderByDescending(x => x);
                    if (ordered.Any())
                        return ordered.FirstOrDefault();
                    break;
            }
            return ConcreteSlotValueType.Matrix2;
        }

        public virtual void ValidateNode()
        {
            var isInError = false;

            // all children nodes needs to be updated first
            // so do that here
            var slots = ListPool<GeometrySlot>.Get();
            GetInputSlots(slots);
            foreach(var inputSlot in slots)
            {
                inputSlot.hasError = false;

                var edges = owner.GetEdges(inputSlot.slotReference);
                foreach(var edge in edges)
                {
                    var fromSocketRef = edge.outputSlot;
                    var outputNode = owner.GetNodeFromGuid(fromSocketRef.nodeGuid);
                    if (outputNode == null)
                        continue;

                    outputNode.ValidateNode();
                    if (outputNode.hasError)
                        isInError = true;
                }
            }
            ListPool<GeometrySlot>.Release(slots);

            var dynamicInputSlotsToCompare = DictionaryPool<DynamicVectorGeometrySlot, ConcreteSlotValueType>.Get();
            var skippedDynamicSlots = ListPool<DynamicVectorGeometrySlot>.Get();

            //var dynamicMatrixInputSlotsToCompare = DictionaryPool<DynamicMatrixGeometrySlot, ConcreteSlotValueType>.Get();
            //var skippedDynamicMatrixSlots = ListPool<DynamicMatrixGeometrySlot>.Get();

            // iterate the input slots
            s_TempSlots.Clear();
            GetInputSlots(s_TempSlots);
            foreach(var inputSlot in s_TempSlots)
            {
                // if there is a connection
                var edges = owner.GetEdges(inputSlot.slotReference).ToList();
                if (!edges.Any())
                {
                    if (inputSlot is DynamicVectorGeometrySlot)
                        skippedDynamicSlots.Add(inputSlot as DynamicVectorGeometrySlot);
                    //if (inputSlot is DynamicMatrixGeometrySlot)
                        //skippedDynamicMatrixSlots.Add(inputSlot as DynamicMatrixGeometrySlot);
                    continue;
                }

                // get the output details
                var outputSlotRef = edges[0].outputSlot;
                var outputNode = owner.GetNodeFromGuid(outputSlotRef.nodeGuid);
                if (outputNode == null)
                    continue;

                var outputSlot = outputNode.FindOutputSlot<GeometrySlot>(outputSlotRef.slotId);
                if (outputSlot == null)
                    continue;

                if(outputSlot.hasError)
                {
                    inputSlot.hasError = true;
                    continue;
                }

                var outputConcreteType = outputSlot.concreteValueType;
                // dynamic input... depends on output from other node.
                // we need to compare ALL dynamic inputs to make sure they
                // are compatable.
                if(inputSlot is DynamicVectorGeometrySlot)
                {
                    dynamicInputSlotsToCompare.Add((DynamicVectorGeometrySlot)inputSlot, outputConcreteType);
                    continue;
                }
                //else if(inputSlot is DynamicMatrixGeometrySlot)
                //{
                //    dynamicMatrixInputSlotsToCompare.Add((DynamicMatrixGeometrySlot)inputSlot, outputConcreteType);
                //    continue;
                //}

                // if we have a standard connection... just check the types work!
                if (!ImplicitConversionExists(outputConcreteType, inputSlot.concreteValueType))
                    inputSlot.hasError = true;
            }

            // we can now figure out the dynamic slotType
            // from here set all the
            var dynamicType = ConvertDynamicInputTypeToConcrete(dynamicInputSlotsToCompare.Values);
            foreach (var dynamicKvP in dynamicInputSlotsToCompare)
                dynamicKvP.Key.SetConcreteType(dynamicType);
            foreach (var skippedSlot in skippedDynamicSlots)
                skippedSlot.SetConcreteType(dynamicType);

            // and now dynamic matrices
            //var dynamicMatrixType = ConvertDynamicMatrixInputTypeToConcrete(dynamicMatrixInputSlotsToCompare.Values);
            //foreach (var dynamicKvP in dynamicMatrixInputSlotsToCompare)
            //    dynamicKvP.Key.SetConcreteType(dynamicMatrixType);
            //foreach (var skippedSlot in skippedDynamicMatrixSlots)
            //    skippedSlot.SetConcreteType(dynamicMatrixType);

            s_TempSlots.Clear();
            GetInputSlots(s_TempSlots);
            var inputError = s_TempSlots.Any(x => x.hasError);

            // configure the output slots now
            // their slotType will either be the default output slotType
            // or the above dynanic slotType for dynamic nodes
            // or error if there is an input error
            s_TempSlots.Clear();
            GetOutputSlots(s_TempSlots);
            foreach(var outputSlot in s_TempSlots)
            {
                outputSlot.hasError = false;

                if (inputError)
                {
                    outputSlot.hasError = true;
                    continue;
                }

                if(outputSlot is DynamicVectorGeometrySlot)
                {
                    (outputSlot as DynamicVectorGeometrySlot).SetConcreteType(dynamicType);
                    continue;
                }
                //else if(outputSlot is DynamicMatrixGeometrySlot)
                //{
                //    (outputSlot as DynamicMatrixGeometrySlot).SetConcreteType(dynamicMatrixType);
                //    continue;
                //}
            }

            isInError |= inputError;
            s_TempSlots.Clear();
            GetOutputSlots(s_TempSlots);
            isInError |= s_TempSlots.Any(x => x.hasError);
            isInError |= CalculateNodeHasError();
            hasError = isInError;

            if (!hasError)
            {
                ++version;
            }

            ListPool<DynamicVectorGeometrySlot>.Release(skippedDynamicSlots);
            DictionaryPool<DynamicVectorGeometrySlot, ConcreteSlotValueType>.Release(dynamicInputSlotsToCompare);

            //ListPool<DynamicMatrixGeometrySlot>.Release(skippedDynamicMatrixSlots);
            //DictionaryPool<DynamicMatrixGeometrySlot, ConcreteSlotValueType>.Release(dynamicMatrixInputSlotsToCompare);
        }

        public virtual void CollectPreviewGeometryProperties(List<PreviewProperty> properties)
        {
            s_TempSlots.Clear();
            GetInputSlots(s_TempSlots);
            foreach (var s in s_TempSlots)
            {
                s_TempPreviewProperties.Clear();
                s_TempEdges.Clear();
                owner.GetEdges(s.slotReference, s_TempEdges);
                if (s_TempEdges.Any())
                    continue;

                s.GetPreviewProperties(s_TempPreviewProperties, GetVariableNameForSlot(s.id));
                for (int i = 0; i < s_TempPreviewProperties.Count; i++)
                {
                    if (s_TempPreviewProperties[i].name == null)
                        continue;

                    properties.Add(s_TempPreviewProperties[i]);
                }
            }
        }

        public virtual string GetVariableNameForSlot(int slotId)
        {
            var slot = FindSlot<GeometrySlot>(slotId);
            if (slot == null)
                throw new ArgumentException(string.Format("Attempting to use GeometrySlot({0}) on node of type {1} where this slot can not be found", slotId, this), "slotId");
            return string.Format("_{0}_{1}", GetVariableNameForNode(), NodeUtils.GetHLSLSafeName(slot.geometryOutputName));
        }

        public virtual string GetVariableNameForNode()
        {
            return defaultVariableName;
        }

        public void AddSlot(ISlot slot)
        {
            if(!(slot is GeometrySlot))
                throw new ArgumentException(string.Format("Trying to add slot {0} to Geometry node {1}, but it is not a {2}", slot, this, typeof(GeometrySlot)));

            var addingSlot = (GeometrySlot)slot;
            var foundSlot = FindSlot<GeometrySlot>(slot.id);

            // this will remove the old slot and add a new one
            // if an old one was found. This allows updating values
            m_Slots.RemoveAll(x => x.id == ((GeometrySlot)slot).id);
            m_Slots.Add(slot);
            slot.owner = this;

            Dirty(ModificationScope.Topological);

            if (foundSlot == null)
                return;

            addingSlot.CopyValueFrom(foundSlot);
        }

        public void RemoveSlot(int slotId)
        {
            // Remove edges that use this slot
            // no owner can happen after creation
            // but before added to graph
            if(owner != null)
            {
                var edges = owner.GetEdges(GetSlotReference(slotId));

                foreach (var edge in edges.ToArray())
                    owner.RemoveEdge(edge);
            }

            // remove slots
            m_Slots.RemoveAll(x => x.id == slotId);

            Dirty(ModificationScope.Topological);
        }

        public void RemoveSlotsNameNotMatching(IEnumerable<int> slotIds, bool supressWarnings = false)
        {
            var invalidSlots = m_Slots.Select(x => x.id).Except(slotIds);

            foreach(var invalidSlot in invalidSlots.ToArray())
            {
                if(!supressWarnings)
                    Debug.LogWarningFormat("Removing Invalid GeometrySlot: {0}", invalidSlot);

                RemoveSlot(invalidSlot);
            }
        }

        public SlotReference GetSlotReference(int slotId)
        {
            var slot = FindSlot<ISlot>(slotId);
            if(slot == null)
                throw new ArgumentException("Slot could not be found", "slotId");

            return new SlotReference(guid, slotId);
        }

        public T FindSlot<T>(int slotId) where T : ISlot
        {
            foreach(var slot in m_Slots)
            {
                if (slot.id == slotId && slot is T)
                    return (T)slot;
            }
            return default(T);
        }

        public T FindInputSlot<T>(int slotId) where T : ISlot
        {
            foreach (var slot in m_Slots)
            {
                if (slot.isInputSlot && slot.id == slotId && slot is T)
                    return (T)slot;
            }
            return default(T);
        }

        public T FindOutputSlot<T>(int slotId) where T : ISlot
        {
            foreach (var slot in m_Slots)
            {
                if (slot.isOutputSlot && slot.id == slotId && slot is T)
                    return (T)slot;
            }
            return default(T);
        }

        public IEnumerable<ISlot> GetInputsWithNoConnection()
        {
            return this.GetInputSlots<ISlot>().Where(x => !owner.GetEdges(GetSlotReference(x.id)).Any());
        }

        public virtual void OnBeforeSerialize()
        {
            m_GuidSerialized = m_Guid.ToString();
            m_SerializableSlots = SerializationHelper.Serialize<ISlot>(m_Slots);
        }

        public virtual void OnAfterDeserialize()
        {
            if (!string.IsNullOrEmpty(m_GuidSerialized))
                m_Guid = new Guid(m_GuidSerialized);
            else
                m_Guid = Guid.NewGuid();

            m_Slots = SerializationHelper.Deserialize<ISlot>(m_SerializableSlots, GraphUtil.GetLegacyTypeRemapping());
            m_SerializableSlots = null;
            foreach (var s in m_Slots)
                s.owner = this;
            UpdateNodeAfterDeserialization();
        }

        public virtual void UpdateNodeAfterDeserialization()
        {

        }

        public bool IsSlotConnected(int slotId)
        {
            var slot = FindSlot<GeometrySlot>(slotId);
            return slot != null && owner.GetEdges(slot.slotReference).Any();
        }
    }
}
