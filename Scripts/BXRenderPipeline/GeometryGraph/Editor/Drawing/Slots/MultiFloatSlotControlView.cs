using System;
using System.Collections;
using System.Collections.Generic;
using BXGraphing;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    public class MultiFloatSlotControlView : VisualElement
    {
        private readonly INode m_Node;
        private readonly Func<Vector4> m_Get;
        private readonly Action<Vector4> m_Set;
        int m_UndoGroup = -1;

        public MultiFloatSlotControlView(INode node, string[] labels, Func<Vector4> get, Action<Vector4> set)
        {
            this.AddStyleSheetPath("Assets/Scripts/BXRenderPipeline/GeometryGraph/Editor/Resource/Styles/MultiFloatSlotControlView.uss");
            m_Node = node;
            m_Get = get;
            m_Set = set;
            var initalValue = get();
            for(var i = 0; i < labels.Length; ++i)
            {
                AddField(initalValue, i, labels[i]);
            }
        }

        private void AddField(Vector4 initialValue, int index, string subLabel)
        {
            var dummy = new VisualElement { name = "dummy" };
            var label = new Label(subLabel);
            dummy.Add(label);
            Add(dummy);
            var field = new FloatField { userData = index, value = initialValue[index] };
            var dragger = new FieldMouseDragger<double>(field as IValueField<double>);
            dragger.SetDragZone(label);
            field.RegisterValueChangedCallback(evt =>
            {
                var value = m_Get();
                value[index] = (float)evt.newValue;
                m_Set(value);
                m_Node.Dirty(ModificationScope.Node);
                m_UndoGroup = -1;
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
                var value = m_Get();
                if (Math.Abs(value[index] - newValue) > 1e-9)
                {
                    value[index] = newValue;
                    m_Set(value);
                    m_Node.Dirty(ModificationScope.Node);
                }
            });
            field.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Escape && m_UndoGroup > -1)
                {
                    Undo.RevertAllDownToGroup(m_UndoGroup);
                    m_UndoGroup = -1;
                    evt.StopPropagation();
                }
                this.MarkDirtyRepaint();
            });
            Add(field);
        }
    }
}
