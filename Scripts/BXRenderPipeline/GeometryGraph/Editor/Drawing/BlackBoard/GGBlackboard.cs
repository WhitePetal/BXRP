using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    class BlackboardGroupInfo
    {
        [SerializeField]
        SerializableGuid m_Guid = new SerializableGuid();

        internal Guid guid => m_Guid.guid;

        [SerializeField]
        string m_GroupName;

        internal string GroupName
        {
            get => m_GroupName;
            set => m_GroupName = value;
        }

        BlackboardGroupInfo()
        {
        }
    }

    class GGBlackboard : IGGControlledElement<BlackboardController>
    {
        public BlackboardController controller => throw new NotImplementedException();

        GGController IGGControlledElement.controller => throw new NotImplementedException();

        public void OnControllerChanged(ref GGControllerChangedEvent e)
        {
            throw new NotImplementedException();
        }

        public void OnControllerEvent(GGControllerEvent e)
        {
            throw new NotImplementedException();
        }
    }
}
