using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BXGraphing;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SliderControlAttribute : Attribute, IControlAttribute
    {
        private string m_Label;
        private bool m_DisplayMinMax;

        public SliderControlAttribute(string label = null, bool displayMinMax = false)
        {
            m_Label = label;
            m_DisplayMinMax = displayMinMax;
        }

        public VisualElement InstantiateControl(AbstractGeometryNode node, PropertyInfo propertyInfo)
        {
            return new SliderControlView(m_Label, m_DisplayMinMax, node, propertyInfo);
        }
    }

    public class SliderControlView : VisualElement, INodeModificationListener
    {
        private AbstractGeometryNode m_Node;
        private PropertyInfo m_PropertyInfo;
        private bool m_DisplayMinMax;
        private int m_UndoGroup = -1;
        private Vector3 m_Value;

        private VisualElement m_SliderPanel;
        private Slider m_Slider;
        private FloatField m_SliderInput;

        public SliderControlView(string label, bool displayMinMax, AbstractGeometryNode node, PropertyInfo propertyInfo)
        {
            m_Node = node;
            m_PropertyInfo = propertyInfo;
            this.AddStyleSheetPath("Assets/Scripts/BXRenderPipeline/GeometryGraph/Editor/Resources/Styles/SliderControlView.uss");
            m_DisplayMinMax = displayMinMax;

            if(propertyInfo.PropertyType != typeof(Vector3))
                throw new ArgumentException("Property must be of type Vector3.", "propertyInfo");

            new GUIContent(label ?? ObjectNames.NicifyVariableName(propertyInfo.Name));
            m_Value = (Vector3)m_PropertyInfo.GetValue(m_Node, null);

            m_SliderPanel = new VisualElement { name = "SliderPanel" };
            if (!string.IsNullOrEmpty(label))
                m_SliderPanel.Add(new Label(label));
            m_Slider = new Slider(m_Value.y, m_Value.z);
            m_Slider.RegisterValueChangedCallback((s) => { OnChangeSlider(s.newValue); });
            m_Slider.value = m_Value.x;
            m_SliderPanel.Add(m_Slider);
            m_SliderInput = AddField(m_SliderPanel, "", 0, m_Value);
            Add(m_SliderPanel);

            if (m_DisplayMinMax)
            {
                var fieldsPanel = new VisualElement { name = "FieldsPanel" };
                AddField(fieldsPanel, "Min", 1, m_Value);
                AddField(fieldsPanel, "Max", 2, m_Value);
                Add(fieldsPanel);
            }
        }

        public void OnNodeModified(ModificationScope scope)
        {
            if (scope == ModificationScope.Graph)
            {
                this.MarkDirtyRepaint();
            }
        }

        private void OnChangeSlider(float newValue)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Slider Change");
            var value = (Vector3)m_PropertyInfo.GetValue(m_Node, null);
            value.x = newValue;
            m_PropertyInfo.SetValue(m_Node, value, null);
            if (m_SliderInput != null)
                m_SliderInput.value = newValue;
            this.MarkDirtyRepaint();
        }

        private FloatField AddField(VisualElement panel, string label, int index, Vector3 initValiue)
        {
            var field = new FloatField { userData = index, value = initValiue[index] };

            if(label != "")
            {
                var l = new Label(label);
                panel.Add(l);
                var dragger = new FieldMouseDragger<double>(field as IValueField<double>);
                dragger.SetDragZone(l);
            }

            field.RegisterCallback<MouseDownEvent>(Repaint);
            field.RegisterCallback<MouseMoveEvent>(Repaint);
            field.RegisterValueChangedCallback(evt =>
            {
                var value = (Vector3)m_PropertyInfo.GetValue(m_Node, null);
                value[index] = (float)evt.newValue;
                m_PropertyInfo.SetValue(m_Node, value, null);
                m_UndoGroup = -1;
                UpdateSlider(m_SliderPanel, index, value);
                this.MarkDirtyRepaint();
            });
            field.RegisterCallback<InputEvent>(evt =>
            {
                if (m_UndoGroup == -1)
                {
                    m_UndoGroup = Undo.GetCurrentGroup();
                    m_Node.owner.owner.RegisterCompleteObjectUndo("Change " + m_Node.name);
                }
                float newValue;
                if (!float.TryParse(evt.newData, out newValue))
                    newValue = 0f;
                var value = (Vector3)m_PropertyInfo.GetValue(m_Node, null);
                value[index] = newValue;
                m_PropertyInfo.SetValue(m_Node, value, null);
                this.MarkDirtyRepaint();
            });
            field.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Escape && m_UndoGroup > -1)
                {
                    Undo.RevertAllDownToGroup(m_UndoGroup);
                    m_UndoGroup = -1;
                    m_Value = (Vector3)m_PropertyInfo.GetValue(m_Node, null);
                    UpdateSlider(m_SliderPanel, index, m_Value);
                    evt.StopPropagation();
                }
                this.MarkDirtyRepaint();
            });
            panel.Add(field);
            return field;
        }

        private void UpdateSlider(VisualElement panel, int index, Vector3 value)
        {
            value.x = Mathf.Max(Mathf.Min(value.x, value.z), value.y);
            panel.Remove(m_Slider);
            m_Slider = new Slider(value.y, value.z);
            m_Slider.RegisterValueChangedCallback((s) => { OnChangeSlider(s.newValue); });
            m_Slider.lowValue = value.y;
            m_Slider.highValue = value.z;
            m_Slider.value = value.x;
            panel.Add(m_Slider);

            panel.Remove(m_SliderInput);
            if (index != 0)
                m_SliderInput = AddField(panel, "", 0, value);
            m_SliderInput.value = value.x;
            panel.Add(m_SliderInput);
        }

        private void Repaint<T>(MouseEventBase<T> evt) where T : MouseEventBase<T>, new()
        {
            evt.StopPropagation();
            this.MarkDirtyRepaint();
        }
    }
}