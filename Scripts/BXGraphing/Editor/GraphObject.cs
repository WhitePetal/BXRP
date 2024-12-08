using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BXGraphing
{
	public class GraphObject : ScriptableObject, IGraphObject, ISerializationCallbackReceiver
	{
		[SerializeField]
		private SerializationHelper.JSONSerializedElement m_SerializedGraph;

		[SerializeField]
		private bool m_IsDirty;

		private IGraph m_Graph;
		private IGraph m_DeserializedGraph;

		public IGraph graph
		{
			get { return m_Graph; }
			set
			{
				if (m_Graph != null)
					m_Graph.owner = null;
				m_Graph = value;
				if (m_Graph != null)
					m_Graph.owner = this;
			}
		}
		
		public bool isDirty
		{
			get { return m_IsDirty; }
			set { m_IsDirty = value; }
		}

		public void RegisterCompleteObjectUndo(string name)
		{
			Undo.RegisterCompleteObjectUndo(this, name);
			m_IsDirty = true;
		}

		public void OnBeforeSerialize()
		{
			if (graph != null)
				m_SerializedGraph = SerializationHelper.Serialize(graph);
		}

		public void OnAfterDeserialize()
		{
			var deserializedGraph = SerializationHelper.Deserialize<IGraph>(m_SerializedGraph, null);
			if (graph == null)
				graph = deserializedGraph;
			else
				m_DeserializedGraph = deserializedGraph;
		}

		private void Validate()
		{
			if(graph != null)
			{
				graph.OnEnable();
				graph.ValidateGraph();
			}
		}

		private void OnEnable()
		{
			Validate();

			Undo.undoRedoPerformed += UndoRedoPerformed;
			UndoRedoPerformed();
		}

		private void OnDisable()
		{
			Undo.undoRedoPerformed -= UndoRedoPerformed;
		}

		private void UndoRedoPerformed()
		{
			if(m_DeserializedGraph != null)
			{
				graph.ReplaceWith(m_DeserializedGraph);
				m_DeserializedGraph = null;
			}
		}
	}
}
