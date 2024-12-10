using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    public class BlockNode : AbstractGeometryNode
    {
        [SerializeField]
        string m_SerializedDescriptor;

        [NonSerialized]
        private ContextData m_ContextData;

        [NonSerialized]
        private BlockFieldDescriptor m_Descriptor;

        public override string displayName
        {
            get
            {
                string displayName = "";
                if (m_Descriptor != null)
                {
                    //displayName = m_Descriptor.shaderStage.ToString();
                    //if (!string.IsNullOrEmpty(displayName))
                        //displayName += " ";
                    displayName += m_Descriptor.displayName;
                }

                return displayName;
            }
        }
    }
}
