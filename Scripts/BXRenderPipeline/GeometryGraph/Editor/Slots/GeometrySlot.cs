using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    [System.Serializable]
    abstract class GeometrySlot : JsonObject, IDisposable
    {
        const string k_NotInit = "Not Initilaized";

        [SerializeField]
        private int m_Id;

        [SerializeField]
        private string m_DisplayName = k_NotInit;

        [SerializeField]
        private SlotType m_SlotType = SlotType.Input;

        [SerializeField]
        private bool m_Hidden;

        [SerializeField]
        private string m_GeometryOutputName;

        [SerializeField]
        GeometryStageCapability m_StageCapability;

        private bool m_HasError;

        public GeometryStageCapability stageCapability
        {
            get { return m_StageCapability; }
            set { m_StageCapability = value; }
        }

        protected GeometrySlot()
        {

        }

        protected GeometrySlot(int slotId, string displayName, string geometryOutputName, SlotType slotType, GeometryStageCapability stageCapability = GeometryStageCapability.All, bool hidden = false)
        {
            m_Id = slotId;
            m_DisplayName = displayName;
            m_SlotType = slotType;
            m_Hidden = hidden;
            m_GeometryOutputName = geometryOutputName;
            this.stageCapability = stageCapability;
        }

        internal void SetInternalData(SlotType slotType, string geometryOutputName)
        {
            this.m_SlotType = slotType;
            this.geometryOutputName = geometryOutputName;
        }

        public bool IsConnectionTestable()
        {
            if(owner is SubGraphNode sgNode)
            {
                var property = sgNode.GetGeometryProperty(id);
                if (property != null)
                {
                    return property.isConnectionTestable;
                }
            }
            else if(owner is PropertyNode propertyNode)
            {
                return propertyNode.property.isConnectionTestable;
            }
            return false;
        }

        public VisualElement InstantiateCustomControl()
        {
            if (!isConnected && IsConnectionTestable())
            {
                var sgNode = owner as SubGraphNode;
                var property = sgNode.GetGeometryProperty(id);
                return new LabelSlotControlView(property.customSlotLabel);
            }
            return null;
        }

        public virtual VisualElement InstantiateControl()
        {
            return null;
        }

        private static string ConcreteSlotValueTypeAsString(ConcreteSlotValueType type)
        {
            switch (type)
            {
                case ConcreteSlotValueType.Vector1:
                    return "(1)";
                case ConcreteSlotValueType.Vector2:
                    return "(2)";
                case ConcreteSlotValueType.Vector3:
                    return "(3)";
                case ConcreteSlotValueType.Vector4:
                    return "(4)";
                case ConcreteSlotValueType.Boolean:
                    return "(B)";
                case ConcreteSlotValueType.Matrix2:
                    return "(2x2)";
                case ConcreteSlotValueType.Matrix3:
                    return "(3x3)";
                case ConcreteSlotValueType.Matrix4:
                    return "(4x4)";
                case ConcreteSlotValueType.SamplerState:
                    return "(SS)";
                case ConcreteSlotValueType.Texture2D:
                    return "(T2)";
                case ConcreteSlotValueType.Texture2DArray:
                    return "(T2A)";
                case ConcreteSlotValueType.Texture3D:
                    return "(T3)";
                case ConcreteSlotValueType.Cubemap:
                    return "(C)";
                case ConcreteSlotValueType.Gradient:
                    return "(G)";
                case ConcreteSlotValueType.VirtualTexture:
                    return "(VT)";
                case ConcreteSlotValueType.PropertyConnectionState:
                    return "(P)";
                case ConcreteSlotValueType.Geometry:
                    return "(Geo)";
                default:
                    return "(E)";
            }
        }

        public virtual string displayName
        {
            get { return m_DisplayName + ConcreteSlotValueTypeAsString(concreteValueType); }
            set { m_DisplayName = value; }
        }

        public string RawDisplayName()
        {
            return m_DisplayName;
        }

        public static GeometrySlot CreateGeometrySlot(SlotValueType type, int slotId, string displayName, string geometryOutputName, SlotType slotType, Vector4 defaultValue, GeometryStageCapability geometryStageCapability = GeometryStageCapability.All, bool hidden = false)
        {
            switch (type)
            {
                //case SlotValueType.SamplerState:
                    //return new SamplerStateMaterialSlot(slotId, displayName, shaderOutputName, slotType, shaderStageCapability, hidden);
                //case SlotValueType.DynamicMatrix:
                    //return new DynamicMatrixMaterialSlot(slotId, displayName, shaderOutputName, slotType, shaderStageCapability, hidden);
                //case SlotValueType.Matrix4:
                    //return new Matrix4MaterialSlot(slotId, displayName, shaderOutputName, slotType, shaderStageCapability, hidden);
                //case SlotValueType.Matrix3:
                    //return new Matrix3MaterialSlot(slotId, displayName, shaderOutputName, slotType, shaderStageCapability, hidden);
                //case SlotValueType.Matrix2:
                    //return new Matrix2MaterialSlot(slotId, displayName, shaderOutputName, slotType, shaderStageCapability, hidden);
                //case SlotValueType.Texture2D:
                    //return slotType == SlotType.Input
                        //? new Texture2DInputMaterialSlot(slotId, displayName, shaderOutputName, shaderStageCapability, hidden)
                        //: new Texture2DMaterialSlot(slotId, displayName, shaderOutputName, slotType, shaderStageCapability, hidden);
                //case SlotValueType.Texture2DArray:
                    //return slotType == SlotType.Input
                        //? new Texture2DArrayInputMaterialSlot(slotId, displayName, shaderOutputName, shaderStageCapability, hidden)
                        //: new Texture2DArrayMaterialSlot(slotId, displayName, shaderOutputName, slotType, shaderStageCapability, hidden);
                //case SlotValueType.Texture3D:
                    //return slotType == SlotType.Input
                        //? new Texture3DInputMaterialSlot(slotId, displayName, shaderOutputName, shaderStageCapability, hidden)
                        //: new Texture3DMaterialSlot(slotId, displayName, shaderOutputName, slotType, shaderStageCapability, hidden);
                //case SlotValueType.Cubemap:
                    //return slotType == SlotType.Input
                        //? new CubemapInputMaterialSlot(slotId, displayName, shaderOutputName, shaderStageCapability, hidden)
                        //: new CubemapMaterialSlot(slotId, displayName, shaderOutputName, slotType, shaderStageCapability, hidden);
                //case SlotValueType.VirtualTexture:
                    //return slotType == SlotType.Input
                        //? new VirtualTextureInputMaterialSlot(slotId, displayName, shaderOutputName, shaderStageCapability, hidden)
                        //: new VirtualTextureMaterialSlot(slotId, displayName, shaderOutputName, slotType, shaderStageCapability, hidden);
                //case SlotValueType.Gradient:
                    //return slotType == SlotType.Input
                        //? new GradientInputMaterialSlot(slotId, displayName, shaderOutputName, shaderStageCapability, hidden)
                        //: new GradientMaterialSlot(slotId, displayName, shaderOutputName, slotType, shaderStageCapability, hidden);
                case SlotValueType.DynamicVector:
                return new DynamicVectorGeometrySlot(slotId, displayName, geometryOutputName, slotType, defaultValue, geometryStageCapability, hidden);
                case SlotValueType.Vector4:
                    return new Vector4GeometrySlot(slotId, displayName, geometryOutputName, slotType, defaultValue, geometryStageCapability, hidden: hidden);
                case SlotValueType.Vector3:
                    return new Vector3GeometrySlot(slotId, displayName, geometryOutputName, slotType, defaultValue, geometryStageCapability, hidden: hidden);
                case SlotValueType.Vector2:
                    return new Vector2GeometrySlot(slotId, displayName, geometryOutputName, slotType, defaultValue, geometryStageCapability, hidden: hidden);
                case SlotValueType.Vector1:
                    return new Vector1GeometrySlot(slotId, displayName, geometryOutputName, slotType, defaultValue.x, geometryStageCapability, hidden: hidden);
                case SlotValueType.Integer1:
                    return new Integer1GeometrySlot(slotId, displayName, geometryOutputName, slotType, (int)defaultValue.x, geometryStageCapability, hidden: hidden);
                //case SlotValueType.Dynamic:
                //return new DynamicValueMaterialSlot(slotId, displayName, shaderOutputName, slotType, new Matrix4x4(defaultValue, Vector4.zero, Vector4.zero, Vector4.zero), shaderStageCapability, hidden);
                case SlotValueType.Boolean:
                    return new BooleanGeometrySlot(slotId, displayName, geometryOutputName, slotType, defaultValue.x == 1, geometryStageCapability, hidden);
                //case SlotValueType.PropertyConnectionState:
                //return new PropertyConnectionStateMaterialSlot(slotId, displayName, shaderOutputName, slotType, shaderStageCapability, hidden);
                case SlotValueType.Geometry:
                    return new GeometryGeometrySlot(slotId, displayName, geometryOutputName, slotType, geometryStageCapability, hidden: hidden);
            }

            throw new ArgumentOutOfRangeException("type", type, null);
        }

        public SlotReference slotReference
        {
            get { return new SlotReference(owner, m_Id); }
        }

        public AbstractGeometryNode owner { get; set; }

        // if hidden, the slot does not create a port in the UI
        public bool hidden
        {
            get { return m_Hidden; }
            set { m_Hidden = value; }
        }

        public int id
        {
            get { return m_Id; }
        }

        public bool isInputSlot
        {
            get { return m_SlotType == SlotType.Input; }
        }

        public bool isOutputSlot
        {
            get { return m_SlotType == SlotType.Output; }
        }

        public SlotType slotType
        {
            get { return m_SlotType; }
        }

        public bool isConnected
        {
            get
            {
                // node and graph respectivly
                if (owner == null || owner.owner == null)
                    return false;

                var graph = owner.owner;
                var edges = graph.GetEdges(slotReference);
                return edges.Any();
            }
        }

        public abstract bool isDefaultValue { get; }

        public abstract SlotValueType valueType { get; }

        public abstract ConcreteSlotValueType concreteValueType { get; }

        public string geometryOutputName
        {
            get { return m_GeometryOutputName; }
            set { m_GeometryOutputName = value; }
        }

        public bool hasError
        {
            get { return m_HasError; }
            set { m_HasError = value; }
        }

        public bool IsUsingDefaultValue()
        {
            if (!isConnected && isDefaultValue)
                return true;
            else
                return false;
        }

        public bool IsCompatibleWith(GeometrySlot otherSlot)
        {
            return otherSlot != null
                && otherSlot.owner != owner
                && otherSlot.isInputSlot != isInputSlot
                && !hidden
                && !otherSlot.hidden
                && ((isInputSlot
                    ? SlotValueHelper.AreCompatible(valueType, otherSlot.concreteValueType, otherSlot.IsConnectionTestable())
                    : SlotValueHelper.AreCompatible(otherSlot.valueType, concreteValueType, IsConnectionTestable())));
        }

        public bool IsCompatibleStageWith(GeometrySlot otherSlot)
        {
            var startStage = otherSlot.stageCapability;
            if (startStage == GeometryStageCapability.All || otherSlot.owner is SubGraphNode)
                startStage = NodeUtils.GetEffectiveGeometryStageCapability(otherSlot, true)
                    & NodeUtils.GetEffectiveGeometryStageCapability(otherSlot, false);
            return IsCompatibleStageWith(startStage);
        }

        internal bool IsCompatibleStageWith(GeometryStageCapability capability)
        {
            return capability == GeometryStageCapability.All || stageCapability == GeometryStageCapability.All || stageCapability == capability;
        }

        public string GetDefaultValue(GenerationMode generationMode, ConcretePrecision concretePrecision)
        {
            string defaultValue = GetDefaultValue(generationMode);
            return defaultValue.Replace(PrecisionUtil.Token, concretePrecision.ToGeometryString());
        }

        public virtual int GetIntDefaultValue()
        {
            return 0;
        }
        public virtual float GetFloatDefaultValue()
        {
            return 0f;
        }
        public virtual Vector3 GetVector3DefaultValue()
        {
            return Vector3.zero;
        }

        public virtual string GetDefaultValue(GenerationMode generationMode)
        {
            var matOwner = owner as AbstractGeometryNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractGeometryNode)));

            if (generationMode.IsPreview() && matOwner.isActive)
                return matOwner.GetVariableNameForSlot(id);

            return ConcreteSlotValueAsVariable();
        }

        protected virtual string ConcreteSlotValueAsVariable()
        {
            return "error";
        }

        public abstract void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode);

        public virtual void GetPreviewProperties(List<PreviewProperty> properties, string name)
        {
            properties.Add(default(PreviewProperty));
        }

        public virtual void AppendHLSLParameterDeclaration(ShaderStringBuilder sb, string paramName)
        {
            sb.Append(concreteValueType.ToGeometryString());
            sb.Append(" ");
            sb.Append(paramName);
        }

        public abstract void CopyValuesFrom(GeometrySlot foundSlot);

        public bool Equals(GeometrySlot other)
        {
            return m_Id == other.m_Id && owner == other.owner;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((GeometrySlot)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (m_Id * 397) ^ (owner != null ? owner.GetHashCode() : 0);
            }
        }

        public void Dispose()
        {
            owner = null;
        }

        // this tracks old CustomFunctionNode slots that are expecting the old bare resource inputs
        // rather than the new structure-based inputs
        internal virtual bool bareResource { get { return false; } set { } }

        public virtual void CopyDefaultValue(GeometrySlot other)
        {
            m_Id = other.m_Id;
            m_DisplayName = other.m_DisplayName;
            m_SlotType = other.m_SlotType;
            m_Hidden = other.m_Hidden;
            m_GeometryOutputName = other.m_GeometryOutputName;
            m_StageCapability = other.m_StageCapability;
        }
    }
}
