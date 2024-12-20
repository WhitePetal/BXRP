using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace BXGeometryGraph
{
    [Serializable]
    [Title("Input", "Property")]
    class PropertyNode : AbstractGeometryNode, IGeneratesShaderBodyCode, IOnAssetEnabled, IGeometryInputObserver
    {
        [SerializeField]
        private JsonRef<AbstractGeometryProperty> m_Property;

        public AbstractGeometryProperty property
        {
            get { return m_Property; }
            set
            {
                if (m_Property == value)
                    return;

                m_Property = value;
                AddOutputSlot();
                Dirty(ModificationScope.Topological);
            }
        }

        public PropertyNode()
        {
            name = "Property";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/WhitePetal/FloowDream/tree/main/Scripts/BXRenderPipeline/GeometryGraph/Editor/Resources/Documents/PropertyNode"; }
        }

        public override void UpdateNodeAfterDeserialization()
        {
            base.UpdateNodeAfterDeserialization();

            if (owner == null)
                return;

            if(property is Vector1GeometryProperty vector1GeometryProperty && vector1GeometryProperty.floatType == FloatType.Slider)
            {
                // Previously, the Slider vector1 property allowed the min value to be greater than the max
                // We no longer want to support that behavior so if such a property is encountered, swap the values
                if (vector1GeometryProperty.rangeValues.x > vector1GeometryProperty.rangeValues.y)
                {
                    vector1GeometryProperty.rangeValues = new Vector2(vector1GeometryProperty.rangeValues.y, vector1GeometryProperty.rangeValues.x);
                    Dirty(ModificationScope.Graph);
                }
            }
        }

        // this node's precision is always controlled by the property precision
        public override bool canSetPrecision => false;

        public void UpdateNodeDisplayName(string newDisplayName)
        {
            GeometrySlot foundSlot = FindSlot<GeometrySlot>(OutputSlotId);

            if (foundSlot != null)
                foundSlot.displayName = newDisplayName;
        }

        public void OnEnable()
        {
            AddOutputSlot();
        }

        public const int OutputSlotId = 0;

        void AddOutputSlot()
        {
            if (property is MultiJsonInternal.UnknownGeometryPropertyType uspt)
            {
                // keep existing slots, don't modify them
                return;
            }
            switch (property.concreteGeometryValueType)
            {
                //case ConcreteSlotValueType.Boolean:
                    //AddSlot(new BooleanMaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output, false));
                    //RemoveSlotsNameNotMatching(new[] { OutputSlotId });
                    //break;
                case ConcreteSlotValueType.Vector1:
                    AddSlot(new Vector1GeometrySlot(OutputSlotId, property.displayName, "Out", SlotType.Output, 0));
                    RemoveSlotsNameNotMatching(new[] { OutputSlotId });
                    break;
                case ConcreteSlotValueType.Vector2:
                    AddSlot(new Vector2GeometrySlot(OutputSlotId, property.displayName, "Out", SlotType.Output, Vector4.zero));
                    RemoveSlotsNameNotMatching(new[] { OutputSlotId });
                    break;
                case ConcreteSlotValueType.Vector3:
                    AddSlot(new Vector3GeometrySlot(OutputSlotId, property.displayName, "Out", SlotType.Output, Vector4.zero));
                    RemoveSlotsNameNotMatching(new[] { OutputSlotId });
                    break;
                //case ConcreteSlotValueType.Vector4:
                //    AddSlot(new Vector4GeometrySlot(OutputSlotId, property.displayName, "Out", SlotType.Output, Vector4.zero));
                //    RemoveSlotsNameNotMatching(new[] { OutputSlotId });
                //    break;
                //case ConcreteSlotValueType.Matrix2:
                //    AddSlot(new Matrix2MaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output));
                //    RemoveSlotsNameNotMatching(new[] { OutputSlotId });
                //    break;
                //case ConcreteSlotValueType.Matrix3:
                //    AddSlot(new Matrix3MaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output));
                //    RemoveSlotsNameNotMatching(new[] { OutputSlotId });
                //    break;
                //case ConcreteSlotValueType.Matrix4:
                //    AddSlot(new Matrix4MaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output));
                //    RemoveSlotsNameNotMatching(new[] { OutputSlotId });
                //    break;
                //case ConcreteSlotValueType.Texture2D:
                //    AddSlot(new Texture2DMaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output));
                //    RemoveSlotsNameNotMatching(new[] { OutputSlotId });
                //    break;
                //case ConcreteSlotValueType.Texture2DArray:
                //    AddSlot(new Texture2DArrayMaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output));
                //    RemoveSlotsNameNotMatching(new[] { OutputSlotId });
                //    break;
                //case ConcreteSlotValueType.Texture3D:
                //    AddSlot(new Texture3DMaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output));
                //    RemoveSlotsNameNotMatching(new[] { OutputSlotId });
                //    break;
                //case ConcreteSlotValueType.Cubemap:
                //    AddSlot(new CubemapMaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output));
                //    RemoveSlotsNameNotMatching(new[] { OutputSlotId });
                //    break;
                //case ConcreteSlotValueType.SamplerState:
                //    AddSlot(new SamplerStateMaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output));
                //    RemoveSlotsNameNotMatching(new[] { OutputSlotId });
                //    break;
                //case ConcreteSlotValueType.Gradient:
                //    AddSlot(new GradientMaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output));
                //    RemoveSlotsNameNotMatching(new[] { OutputSlotId });
                //    break;
                //case ConcreteSlotValueType.VirtualTexture:
                //    AddSlot(new VirtualTextureMaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output));
                //    RemoveSlotsNameNotMatching(new[] { OutputSlotId });
                //    break;
                case ConcreteSlotValueType.Geometry:
                    AddSlot(new GeometryGeometrySlot(OutputSlotId, property.displayName, "Out", SlotType.Output));
                    RemoveSlotsNameNotMatching(new[] { OutputSlotId });
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void GenerateNodeShaderCode(ShaderStringBuilder sb, GenerationMode mode)
        {
            // TODO
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            // TODO: we should switch VirtualTexture away from the macro-based variables and towards using the same approach as Texture2D
            switch (property.propertyType)
            {
                case PropertyType.VirtualTexture:
                    return property.GetHLSLVariableName(owner.isSubGraph, GenerationMode.ForReals);
            }

            return base.GetVariableNameForSlot(slotId);
        }

        public string GetConnectionStateVariableNameForSlot(int slotId)
        {
            return GeometryInput.GetConnectionStateVariableName(GetVariableNameForSlot(slotId));
        }

        protected override void CalculateNodeHasError()
        {
            if (property == null || !owner.properties.Any(x => x == property))
            {
                owner.AddConcretizationError(objectId, "Property Node has no associated Blackboard property.");
            }
            else if (property is MultiJsonInternal.UnknownGeometryPropertyType)
            {
                owner.AddValidationError(objectId, "Property is of unknown type, a package may be missing.", GeometryCompilerMessageSeverity.Warning);
            }
        }

        public override void UpdatePrecision(List<GeometrySlot> inputSlots)
        {
            // Get precision from Property
            if (property == null)
            {
                owner.AddConcretizationError(objectId, string.Format("No matching poperty found on owner for node {0}", objectId));
                hasError = true;
                return;
            }

            // this node's precision is always controlled by the property precision
            precision = property.precision;

            graphPrecision = precision.ToGraphPrecision(GraphPrecision.Graph);
            concretePrecision = graphPrecision.ToConcrete(owner.graphDefaultConcretePrecision);
        }

        public void OnGeometryInputUpdated(ModificationScope modificationScope)
        {
            if (modificationScope == ModificationScope.Layout)
                UpdateNodeDisplayName(property.displayName);
            Dirty(modificationScope);
        }
    }
}
