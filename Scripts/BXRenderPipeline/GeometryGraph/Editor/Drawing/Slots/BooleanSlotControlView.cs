using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    internal class BooleanSlotControlView : VisualElement
    {
        private BooleanGeometrySlot m_Slot;

        public BooleanSlotControlView(BooleanGeometrySlot slot)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/Controls/BooleanSlotControlView"));
            m_Slot = slot;
            var toogleField = new Toggle() { value = m_Slot.value };
            toogleField.OnToggleChanged(OnChangeToggle);
            Add(toogleField);
        }

        void OnChangeToggle(ChangeEvent<bool> evt)
        {
            if (evt.newValue != m_Slot.value)
            {
                m_Slot.owner.owner.owner.RegisterCompleteObjectUndo("Toggle Change");
                m_Slot.value = evt.newValue;
                m_Slot.owner.Dirty(ModificationScope.Node);
            }
        }
    }
}
