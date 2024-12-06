using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BXGraphing;
using UnityEngine;

namespace BXGeometryGraph
{
    [Title("Input", "Property")]
    public class PropertyNode : AbstractGeometryNode, IGeneratesBodyCode, IOnAssetEnabled
    {
        private Guid m_PropertyGuid;

        [SerializeField]
        private string m_PropertyGuidSerialized;

        public const int OutputSlotId = 0;

        public PropertyNode()
        {
            name = "Property";
            UpdateNodeAfterDeserialization();
        }

        public Guid propertyGuid
        {
            get { return m_PropertyGuid; }
            set
            {
                if (m_PropertyGuid == value)
                    return;

                var graph = owner as AbstructGeometryGraph;
                var property = graph.properties.FirstOrDefault(x => x.guid == value);
                if (property == null)
                    return;
                m_PropertyGuid = value;

                UpdateNode();

                Dirty(ModificationScope.Topological);
            }
        }

        public override string documentationURL
        {
            get { return "https://github.com/WhitePetal/FloowDream/tree/main/Scripts/BXRenderPipeline/GeometryGraph/Editor/Resource/Documents/PropertyNode"; }
        }

        private void UpdateNode()
        {
            var graph = owner as AbstructGeometryGraph;
            var property = graph.properties.FirstOrDefault(x => x.guid == propertyGuid);
            if (property == null)
                return;

            if (property is Vector1GeometryProperty)
            {
                AddSlot(new Vector1GeometrySlot(OutputSlotId, property.displayName, "Out", SlotType.Output, 0));
                RemoveSlotsNameNotMatching(new[] { OutputSlotId });
            }
            else if (property is Vector2GeometryProperty)
            {
                AddSlot(new Vector2GeometrySlot(OutputSlotId, property.displayName, "Out", SlotType.Output, Vector4.zero));
                RemoveSlotsNameNotMatching(new[] { OutputSlotId });
            }
            else if (property is Vector3GeometryProperty)
            {
                AddSlot(new Vector3GeometrySlot(OutputSlotId, property.displayName, "Out", SlotType.Output, Vector4.zero));
                RemoveSlotsNameNotMatching(new[] { OutputSlotId });
            }
            else if (property is Vector4GeometryProperty)
            {
                AddSlot(new Vector4GoemetrySlot(OutputSlotId, property.displayName, "Out", SlotType.Output, Vector4.zero));
                RemoveSlotsNameNotMatching(new[] { OutputSlotId });
            }
            //else if (property is ColorShaderProperty)
            //{
            //    AddSlot(new Vector4MaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output, Vector4.zero));
            //    RemoveSlotsNameNotMatching(new[] { OutputSlotId });
            //}
            //else if (property is TextureGeometryProperty)
            //{
            //    AddSlot(new Texture2DMaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output));
            //    RemoveSlotsNameNotMatching(new[] { OutputSlotId });
            //}
            //else if (property is CubemapShaderProperty)
            //{
            //    AddSlot(new CubemapMaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output));
            //    RemoveSlotsNameNotMatching(new[] { OutputSlotId });
            //}
            //else if (property is BooleanShaderProperty)
            //{
            //    AddSlot(new BooleanMaterialSlot(OutputSlotId, property.displayName, "Out", SlotType.Output, false));
            //    RemoveSlotsNameNotMatching(new[] { OutputSlotId });
            //}
        }

        public void GenerateNodeCode(GeometryGenerator visitor, GenerationMode generationMode)
        {
            var graph = owner as AbstructGeometryGraph;
            var property = graph.properties.FirstOrDefault(x => x.guid == propertyGuid);
            if (property == null)
                return;

            if (property is Vector1GeometryProperty)
            {
                var result = string.Format("{0} {1} = {2};"
                        , precision
                        , GetVariableNameForSlot(OutputSlotId)
                        , property.referenceName);
                visitor.AddGeometryChunk(result, true);
            }
            else if (property is Vector2GeometryProperty)
            {
                var result = string.Format("{0}2 {1} = {2};"
                        , precision
                        , GetVariableNameForSlot(OutputSlotId)
                        , property.referenceName);
                visitor.AddGeometryChunk(result, true);
            }
            else if (property is Vector3GeometryProperty)
            {
                var result = string.Format("{0}3 {1} = {2};"
                        , precision
                        , GetVariableNameForSlot(OutputSlotId)
                        , property.referenceName);
                visitor.AddGeometryChunk(result, true);
            }
            else if (property is Vector4GeometryProperty)
            {
                var result = string.Format("{0}4 {1} = {2};"
                        , precision
                        , GetVariableNameForSlot(OutputSlotId)
                        , property.referenceName);
                visitor.AddGeometryChunk(result, true);
            }
            //else if (property is ColorShaderProperty)
            //{
            //    var result = string.Format("{0}4 {1} = {2};"
            //            , precision
            //            , GetVariableNameForSlot(OutputSlotId)
            //            , property.referenceName);
            //    visitor.AddGeometryChunk(result, true);
            //}
            //else if (property is BooleanShaderProperty)
            //{
            //    var result = string.Format("{0} {1} = {2};"
            //            , precision
            //            , GetVariableNameForSlot(OutputSlotId)
            //            , property.referenceName);
            //    visitor.AddGeometryChunk(result, true);
            //}
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            var graph = owner as AbstructGeometryGraph;
            var property = graph.properties.FirstOrDefault(x => x.guid == propertyGuid);

            //if (!(property is TextureGeometryProperty) && !(property is CubemapShaderProperty))
                //return base.GetVariableNameForSlot(slotId);

            return property.referenceName;
        }

        protected override bool CalculateNodeHasError()
        {
            var graph = owner as AbstructGeometryGraph;

            if (!graph.properties.Any(x => x.guid == propertyGuid))
                return true;

            return false;
        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            m_PropertyGuidSerialized = m_PropertyGuid.ToString();
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            if (!string.IsNullOrEmpty(m_PropertyGuidSerialized))
                m_PropertyGuid = new Guid(m_PropertyGuidSerialized);
        }

        public void OnEnable()
        {
            UpdateNode();
        }
    }
}
