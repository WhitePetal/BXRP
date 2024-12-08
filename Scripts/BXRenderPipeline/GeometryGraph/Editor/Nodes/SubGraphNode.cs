using BXGraphing;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BXGeometryGraph
{
	[Title("Utility", "Sub-graph")]
	public class SubGraphNode : AbstractGeometryNode,
		IGeneratesBodyCode,
		IOnAssetEnabled
	{
		[SerializeField]
		private string m_SerializedSubGraph = string.Empty;

		// TODO
//#if UNITY_EDITOR
//		[ObjectControl("")]
//		public GeometrySubGraphAsset subGraphAsset
//		{
//			get
//			{
//				if (string.IsNullOrEmpty(m_SerializedSubGraph))
//					return null;

//				var helper = new SubGraphHelper();
//				EditorJsonUtility.FromJsonOverwrite(m_SerializedSubGraph, helper);
//				return helper.subGraph;
//			}
//			set
//			{
//				if (subGraphAsset == value)
//					return;

//				var helper = new SubGraphHelper();
//				helper.subGraph = value;
//				m_SerializedSubGraph = EditorJsonUtility.ToJson(helper, true);
//				UpdateSlots();

//				Dirty(ModificationScope.Topological);
//			}
//		}
//#else
//        public MaterialSubGraphAsset subGraphAsset {get; set; }
//#endif

		[SerializeField]
		private class SubGraphHelper
		{
			//public GeometrySubGraphAsset subGraph;
		}

		public void GenerateNodeCode(GeometryGenerator visitor, GenerationMode generationMode)
		{
			throw new System.NotImplementedException();
		}

		public void OnEnable()
		{
			throw new System.NotImplementedException();
		}
	}
}
