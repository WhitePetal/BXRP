using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    [AttributeUsage(AttributeTargets.Property)]
    public class IntegerControlAttribute : Attribute, IControlAttribute
    {
        private string m_Label;

        public IntegerControlAttribute(string label = null)
        {
            m_Label = label;
        }

        public VisualElement InstantiateControl(AbstractGeometryNode node, PropertyInfo propertyInfo)
        {
            return new IntegerControlView(m_Label, node, propertyInfo);
        }
    }

    public class IntegerControlView : VisualElement
    {
        private AbstractGeometryNode m_Node;
        private PropertyInfo m_PropertyInfo;

        public IntegerControlView(string label, AbstractGeometryNode node, PropertyInfo propertyInfo)
        {
            this.AddStyleSheetPath("Assets/Scripts/BXRenderPipeline/GeometryGraph/Editor/Resource/Styles/IntegerControlView.uss");
            m_Node = node;
            m_PropertyInfo = propertyInfo;
            if(propertyInfo.PropertyType != typeof(int))
                throw new ArgumentException("Property must be of type integer.", "propertyInfo");
            label = label ?? ObjectNames.NicifyVariableName(propertyInfo.Name);

            if (!string.IsNullOrEmpty(label))
                Add(new Label(label));

            var intField = new IntegerField { value = (int)m_PropertyInfo.GetValue(m_Node, null) };
            intField.RegisterValueChangedCallback(OnChange);

            Add(intField);
        }

        private void OnChange(ChangeEvent<int> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Integer Change");
            var newValue = evt.newValue;
            m_PropertyInfo.SetValue(m_Node, newValue, null);
            this.MarkDirtyRepaint();
        }
    }
}
