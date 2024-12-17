using System;
using System.Collections;
using System.Collections.Generic;
using BXGraphing;
using UnityEngine;

namespace BXGeometryGraph
{
    class BlockNode : AbstractGeometryNode
    {
        [SerializeField]
        string m_SerializedDescriptor;

        [NonSerialized]
        private ContextData m_ContextData;

        [NonSerialized]
        private BlockFieldDescriptor m_Descriptor;

        public override string displayName
        {
            get
            {
                string displayName = "";
                if (m_Descriptor != null)
                {
                    //displayName = m_Descriptor.shaderStage.ToString();
                    //if (!string.IsNullOrEmpty(displayName))
                        //displayName += " ";
                    displayName += m_Descriptor.displayName;
                }

                return displayName;
            }
        }

        public override bool canCutNode => false;
        public override bool canCopyNode => false;

        public override string documentationURL => "https://github.com/WhitePetal/FloowDream/tree/main/Scripts/BXRenderPipeline/GeometryGraph/Editor/Resources/Documents/BlockNode";

        // Because the GraphData is deserialized after its child elements
        // the descriptor list is not built (and owner is not set)
        // at the time of node deserialization
        // Therefore we need to deserialize this element at GraphData.OnAfterDeserialize
        public string serializedDescriptor => m_SerializedDescriptor;

        public ContextData contextData
        {
            get => m_ContextData;
            set => m_ContextData = value;
        }

        public int index => contextData.blocks.IndexOf(this);

        public BlockFieldDescriptor descriptor
        {
            get => m_Descriptor;
            set => m_Descriptor = value;
        }

        private const string k_CustomBlockDefaultName = "CustomInterpolator";

        internal enum CustomBlockType { Float = 1, Vector2 = 2, Vector3 = 3, Vector4 = 4 }

        internal bool isCustomBlock { get => m_Descriptor?.isCustom ?? false; }

        internal string customName
        {
            get => m_Descriptor.name;
            set => OnCustomBlockFieldModified(value, customWidth);
        }

        internal CustomBlockType customWidth
        {
            get => (CustomBlockType)ControlToWidth(m_Descriptor.control);
            set => OnCustomBlockFieldModified(customName, value);
        }

        public void Init(BlockFieldDescriptor fieldDescriptor)
        {
            m_Descriptor = fieldDescriptor;

            // custom blocks can be "copied" via a custom Field Descriptor, we'll use the CI name instead though.
            name = !isCustomBlock
                ? $"{fieldDescriptor.tag}.{fieldDescriptor.name}"
                : $"{BlockFields.GeometryDescription.name}.{k_CustomBlockDefaultName}";

            // TODO: This exposes the MaterialSlot API
            // TODO: This needs to be removed but is currently required by HDRP for DiffusionProfileInputMaterialSlot
            if(m_Descriptor is CustomSlotBlockFieldDescriptor customSlotDesciptor)
            {
                var newSlot = customSlotDesciptor.createSlot();
                AddSlot(newSlot);
                RemoveSlotsNameNotMatching(new int[] { 0 });
                return;
            }

            AddSlotFromControlType();
        }

        internal void InitCustomDefault()
        {
            Init(MakeCustomBlockField(k_CustomBlockDefaultName, CustomBlockType.Vector4));
        }

        private void AddSlotFromControlType(bool attemptToModifyExisting = true)
        {
            // TODO: this should really just use callbacks like the CustomSlotBlockFieldDescriptor. then we wouldn't need this switch to make a copy
            var stageCapability = m_Descriptor.geometryStage.GetGeometryStageCapability();
            switch (descriptor.control)
            {
                case GeometryControl geometryControl:
                    AddSlot(new GeometryGeometrySlot(0, descriptor.displayName, descriptor.name, SlotType.Input, stageCapability), attemptToModifyExisting);
                    break;
                case FloatControl floatControl:
                    AddSlot(new Vector1GeometrySlot(0, descriptor.displayName, descriptor.name, SlotType.Input, floatControl.value, stageCapability), attemptToModifyExisting);
                    break;
            }
        }
    }
}
