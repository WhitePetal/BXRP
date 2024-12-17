using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    [Serializable]
    [NeverAllowedByTarget]
    class LegacyUnknownTypeNode : AbstractGeometryNode
    {
        public string serializedType;
        public string serializedData;

        [NonSerialized]
        public Type foundType = null;

        public LegacyUnknownTypeNode() : base()
        {
            serializedData = null;
            isValid = false;
        }

        public LegacyUnknownTypeNode(string typeData, string serializedNodeData) : base()
        {
            serializedType = typeData;
            serializedData = serializedNodeData;
            isValid = false;
        }

        public override void OnAfterDeserialize(string json)
        {
            base.OnAfterDeserialize(json);
        }

        public override void ValidateNode()
        {
            base.ValidateNode();
            owner.AddValidationError(objectId, "This node type could not be found. No function will be generated in the shader.", GeometryCompilerMessageSeverity.Warning);
        }
    }
}
