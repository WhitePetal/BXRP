using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BXGraphing;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Toggle = UnityEngine.UIElements.Toggle;

namespace BXGeometryGraph
{
    public class BlackboardFieldPropertyView : VisualElement
    {
        private readonly AbstructGeometryGraph m_Graph;

        private IGeometryProperty m_Property;
        private Toggle m_ExposedToogle;
        private TextField m_ReferenceNameField;

        private static Type s_ContextualMenuManipulator = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypesOrNothing()).FirstOrDefault(t => t.FullName == "UnityEngine.UIElements.ContextualMenuManipulator");

        private IManipulator m_ResetReferenceMenu;

        public BlackboardFieldPropertyView(AbstructGeometryGraph graph, IGeometryProperty property)
        {
            this.AddStyleSheetPath("Assets/Scripts/BXRenderPipeline/GeometryGraph/Editor/Resources/Styles/GeometryGraphBlackboard.uss");

            m_Graph = graph;
            m_Property = property;

            m_ExposedToogle = new Toggle();
            m_ExposedToogle.RegisterValueChangedCallback((evt) =>
            {
                property.generatePropertyBlock = m_ExposedToogle.value;
                DirtyNodes(ModificationScope.Graph);
            });
            m_ExposedToogle.value = property.generatePropertyBlock;
            AddRow("Exposed", m_ExposedToogle);

            m_ReferenceNameField = new TextField(512, false, false, ' ');
            m_ReferenceNameField.AddStyleSheetPath("Assets/Scripts/BXRenderPipeline/GeometryGraph/Editor/Resources/Styles/PropertyNameReferenceField.uss");
            AddRow("Reference", m_ReferenceNameField);
            m_ReferenceNameField.value = property.referenceName;
            m_ReferenceNameField.isDelayed = true;
            m_ReferenceNameField.RegisterValueChangedCallback((newName) =>
            {
                string newReferenceName = m_Graph.SanitizePropertyReferenceName(newName.newValue, property.guid);
                property.overrideReferenceName = newReferenceName;
                m_ReferenceNameField.value = property.referenceName;

                if (string.IsNullOrEmpty(property.overrideReferenceName))
                    m_ReferenceNameField.RemoveFromClassList("modified");
                else
                    m_ReferenceNameField.AddToClassList("modified");

                DirtyNodes(ModificationScope.Graph);
                UpdateReferenceNameResetMenu();
            });

            if (!string.IsNullOrEmpty(property.overrideReferenceName))
                m_ReferenceNameField.AddToClassList("modified");

            if(property is Vector1GeometryProperty)
            {
                VisualElement floatRow = new VisualElement();
                VisualElement intRow = new VisualElement();
                VisualElement modeRow = new VisualElement();
                VisualElement minRow = new VisualElement();
                VisualElement maxRow = new VisualElement();
                FloatField floatField = null;

                var floatProperty = (Vector1GeometryProperty)property;
                if (floatProperty.floatType == FloatType.Integer)
                {
                    var field = new IntegerField { value = (int)floatProperty.value };
                    field.RegisterValueChangedCallback(intEvt =>
                    {
                        floatProperty.value = (float)intEvt.newValue;
                        DirtyNodes();
                    });
                    intRow = AddRow("Default", field);
                }
                else
                {
                    floatField = new FloatField { value = floatProperty.value };
                    floatField.RegisterValueChangedCallback(evt =>
                    {
                        floatProperty.value = (float)evt.newValue;
                        DirtyNodes();
                    });
                    floatRow = AddRow("Default", floatField);
                }

                var floatModeField = new EnumField((Enum)floatProperty.floatType);
                floatModeField.value = floatProperty.floatType;
                floatModeField.RegisterValueChangedCallback(evt =>
                {
                    if (floatProperty.floatType == (FloatType)evt.newValue)
                        return;

                    floatProperty = (Vector1GeometryProperty)property;
                    floatProperty.floatType = (FloatType)evt.newValue;
                    switch (floatProperty.floatType)
                    {
                        case FloatType.Slider:
                            RemoveElements(new VisualElement[] { floatRow, intRow, modeRow, minRow, maxRow });
                            var field = new FloatField { value = Mathf.Max(Mathf.Min(floatProperty.value, floatProperty.rangeValues.y), floatProperty.rangeValues.x) };
                            floatProperty.value = (float)field.value;
                            field.RegisterValueChangedCallback(defaultEvt =>
                            {
                                floatProperty.value = Mathf.Max(Mathf.Min((float)defaultEvt.newValue, floatProperty.rangeValues.y), floatProperty.rangeValues.x);
                                field.value = floatProperty.value;
                                DirtyNodes();
                            });
                            floatRow = AddRow("Default", field);
                            field.value = Mathf.Max(Mathf.Min(floatProperty.value, floatProperty.rangeValues.y), floatProperty.rangeValues.x);
                            modeRow = AddRow("Mode", floatModeField);
                            var minFiled = new FloatField { value = floatProperty.rangeValues.x };
                            minFiled.RegisterValueChangedCallback(minEvt =>
                            {
                                floatProperty.rangeValues = new Vector2((float)minEvt.newValue, floatProperty.rangeValues.y);
                                floatProperty.value = Mathf.Max(Mathf.Min(floatProperty.value, floatProperty.rangeValues.y), floatProperty.rangeValues.x);
                                field.value = floatProperty.value;
                                DirtyNodes();
                            });
                            minRow = AddRow("Min", minFiled);
                            var maxField = new FloatField { value = floatProperty.rangeValues.y };
                            maxField.RegisterValueChangedCallback(maxEvt =>
                            {
                                floatProperty.rangeValues = new Vector2(floatProperty.rangeValues.x, (float)maxEvt.newValue);
                                floatProperty.value = Mathf.Max(Mathf.Min(floatProperty.value, floatProperty.rangeValues.y), floatProperty.rangeValues.x);
                                field.value = floatProperty.value;
                                DirtyNodes();
                            });
                            maxRow = AddRow("Max", maxField);
                            break;
                        case FloatType.Integer:
                            RemoveElements(new VisualElement[] { floatRow, intRow, modeRow, minRow, maxRow });
                            var intField = new IntegerField { value = (int)floatProperty.value };
                            intField.RegisterValueChangedCallback(intEvt =>
                            {
                                floatProperty.value = (float)intEvt.newValue;
                                DirtyNodes();
                            });
                            intRow = AddRow("Default", intField);
                            modeRow = AddRow("Mode", floatModeField);
                            break;
                        default:
                            RemoveElements(new VisualElement[] { floatRow, intRow, modeRow, minRow, maxRow });
                            field = new FloatField { value = floatProperty.value };
                            field.RegisterValueChangedCallback(defaultEvt =>
                            {
                                floatProperty.value = (float)defaultEvt.newValue;
                                DirtyNodes();
                            });
                            floatRow = AddRow("Default", field);
                            modeRow = AddRow("Mode", floatModeField);
                            break;
                    }
                    DirtyNodes();
                });
                modeRow = AddRow("Mode", floatModeField);

                if (floatProperty.floatType == FloatType.Slider)
                {
                    var minField = new FloatField { value = floatProperty.rangeValues.x };
                    minField.RegisterValueChangedCallback(minEvt =>
                    {
                        floatProperty.rangeValues = new Vector2((float)minEvt.newValue, floatProperty.rangeValues.y);
                        floatProperty.value = Mathf.Max(Mathf.Min(floatProperty.value, floatProperty.rangeValues.y), floatProperty.rangeValues.x);
                        floatField.value = floatProperty.value;
                        DirtyNodes();
                    });
                    minRow = AddRow("Min", minField);
                    var maxField = new FloatField { value = floatProperty.rangeValues.y };
                    maxField.RegisterValueChangedCallback(maxEvt =>
                    {
                        floatProperty.rangeValues = new Vector2(floatProperty.rangeValues.x, (float)maxEvt.newValue);
                        floatProperty.value = Mathf.Max(Mathf.Min(floatProperty.value, floatProperty.rangeValues.y), floatProperty.rangeValues.x);
                        floatField.value = floatProperty.value;
                        DirtyNodes();
                    });
                    maxRow = AddRow("Max", maxField);
                }
            }
            else if(property is Vector2GeometryProperty)
            {
                var vectorProperty = (Vector2GeometryProperty)property;
                var field = new Vector2Field { value = vectorProperty.value };
                field.RegisterValueChangedCallback(evt =>
                {
                    vectorProperty.value = evt.newValue;
                    DirtyNodes();
                });
                AddRow("Default", field);
            }
            else if(property is Vector3GeometryProperty)
            {
                var vectorProperty = (Vector3GeometryProperty)property;
                var field = new Vector3Field { value = vectorProperty.value };
                field.RegisterValueChangedCallback(evt =>
                {
                    vectorProperty.value = evt.newValue;
                    DirtyNodes();
                });
                AddRow("Default", field);
            }
            else if(property is Vector4GeometryProperty)
            {
                var vectorProperty = (Vector4GeometryProperty)property;
                var field = new Vector4Field { value = vectorProperty.value };
                field.RegisterValueChangedCallback(evt =>
                {
                    vectorProperty.value = evt.newValue;
                    DirtyNodes();
                });
                AddRow("Default", field);
            }
            //else if (property is ColorShaderProperty)
            //{
            //    var colorProperty = (ColorShaderProperty)property;
            //    var colorField = new ColorField { value = property.defaultValue, showEyeDropper = false, hdr = colorProperty.colorMode == ColorMode.HDR };
            //    colorField.OnValueChanged(evt =>
            //    {
            //        colorProperty.value = evt.newValue;
            //        DirtyNodes();
            //    });
            //    AddRow("Default", colorField);
            //    var colorModeField = new EnumField((Enum)colorProperty.colorMode);
            //    colorModeField.OnValueChanged(evt =>
            //    {
            //        if (colorProperty.colorMode == (ColorMode)evt.newValue)
            //            return;
            //        colorProperty.colorMode = (ColorMode)evt.newValue;
            //        colorField.hdr = colorProperty.colorMode == ColorMode.HDR;
            //        colorField.DoRepaint();
            //        DirtyNodes();
            //    });
            //    AddRow("Mode", colorModeField);
            //}
            else if(property is TextureGeometryProperty)
            {
                var textureProperty = (TextureGeometryProperty)property;
                var field = new ObjectField { value = textureProperty.value.texture, objectType = typeof(Texture) };
                field.RegisterValueChangedCallback(evt =>
                {
                    textureProperty.value.texture = (Texture)evt.newValue;
                    DirtyNodes();
                });
                AddRow("Default", field);
            }
            //else if (property is CubemapGeometryProperty)
            //{
            //    var cubemapProperty = (CubemapGeometryProperty)property;
            //    var field = new ObjectField { value = cubemapProperty.value.cubemap, objectType = typeof(Cubemap) };
            //    field.OnValueChanged(evt =>
            //    {
            //        cubemapProperty.value.cubemap = (Cubemap)evt.newValue;
            //        DirtyNodes();
            //    });
            //    AddRow("Default", field);
            //}
            //else if (property is BooleanGeometryProperty)
            //{
            //    var booleanProperty = (BooleanGeometryProperty)property;
            //    Action onBooleanChanged = () =>
            //    {
            //        booleanProperty.value = !booleanProperty.value;
            //        DirtyNodes();
            //    };
            //    var field = new Toggle(onBooleanChanged);
            //    field.SetValue(booleanProperty.value);
            //    AddRow("Default", field);
            //}
            //            AddRow("Type", new TextField());
            //            AddRow("Exposed", new Toggle(null));
            //            AddRow("Range", new Toggle(null));
            //            AddRow("Default", new TextField());
            //            AddRow("Tooltip", new TextField());

            AddToClassList("sgblackboardFieldPropertyView");
            UpdateReferenceNameResetMenu();
        }

        private void UpdateReferenceNameResetMenu()
        {
            if (string.IsNullOrEmpty(m_Property.overrideReferenceName))
            {
                this.RemoveManipulator(m_ResetReferenceMenu);
                m_ResetReferenceMenu = null;
            }
            else
            {
                m_ResetReferenceMenu = (IManipulator)Activator.CreateInstance(s_ContextualMenuManipulator, (Action<ContextualMenuPopulateEvent>)BuildContextualMenu);
                this.AddManipulator(m_ResetReferenceMenu);
            }
        }

        private void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Reset reference", e =>
            {
                m_Property.overrideReferenceName = null;
                m_ReferenceNameField.value = m_Property.referenceName;
                m_ReferenceNameField.RemoveFromClassList("modified");
                DirtyNodes(ModificationScope.Graph);
            }, DropdownMenuAction.Status.Normal);
        }

        private VisualElement AddRow(string labelText, VisualElement control)
        {
            VisualElement rowView = new VisualElement();

            rowView.AddToClassList("rowView");

            Label label = new Label(labelText);

            label.AddToClassList("rowViewLabel");
            rowView.Add(label);

            control.AddToClassList("rowViewControl");
            rowView.Add(control);

            Add(rowView);
            return rowView;
        }

        private void RemoveElements(VisualElement[] elements)
        {
            for(int i = 0; i < elements.Length; ++i)
            {
                if (elements[i].parent == this)
                    Remove(elements[i]);
            }
        }

        private void DirtyNodes(ModificationScope modificationScope = ModificationScope.Node)
        {
            foreach (var node in m_Graph.GetNodes<PropertyNode>())
                node.Dirty(modificationScope);
        }
    }
}
