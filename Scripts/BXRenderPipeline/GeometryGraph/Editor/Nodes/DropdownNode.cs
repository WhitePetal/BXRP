using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BXGraphing;
using UnityEngine;

namespace BXGeometryGraph
{
    [Serializable]
    [Title("Utility", "Dropdown")]
    class DropdownNode : AbstractGeometryNode, IOnAssetEnabled, IGeneratesShaderBodyCode, IGeometryInputObserver
    {
        internal const int k_MinEnumEntries = 2;

        public DropdownNode()
        {
            UpdateNodeAfterDeserialization();
        }

        [SerializeField]
        JsonRef<GeometryDropdown> m_Dropdown;

        public GeometryDropdown dropdown
        {
            get { return m_Dropdown; }
            set
            {
                if (m_Dropdown == value)
                    return;

                m_Dropdown = value;
                UpdateNode();
                Dirty(ModificationScope.Topological);
            }
        }

        public override bool canSetPrecision => false;
        public override bool hasPreview => true;
        public const int OutputSlotId = 0;

        public override bool allowedInMainGraph { get => false; }

        public void UpdateNodeDisplayName(string newDisplayName)
        {
            GeometrySlot foundSlot = FindSlot<GeometrySlot>(OutputSlotId);

            if (foundSlot != null)
                foundSlot.displayName = newDisplayName;
        }

        public void OnEnable()
        {
            UpdateNode();
        }

        public void UpdateNode()
        {
            name = dropdown.displayName;
            UpdatePorts();
        }

        void UpdatePorts()
        {
            // Get slots
            List<GeometrySlot> inputSlots = new List<GeometrySlot>();
            GetInputSlots(inputSlots);

            // Store the edges
            Dictionary<GeometrySlot, List<IEdge>> edgeDict = new Dictionary<GeometrySlot, List<IEdge>>();
            foreach (GeometrySlot slot in inputSlots)
                edgeDict.Add(slot, (List<IEdge>)slot.owner.owner.GetEdges(slot.slotReference));

            // Remove old slots
            for (int i = 0; i < inputSlots.Count; i++)
            {
                RemoveSlot(inputSlots[i].id);
            }

            // Add output slot
            AddSlot(new DynamicVectorGeometrySlot(OutputSlotId, "Out", "Out", SlotType.Output, Vector4.zero));

            // Add input slots
            int[] slotIds = new int[dropdown.entries.Count + 1];
            slotIds[dropdown.entries.Count] = OutputSlotId;
            for (int i = 0; i < dropdown.entries.Count; i++)
            {
                // Get slot based on entry id
                GeometrySlot slot = inputSlots.Where(x =>
                    x.id == dropdown.entries[i].id &&
                    x.RawDisplayName() == dropdown.entries[i].displayName &&
                    x.geometryOutputName == dropdown.entries[i].displayName).FirstOrDefault();

                if (slot == null)
                {
                    slot = new DynamicVectorGeometrySlot(dropdown.entries[i].id, dropdown.entries[i].displayName, dropdown.entries[i].displayName, SlotType.Input, Vector4.zero);
                }

                AddSlot(slot);
                slotIds[i] = dropdown.entries[i].id;
            }
            RemoveSlotsNameNotMatching(slotIds);

            // Reconnect the edges
            foreach (KeyValuePair<GeometrySlot, List<IEdge>> entry in edgeDict)
            {
                foreach (IEdge edge in entry.Value)
                {
                    owner.Connect(edge.outputSlot, edge.inputSlot);
                }
            }

            ValidateNode();
        }

        public void GenerateNodeShaderCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var outputSlot = FindOutputSlot<GeometrySlot>(OutputSlotId);

            bool isGeneratingSubgraph = owner.isSubGraph && (generationMode != GenerationMode.Preview);
            if (generationMode == GenerationMode.Preview || !isGeneratingSubgraph)
            {
                sb.AppendLine(string.Format($"{outputSlot.concreteValueType.ToGeometryString()} {GetVariableNameForSlot(OutputSlotId)};"));
                var value = GetSlotValue(GetSlotIdForActiveSelection(), generationMode);
                sb.AppendLine(string.Format($"{GetVariableNameForSlot(OutputSlotId)} = {value};"));
            }
            else
            {
                // Iterate all entries in the dropdown
                for (int i = 0; i < dropdown.entries.Count; i++)
                {
                    if (i == 0)
                    {
                        sb.AppendLine(string.Format($"{outputSlot.concreteValueType.ToGeometryString()} {GetVariableNameForSlot(OutputSlotId)};"));
                        sb.AppendLine($"if ({m_Dropdown.value.referenceName} == {i})");
                    }
                    else
                    {
                        sb.AppendLine($"else if ({m_Dropdown.value.referenceName} == {i})");
                    }

                    {
                        sb.AppendLine("{");
                        sb.IncreaseIndent();
                        var value = GetSlotValue(GetSlotIdForPermutation(new KeyValuePair<GeometryDropdown, int>(dropdown, i)), generationMode);
                        sb.AppendLine(string.Format($"{GetVariableNameForSlot(OutputSlotId)} = {value};"));
                        sb.DecreaseIndent();
                        sb.AppendLine("}");
                    }

                    if (i == dropdown.entries.Count - 1)
                    {
                        sb.AppendLine($"else");
                        sb.AppendLine("{");
                        sb.IncreaseIndent();
                        var value = GetSlotValue(GetSlotIdForPermutation(new KeyValuePair<GeometryDropdown, int>(dropdown, 0)), generationMode);
                        sb.AppendLine(string.Format($"{GetVariableNameForSlot(OutputSlotId)} = {value};"));
                        sb.DecreaseIndent();
                        sb.AppendLine("}");
                    }
                }
            }
        }

        public int GetSlotIdForPermutation(KeyValuePair<GeometryDropdown, int> permutation)
        {
            return permutation.Key.entries[permutation.Value].id;
        }

        public int GetSlotIdForActiveSelection()
        {
            return dropdown.entries[dropdown.value].id;
        }

        protected override void CalculateNodeHasError()
        {
            if (dropdown == null || !owner.dropdowns.Any(x => x == dropdown))
            {
                owner.AddConcretizationError(objectId, "Dropdown Node has no associated dropdown.");
                hasError = true;
            }
        }

        public void OnGeometryInputUpdated(ModificationScope modificationScope)
        {
            UpdateNode();
            Dirty(modificationScope);

            if (modificationScope == ModificationScope.Layout)
                UpdateNodeDisplayName(dropdown.displayName);
        }
    }
}
