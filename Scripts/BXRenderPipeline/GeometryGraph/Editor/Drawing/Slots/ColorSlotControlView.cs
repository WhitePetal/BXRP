using System.Collections;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    class ColorSlotControlView : VisualElement
    {
        ColorGeometrySlot m_Slot;

        public ColorSlotControlView(ColorGeometrySlot slot)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/Controls/ColorSlotControlView"));
            m_Slot = slot;
            var colorField = new ColorField { value = slot.value, showEyeDropper = false };
            colorField.RegisterValueChangedCallback(OnValueChanged);
            Add(colorField);
        }

        void OnValueChanged(ChangeEvent<Color> evt)
        {
            m_Slot.owner.owner.owner.RegisterCompleteObjectUndo("Color Change");
            m_Slot.value = evt.newValue;
            m_Slot.owner.Dirty(ModificationScope.Node);
        }
    }
}
