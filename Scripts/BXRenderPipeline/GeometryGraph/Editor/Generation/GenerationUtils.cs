using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BXGeometryGraph.Runtime;
using Unity.Mathematics;
using UnityEngine;

namespace BXGeometryGraph
{
    internal static class GenerationUtils
    {
        const string kErrorString = @"ERROR!";

        internal static List<FieldDescriptor> GetActiveFieldsFromConditionals(ConditionalField[] conditionalFields)
        {
            var fields = new List<FieldDescriptor>();
            if (conditionalFields != null)
            {
                foreach (ConditionalField conditionalField in conditionalFields)
                {
                    if (conditionalField.condition == true)
                    {
                        fields.Add(conditionalField.field);
                    }
                }
            }

            return fields;
        }

        //internal static void GenerateSubShaderTags(Target target, SubShaderDescriptor descriptor, ShaderStringBuilder builder)
        //{
        //    builder.AppendLine("Tags");
        //    using (builder.BlockScope())
        //    {
        //        // Pipeline tag
        //        if (!string.IsNullOrEmpty(descriptor.pipelineTag))
        //            builder.AppendLine($"\"RenderPipeline\"=\"{descriptor.pipelineTag}\"");
        //        else
        //            builder.AppendLine("// RenderPipeline: <None>");

        //        // Render Type
        //        if (!string.IsNullOrEmpty(descriptor.renderType))
        //            builder.AppendLine($"\"RenderType\"=\"{descriptor.renderType}\"");
        //        else
        //            builder.AppendLine("// RenderType: <None>");

        //        // Custom shader tags.
        //        if (!string.IsNullOrEmpty(descriptor.customTags))
        //            builder.AppendLine(descriptor.customTags);

        //        // Render Queue
        //        if (!string.IsNullOrEmpty(descriptor.renderQueue))
        //            builder.AppendLine($"\"Queue\"=\"{descriptor.renderQueue}\"");
        //        else
        //            builder.AppendLine("// Queue: <None>");

        //        // DisableBatching tag
        //        if (!string.IsNullOrEmpty(descriptor.disableBatchingTag))
        //            builder.AppendLine($"\"DisableBatching\"=\"{descriptor.disableBatchingTag}\"");
        //        else
        //            builder.AppendLine("// DisableBatching: <None>");

        //        // ShaderGraphShader tag (so we can tell what shadergraph built)
        //        builder.AppendLine("\"ShaderGraphShader\"=\"true\"");

        //        if (target is IHasMetadata metadata)
        //            builder.AppendLine($"\"ShaderGraphTargetId\"=\"{metadata.identifier}\"");

        //        // IgnoreProjector
        //        if (!string.IsNullOrEmpty(descriptor.IgnoreProjector))
        //            builder.AppendLine($"\"IgnoreProjector\"=\"{descriptor.IgnoreProjector}\"");

        //        // PreviewType
        //        if (!string.IsNullOrEmpty(descriptor.PreviewType))
        //            builder.AppendLine($"\"PreviewType\"=\"{descriptor.PreviewType}\"");

        //        // CanUseSpriteAtlas
        //        if (!string.IsNullOrEmpty(descriptor.CanUseSpriteAtlas))
        //            builder.AppendLine($"\"CanUseSpriteAtlas\"=\"{descriptor.CanUseSpriteAtlas}\"");
        //    }
        //}

        //static bool IsFieldActive(FieldDescriptor field, IActiveFields activeFields, bool isOptional)
        //{
        //    bool fieldActive = true;
        //    if (!activeFields.Contains(field) && isOptional)
        //        fieldActive = false; //if the field is optional and not inside of active fields
        //    return fieldActive;
        //}

        //internal static void GenerateShaderStruct(StructDescriptor shaderStruct, ActiveFields activeFields, bool humanReadable, out ShaderStringBuilder structBuilder)
        //{
        //    structBuilder = new ShaderStringBuilder(humanReadable: humanReadable);
        //    structBuilder.AppendLine($"struct {shaderStruct.name}");
        //    using (structBuilder.BlockSemicolonScope())
        //    {
        //        foreach (var activeField in GetActiveFieldsAndKeyword(shaderStruct, activeFields))
        //        {
        //            var subscript = activeField.field;
        //            var keywordIfDefs = activeField.keywordIfDefs;

        //            //if field is active:
        //            if (subscript.HasPreprocessor())
        //                structBuilder.AppendLine($"#if {subscript.preprocessor}");

        //            //if in permutation, add permutation ifdef
        //            if (!string.IsNullOrEmpty(keywordIfDefs))
        //                structBuilder.AppendLine(keywordIfDefs);

        //            //check for a semantic, build string if valid
        //            string semantic = subscript.HasSemantic() ? $" : {subscript.semantic}" : string.Empty;
        //            structBuilder.AppendLine($"{subscript.interpolation} {subscript.type} {subscript.name}{semantic};");

        //            //if in permutation, add permutation endif
        //            if (!string.IsNullOrEmpty(keywordIfDefs))
        //                structBuilder.AppendLine("#endif"); //TODO: add debug collector

        //            if (subscript.HasPreprocessor())
        //                structBuilder.AppendLine("#endif");
        //        }
        //    }
        //}

        internal static void GenerateSurfaceInputTransferCode(ShaderStringBuilder sb, GeometryGraphRequirements requirements, string structName, string variableName)
        {
            //sb.AppendLine($"{structName} {variableName};");

            //GenerateSpaceTranslationSurfaceInputs(requirements.requiresNormal, InterpolatorType.Normal, sb, $"{variableName}.{{0}} = IN.{{0}};");
            //GenerateSpaceTranslationSurfaceInputs(requirements.requiresTangent, InterpolatorType.Tangent, sb, $"{variableName}.{{0}} = IN.{{0}};");
            //GenerateSpaceTranslationSurfaceInputs(requirements.requiresBitangent, InterpolatorType.BiTangent, sb, $"{variableName}.{{0}} = IN.{{0}};");
            //GenerateSpaceTranslationSurfaceInputs(requirements.requiresViewDir, InterpolatorType.ViewDirection, sb, $"{variableName}.{{0}} = IN.{{0}};");
            //GenerateSpaceTranslationSurfaceInputs(requirements.requiresPosition, InterpolatorType.Position, sb, $"{variableName}.{{0}} = IN.{{0}};");
            //GenerateSpaceTranslationSurfaceInputs(requirements.requiresPositionPredisplacement, InterpolatorType.PositionPredisplacement, sb, $"{variableName}.{{0}} = IN.{{0}};");

            //if (requirements.requiresVertexColor)
            //    sb.AppendLine($"{variableName}.{ShaderGeneratorNames.VertexColor} = IN.{ShaderGeneratorNames.VertexColor};");

            //if (requirements.requiresScreenPosition)
            //    sb.AppendLine($"{variableName}.{ShaderGeneratorNames.ScreenPosition} = IN.{ShaderGeneratorNames.ScreenPosition};");

            //if (requirements.requiresNDCPosition)
            //    sb.AppendLine($"{variableName}.{ShaderGeneratorNames.NDCPosition} = IN.{ShaderGeneratorNames.NDCPosition};");

            //if (requirements.requiresPixelPosition)
            //    sb.AppendLine($"{variableName}.{ShaderGeneratorNames.PixelPosition} = IN.{ShaderGeneratorNames.PixelPosition};");

            //if (requirements.requiresFaceSign)
            //    sb.AppendLine($"{variableName}.{ShaderGeneratorNames.FaceSign} = IN.{ShaderGeneratorNames.FaceSign};");

            //foreach (var channel in requirements.requiresMeshUVs.Distinct())
            //    sb.AppendLine($"{variableName}.{channel.GetUVName()} = IN.{channel.GetUVName()};");

            //if (requirements.requiresTime)
            //{
            //    sb.AppendLine($"{variableName}.{ShaderGeneratorNames.TimeParameters} = IN.{ShaderGeneratorNames.TimeParameters};");
            //}

            //if (requirements.requiresVertexSkinning)
            //{
            //    sb.AppendLine($"{variableName}.{ShaderGeneratorNames.BoneIndices} = IN.{ShaderGeneratorNames.BoneIndices};");
            //    sb.AppendLine($"{variableName}.{ShaderGeneratorNames.BoneWeights} = IN.{ShaderGeneratorNames.BoneWeights};");
            //}

            //if (requirements.requiresVertexID)
            //{
            //    sb.AppendLine($"{variableName}.{ShaderGeneratorNames.VertexID} = IN.{ShaderGeneratorNames.VertexID};");
            //}

            //if (requirements.requiresInstanceID)
            //{
            //    sb.AppendLine($"{variableName}.{ShaderGeneratorNames.InstanceID} = IN.{ShaderGeneratorNames.InstanceID};");
            //}
        }

        struct PackedEntry
        {
            public struct Input
            {
                public FieldDescriptor field;
                public int startChannel;
                public int channelCount;
            }

            public Input[] inputFields;
            public FieldDescriptor packedField;
        }

        internal static string AdaptNodeOutput(AbstractGeometryNode node, int outputSlotId, ConcreteSlotValueType convertToType)
        {
            var outputSlot = node.FindOutputSlot<GeometrySlot>(outputSlotId);

            if (outputSlot == null)
                return kErrorString;

            var convertFromType = outputSlot.concreteValueType;
            var rawOutput = node.GetVariableNameForSlot(outputSlotId);
            if (convertFromType == convertToType)
                return rawOutput;

            switch (convertToType)
            {
                case ConcreteSlotValueType.Boolean:
                    switch (convertFromType)
                    {
                        case ConcreteSlotValueType.Vector1:
                            return string.Format("((bool) {0})", rawOutput);
                        case ConcreteSlotValueType.Vector2:
                        case ConcreteSlotValueType.Vector3:
                        case ConcreteSlotValueType.Vector4:
                            return string.Format("((bool) {0}.x)", rawOutput);
                        default:
                            return kErrorString;
                    }
                case ConcreteSlotValueType.Vector1:
                    if (convertFromType == ConcreteSlotValueType.Boolean)
                        return string.Format("(($precision) {0})", rawOutput);
                    else
                        return string.Format("({0}).x", rawOutput);
                case ConcreteSlotValueType.Vector2:
                    switch (convertFromType)
                    {
                        case ConcreteSlotValueType.Boolean:
                            return string.Format("((($precision) {0}).xx)", rawOutput);
                        case ConcreteSlotValueType.Vector1:
                            return string.Format("({0}.xx)", rawOutput);
                        case ConcreteSlotValueType.Vector3:
                        case ConcreteSlotValueType.Vector4:
                            return string.Format("({0}.xy)", rawOutput);
                        default:
                            return kErrorString;
                    }
                case ConcreteSlotValueType.Vector3:
                    switch (convertFromType)
                    {
                        case ConcreteSlotValueType.Boolean:
                            return string.Format("((($precision) {0}).xxx)", rawOutput);
                        case ConcreteSlotValueType.Vector1:
                            return string.Format("({0}.xxx)", rawOutput);
                        case ConcreteSlotValueType.Vector2:
                            return string.Format("($precision3({0}, 0.0))", rawOutput);
                        case ConcreteSlotValueType.Vector4:
                            return string.Format("({0}.xyz)", rawOutput);
                        default:
                            return kErrorString;
                    }
                case ConcreteSlotValueType.Vector4:
                    switch (convertFromType)
                    {
                        case ConcreteSlotValueType.Boolean:
                            return string.Format("((($precision) {0}).xxxx)", rawOutput);
                        case ConcreteSlotValueType.Vector1:
                            return string.Format("({0}.xxxx)", rawOutput);
                        case ConcreteSlotValueType.Vector2:
                            return string.Format("($precision4({0}, 0.0, 1.0))", rawOutput);
                        case ConcreteSlotValueType.Vector3:
                            return string.Format("($precision4({0}, 1.0))", rawOutput);
                        default:
                            return kErrorString;
                    }
                case ConcreteSlotValueType.Matrix3:
                    return rawOutput;
                case ConcreteSlotValueType.Matrix2:
                    return rawOutput;
                case ConcreteSlotValueType.PropertyConnectionState:
                    return node.GetConnnectionStateVariableNameForSlot(outputSlotId);
                default:
                    return kErrorString;
            }
        }

        private static void SetDepenedJobs(AbstractGeometryNode node, IEnumerable<IEdge> edges, int slotId, AbstractGeometryJob[] depenedJobs)
        {
            var outputNode = edges.First().outputSlot.node;
            while (outputNode is RedirectNodeData redirectNode)
            {
                //outputNode = redirectNode.
                // TODO
                throw new NotImplementedException("Not Implement GetSlotVector3DataForGeoJob while outputNode is RedirectNodeData");
            }
            if (outputNode is IGenerateGeometryJob generatorNode)
            {
                var containJobCollection = depenedJobs.Where(x => x != null && x.nodeGuid == node.objectId);
                var containJob = containJobCollection.Any() ? containJobCollection.First() : null;
                if (containJob == null)
                {
                    depenedJobs[slotId] = generatorNode.BuildGeometryJob();
                }
            }
            else
            {
                depenedJobs[slotId] = null;
            }
        }

        internal static (ValueFrom, int, int) GetSlotIntDataForGeoJob(AbstractGeometryNode node, int slotId, AbstractGeometryJob[] depenedJobs)
        {
            var slotRef = node.GetSlotReference(slotId);
            var edges = node.owner.GetEdges(slotRef);
            ValueFrom valueFrom;
            int valueId;
            int valueDefault;
            if (edges.Count() > 0)
            {
                valueFrom = ValueFrom.DepenedJob;
                valueId = edges.First().outputSlot.slot.id;
                valueDefault = default;

                SetDepenedJobs(node, edges, slotId, depenedJobs);
            }
            else
            {
                valueFrom = ValueFrom.Default;
                valueId = 0;
                valueDefault = slotRef.slot.GetIntDefaultValue();
                depenedJobs[slotId] = null;
            }
            return (valueFrom, valueId, valueDefault);
        }

        internal static (ValueFrom, int, float3) GetSlotVector3DataForGeoJob(AbstractGeometryNode node, int slotId, AbstractGeometryJob[] depenedJobs)
        {
            var slotRef = node.GetSlotReference(slotId);
            var edges = node.owner.GetEdges(slotRef);
            ValueFrom valueFrom;
            int valueId;
            float3 valueDefault;
            if (edges.Count() > 0)
            {
                valueFrom = ValueFrom.DepenedJob;
                valueId = edges.First().outputSlot.slot.id;
                valueDefault = default;

                SetDepenedJobs(node, edges, slotId, depenedJobs);
            }
            else
            {
                valueFrom = ValueFrom.Default;
                valueId = 0;
                valueDefault = slotRef.slot.GetVector3DefaultValue();
                depenedJobs[slotId] = null;
            }
            return (valueFrom, valueId, valueDefault);
        }

        internal static (ValueFrom, int) GetSlotGeometryDataForGeoJob(AbstractGeometryNode node, int slotId, AbstractGeometryJob[] depenedJobs)
        {
            var slotRef = node.GetSlotReference(slotId);
            var edges = node.owner.GetEdges(slotRef);
            ValueFrom valueFrom;
            int valueId;
            if (edges.Count() > 0)
            {
                valueFrom = ValueFrom.DepenedJob;
                valueId = edges.First().outputSlot.slot.id;

                SetDepenedJobs(node, edges, slotId, depenedJobs);
            }
            else
            {
                valueFrom = ValueFrom.Default;
                valueId = 0;
                depenedJobs[slotId] = null;
            }
            return (valueFrom, valueId);
        }

        internal static (ValueFrom, int, bool) GetSlotBooleanDataForGeoJob(AbstractGeometryNode node, int slotId, AbstractGeometryJob[] depenedJobs)
        {
            var slotRef = node.GetSlotReference(slotId);
            var edges = node.owner.GetEdges(slotRef);
            ValueFrom valueFrom;
            int valueId;
            bool valueDefault;
            if (edges.Count() > 0)
            {
                valueFrom = ValueFrom.DepenedJob;
                valueId = edges.First().outputSlot.slot.id;
                valueDefault = false;

                SetDepenedJobs(node, edges, slotId, depenedJobs);
            }
            else
            {
                valueFrom = ValueFrom.Default;
                valueId = 0;
                valueDefault = slotRef.slot.GetIntDefaultValue() == 1 ? true : false;
                depenedJobs[slotId] = null;
            }
            return (valueFrom, valueId, valueDefault);
        }
    }
}
