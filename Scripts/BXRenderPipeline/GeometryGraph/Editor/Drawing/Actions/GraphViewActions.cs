using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace BXGeometryGraph
{
    class ConvertToPropertyAction : IGraphDataAction
    {
        void ConvertToProperty(GraphData graphData)
        {
            Assert.IsNotNull(graphData, "GraphData is null while carrying out ConvertToPropertyAction");
            Assert.IsNotNull(inlinePropertiesToConvert, "InlinePropertiesToConvert is null while carrying out ConvertToPropertyAction");

            graphData.owner.RegisterCompleteObjectUndo("Convert to Property");

            var defaultCategory = graphData.categories.FirstOrDefault();
            Assert.IsNotNull(defaultCategory, "Default Category is null while carrying out ConvertToPropertyAction");

            foreach(var converter in inlinePropertiesToConvert)
            {
                var convertedProperty = converter.AsGeometryProperty();
                var node = converter as AbstractGeometryNode;

                graphData.AddGraphInput(convertedProperty);

                // Also insert this input into the default category
                if(defaultCategory != null)
                {
                    var addItemToCategoryAction = new AddItemToCategoryAction();
                    addItemToCategoryAction.categoryGuid = defaultCategory.categoryGuid;
                    addItemToCategoryAction.itemToAdd = convertedProperty;
                    graphData.owner.graphDataStore.Dispatch(addItemToCategoryAction);
                }

                // Add reference to converted property for use in responding to this action later
                convertedPropertyReferences.Add(convertedProperty);

                var propNode = new PropertyNode();
                propNode.drawState = node.drawState;
                propNode.group = node.group;
                graphData.AddNode(propNode);
                propNode.property = convertedProperty;

                var oldSlot = node.FindSlot<GeometrySlot>(converter.outputSlotID);
                var newSlot = node.FindSlot<GeometrySlot>(PropertyNode.OutputSlotId);

                foreach (var edge in graphData.GetEdges(oldSlot.slotReference))
                    graphData.Connect(newSlot.slotReference, edge.inputSlot);

                graphData.RemoveNode(node);
            }
        }

        public Action<GraphData> modifyGraphDataAction => ConvertToProperty;

        public IList<IPropertyFromNode> inlinePropertiesToConvert { get; set; } = new List<IPropertyFromNode>();

        public IList<AbstractGeometryProperty> convertedPropertyReferences { get; set; } = new List<AbstractGeometryProperty>();

        public Vector2 nodePsition { get; set; }
    }

    class ConvertToInlineAction : IGraphDataAction
    {
        void ConvertToInline(GraphData graphData)
        {
            Assert.IsNotNull(graphData, "GraphData is null while carrying out ConvertToInlineAction");
            Assert.IsNotNull(propertyNodesToConvert, "PropertyNodesToConvert is null while carrying out ConvertToInlineAction");
            graphData.owner.RegisterCompleteObjectUndo("Convert to Inline Node");

            foreach (var propertyNode in propertyNodesToConvert)
                graphData.ReplacePropertyNodeWithConcreteNode(propertyNode);
        }

        public Action<GraphData> modifyGraphDataAction => ConvertToInline;

        public IEnumerable<PropertyNode> propertyNodesToConvert { get; set; } = new List<PropertyNode>();
    }

    class DragGraphInputAction : IGraphDataAction
    {
        void DragGraphInput(GraphData graphData)
        {
            Assert.IsNotNull(graphData, "GraphData is null while carrying out DragGraphInputAction");
            Assert.IsNotNull(graphInputBeingDraggedIn, "GraphInputBeingDraggedIn is null while carrying out DragGraphInputAction");
            graphData.owner.RegisterCompleteObjectUndo("Drag Graph Input");

            switch (graphInputBeingDraggedIn)
            {
                case AbstractGeometryProperty property:
                {
                    if (property is MultiJsonInternal.UnknownGeometryPropertyType)
                        break;

                    // This could be from another graph, in which case we add a copy of the ShaderInput to this graph.
                    if (graphData.properties.FirstOrDefault(p => p == property) == null)
                    {
                        var copyShaderInputAction = new CopyGeometryInputAction();
                        copyShaderInputAction.geometryInputToCopy = property;
                        graphData.owner.graphDataStore.Dispatch(copyShaderInputAction);
                        property = (AbstractGeometryProperty)copyShaderInputAction.copiedGeometryInput;
                    }

                    var node = new PropertyNode();
                    var drawState = node.drawState;
                    drawState.position = new Rect(nodePosition, drawState.position.size);
                    node.drawState = drawState;
                    node.property = property; // this did come after, but it's not clear why.
                    graphData.AddNode(node);
                    break;
                }
                case ShaderKeyword keyword:
                {
                        // This could be from another graph, in which case we add a copy of the ShaderInput to this graph.
                        if (graphData.keywords.FirstOrDefault(k => k == keyword) == null)
                        {
                            var copyShaderInputAction = new CopyGeometryInputAction();
                            copyShaderInputAction.geometryInputToCopy = keyword;
                            graphData.owner.graphDataStore.Dispatch(copyShaderInputAction);
                            keyword = (ShaderKeyword)copyShaderInputAction.copiedGeometryInput;
                        }

                        var node = new KeywordNode();
                        var drawState = node.drawState;
                        drawState.position = new Rect(nodePosition, drawState.position.size);
                        node.drawState = drawState;
                        node.keyword = keyword;
                        graphData.AddNode(node);
                        break;
                }
                case GeometryDropdown dropdown:
                {
                    if (graphData.IsInputAllowedInGraph(dropdown))
                    {
                        // This could be from another graph, in which case we add a copy of the ShaderInput to this graph.
                        if (graphData.dropdowns.FirstOrDefault(d => d == dropdown) == null)
                        {
                            var copyShaderInputAction = new CopyGeometryInputAction();
                            copyShaderInputAction.geometryInputToCopy = dropdown;
                            graphData.owner.graphDataStore.Dispatch(copyShaderInputAction);
                            dropdown = (GeometryDropdown)copyShaderInputAction.copiedGeometryInput;
                        }

                        var node = new DropdownNode();
                        var drawState = node.drawState;
                        drawState.position = new Rect(nodePosition, drawState.position.size);
                        node.drawState = drawState;
                        node.dropdown = dropdown;
                        graphData.AddNode(node);
                    }
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public Action<GraphData> modifyGraphDataAction => DragGraphInput;

        public GeometryInput graphInputBeingDraggedIn { get; set; }

        public Vector2 nodePosition { get; set; }
    }
}
