using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace BXGeometryGraph
{
	public class SearchWindowProvider : ScriptableObject, ISearchWindowProvider
	{
		public GeometryPort connectedPort { get; set; }

		public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
		{
			throw new System.NotImplementedException();
		}

		public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
		{
			throw new System.NotImplementedException();
		}
	}
}
