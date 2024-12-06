using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BXGraphing;
using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    [System.Serializable]
    public abstract class GeometrySlot : ISlot
    {
        const string k_NotInit = "Not Initilaized";

        [SerializeField]
        private int m_Id;

        [SerializeField]
        private string m_DisplayName = k_NotInit;

        [SerializeField]
        private SlotType m_SlotType = SlotType.Input;

        [SerializeField]
        private int m_Priority = int.MaxValue;

        [SerializeField]
        private bool m_Hidden;

        [SerializeField]
        private string m_GeometryOutputName;

        //[SerializeField]
        //ShaderStage m_ShaderStage;

        private bool m_HasError;

        protected GeometrySlot()
        {

        }

        protected GeometrySlot(int slotId, string displayName, string geometryOutputName, SlotType slotType, bool hidden = false)
        {
            m_Id = slotId;
            m_DisplayName = displayName;
            m_SlotType = slotType;
            m_Hidden = hidden;
            m_GeometryOutputName = geometryOutputName;
        }

        protected GeometrySlot(int slotId, string displayName, string geometryOutputName, SlotType slotType, int priority, bool hidden = false)
        {
            m_Id = slotId;
            m_DisplayName = displayName;
            m_SlotType = slotType;
            m_Priority = priority;
            m_Hidden = hidden;
            m_GeometryOutputName = geometryOutputName;
        }

        public virtual VisualElement InstantiateControl()
        {
            return null;
        }

        public abstract SlotValueType valueType { get; }

        public abstract ConcreteSlotValueType concreteValueType { get; }

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
                    return "(T)";
                case ConcreteSlotValueType.Cubemap:
                    return "(C)";
                case ConcreteSlotValueType.Gradient:
                    return "(G)";
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

        public static GeometrySlot CreateGeometrySlot(SlotValueType type, int slotId, string displayName, string geometryOutputName, SlotType slotType, Vector4 defaultValue, bool hidden = false)
        {
            //switch (type)
            //{
            //case SlotValueType.SamplerState:
            //}
            throw new ArgumentOutOfRangeException("type", type, null);
        }

        public INode owner { get; set; }

        public SlotReference slotReference
        {
            get { return new SlotReference(owner.guid, m_Id); }
        }

        public bool hidden
        {
            get { return m_Hidden; }
            set { m_Hidden = value; }
        }

        public int id
        {
            get { return m_Id; }
        }

        public int priority
        {
            get { return m_Priority; }
            set { m_Priority = value; }
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

        public string geometryOutputName
        {
            get { return m_GeometryOutputName; }
            set { m_GeometryOutputName = value; }
        }

        //public ShaderStage shaderStage
        //{
        //    get { return m_ShaderStage; }
        //    set { m_ShaderStage = value; }
        //}

        public bool hasError
        {
            get { return m_HasError; }
            set { m_HasError = value; }
        }

        private bool IsCompatiblewWithInputSlotType(SlotValueType inputType)
        {
            //switch (inputType)
            //{

            //}
            throw new NotImplementedException("NotImplement IsCompatiblewWithInputSlotType");
            return false;
        }

        public bool IsCompatibleWith(GeometrySlot otherSlot)
        {
            return otherSlot != null
                && otherSlot.owner != owner
                && otherSlot.isInputSlot != isInputSlot
                && (isInputSlot
                    ? otherSlot.IsCompatiblewWithInputSlotType(valueType)
                    : IsCompatiblewWithInputSlotType(otherSlot.valueType));
        }

        protected virtual string ConcreteSlotValueAsVariable(AbstractGeometryNode.OutputPrecision precision)
        {
            return "error";
        }

        public virtual string GetDefaultValue(GenerationMode generationMode)
        {
            var geoOwner = owner as AbstractGeometryNode;

            if(geoOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractGeometryNode)));

            if(generationMode.IsPreview())
                return geoOwner.GetVariableNameForSlot(id);

            return ConcreteSlotValueAsVariable(geoOwner.precision);
        }

        public abstract void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode);

        protected static PropertyType ConvertConcreteSlotValueTypeToPropertyType(ConcreteSlotValueType slotValue)
        {
            switch (slotValue)
            {
                case ConcreteSlotValueType.Texture2D:
                    return PropertyType.Texture;
                case ConcreteSlotValueType.Cubemap:
                    return PropertyType.Cubemap;
                case ConcreteSlotValueType.Gradient:
                    return PropertyType.Gradient;
                case ConcreteSlotValueType.Boolean:
                    return PropertyType.Boolean;
                case ConcreteSlotValueType.Vector1:
                    return PropertyType.Vector1;
                case ConcreteSlotValueType.Vector2:
                    return PropertyType.Vector2;
                case ConcreteSlotValueType.Vector3:
                    return PropertyType.Vector3;
                case ConcreteSlotValueType.Vector4:
                    return PropertyType.Vector4;
                case ConcreteSlotValueType.Matrix2:
                    return PropertyType.Matrix2;
                case ConcreteSlotValueType.Matrix3:
                    return PropertyType.Matrix3;
                case ConcreteSlotValueType.Matrix4:
                    return PropertyType.Matrix4;
                case ConcreteSlotValueType.SamplerState:
                    return PropertyType.SamplerState;
                default:
                    return PropertyType.Vector4;
            }
        }

        public virtual void GetPreviewProperties(List<PreviewProperty> properties, string name)
        {
            properties.Add(default(PreviewProperty));
        }

        public abstract void CopyValueFrom(GeometrySlot foundSlot);

        private bool Equals(GeometrySlot other)
        {
            return m_Id == other.m_Id && owner.guid.Equals(other.owner.guid);
        }

        public bool Equals(ISlot other)
        {
            return Equals(other as object);
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
    }
}
