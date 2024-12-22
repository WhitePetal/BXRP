using System;
using System.Collections;
using System.Collections.Generic;

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
			Debug.Log("m_descriptor: " + descriptor);
			switch (descriptor.control)
            {
                case GeometryControl geometryControl:
					Debug.Log("Add GeometryGeometrySlot");
                    AddSlot(new GeometryGeometrySlot(0, descriptor.displayName, descriptor.name, SlotType.Input, stageCapability), attemptToModifyExisting);
                    break;
                //case PositionControl positionControl:
                //    AddSlot(new PositionMaterialSlot(0, descriptor.displayName, descriptor.name, positionControl.space, stageCapability), attemptToModifyExisting);
                //    break;
                //case NormalControl normalControl:
                //    AddSlot(new NormalMaterialSlot(0, descriptor.displayName, descriptor.name, normalControl.space, stageCapability), attemptToModifyExisting);
                //    break;
                //case TangentControl tangentControl:
                //    AddSlot(new TangentMaterialSlot(0, descriptor.displayName, descriptor.name, tangentControl.space, stageCapability), attemptToModifyExisting);
                //    break;
                //case VertexColorControl vertexColorControl:
                //    AddSlot(new VertexColorMaterialSlot(0, descriptor.displayName, descriptor.name, stageCapability), attemptToModifyExisting);
                //    break;
                case ColorControl colorControl:
                    var colorMode = colorControl.hdr ? ColorMode.HDR : ColorMode.Default;
                    AddSlot(new ColorGeometrySlot(0, descriptor.displayName, descriptor.name, SlotType.Input, colorControl.value, colorMode, stageCapability), attemptToModifyExisting);
                    break;
                //case ColorRGBAControl colorRGBAControl:
                //    AddSlot(new ColorRGBAMaterialSlot(0, descriptor.displayName, descriptor.name, SlotType.Input, colorRGBAControl.value, stageCapability), attemptToModifyExisting);
                //    break;
                case FloatControl floatControl:
                    AddSlot(new Vector1GeometrySlot(0, descriptor.displayName, descriptor.name, SlotType.Input, floatControl.value, stageCapability), attemptToModifyExisting);
                    break;
                case Vector2Control vector2Control:
                    AddSlot(new Vector2GeometrySlot(0, descriptor.displayName, descriptor.name, SlotType.Input, vector2Control.value, stageCapability), attemptToModifyExisting);
                    break;
                case Vector3Control vector3Control:
                    AddSlot(new Vector3GeometrySlot(0, descriptor.displayName, descriptor.name, SlotType.Input, vector3Control.value, stageCapability), attemptToModifyExisting);
                    break;
                case Vector4Control vector4Control:
                    AddSlot(new Vector4GeometrySlot(0, descriptor.displayName, descriptor.name, SlotType.Input, vector4Control.value, stageCapability), attemptToModifyExisting);
                    break;
            }
            RemoveSlotsNameNotMatching(new int[] { 0 });
        }

        public override string GetVariableNameForNode()
        {
            // Temporary block nodes have temporary guids that cannot be used to set preview data
            // Since each block is unique anyway we just omit the guid
            return NodeUtils.GetHLSLSafeName(name);
        }

        private void OnCustomBlockFieldModified(string name, CustomBlockType width)
        {
            if (!isCustomBlock)
            {
                Debug.LogWarning(String.Format("{0} is not a custom interpolator.", this.name));
                return;
            }

            m_Descriptor = MakeCustomBlockField(name, width);

            // TODO: Preserve the original slot's value and try to reapply after the slot is updated.
            AddSlotFromControlType(false);

            owner?.ValidateGraph();
        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            if (descriptor != null)
            {
                if (isCustomBlock)
                {
                    int width = ControlToWidth(m_Descriptor.control);
                    m_SerializedDescriptor = $"{m_Descriptor.tag}.{m_Descriptor.name}#{width}";
                }
                else
                {
                    m_SerializedDescriptor = $"{m_Descriptor.tag}.{m_Descriptor.name}";
                }
            }
        }

        public override void OnAfterDeserialize()
        {
            // TODO: Go find someone to tell @esme not to do this.
            if (m_SerializedDescriptor.Contains("#"))
            {
                string descName = k_CustomBlockDefaultName;
                CustomBlockType descWidth = CustomBlockType.Vector4;
                var descTag = BlockFields.GeometryDescription.name;

                name = $"{descTag}.{descName}";

                var wsplit = m_SerializedDescriptor.Split(new char[] { '#', '.' });

                try
                {
                    descWidth = (CustomBlockType)int.Parse(wsplit[2]);
                }
                catch
                {
                    Debug.LogWarning(String.Format("Bad width found while deserializing custom interpolator {0}, defaulting to 4.", m_SerializedDescriptor));
                    descWidth = CustomBlockType.Vector4;
                }

                IControl control;
                try { control = (IControl)FindSlot<GeometrySlot>(0).InstantiateControl(); }
                catch { control = WidthToControl((int)descWidth); }

                descName = NodeUtils.ConvertToValidHLSLIdentifier(wsplit[1]);
                m_Descriptor = new BlockFieldDescriptor(descTag, descName, "", control, GeometryStage.Geometry, isCustom: true);
            }
        }

        private static BlockFieldDescriptor MakeCustomBlockField(string name, CustomBlockType width)
        {
            name = NodeUtils.ConvertToValidHLSLIdentifier(name);
            var referenceName = name;
            var define = "";
            IControl control = WidthToControl((int)width);
            var tag = BlockFields.GeometryDescription.name;

            return new BlockFieldDescriptor(tag, referenceName, define, control, GeometryStage.Geometry, isCustom: true);
        }

        private static IControl WidthToControl(int width)
        {
            switch (width)
            {
                case 1: return new FloatControl(default(float));
                case 2: return new Vector2Control(default(Vector2));
                case 3: return new Vector3Control(default(Vector3));
                case 4: return new Vector4Control(default(Vector4));
                default: return null;
            }
        }

        private static int ControlToWidth(IControl control)
        {
            switch (control)
            {
                case FloatControl a: return 1;
                case Vector2Control b: return 2;
                case Vector3Control c: return 3;
                case Vector4Control d: return 4;
                default: return -1;
            }
        }
    }
}
