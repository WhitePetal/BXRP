using BXGraphing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
	public class PreviewManager
	{
		private List<PreviewRenderData> m_RenderDatas = new List<PreviewRenderData>();

		public PreviewRenderData GetPreview(AbstractGeometryNode node)
		{
			return m_RenderDatas[node.tempId.index];
		}
	}

	public delegate void OnPreviewChanged();

	public class PreviewGeometryData
	{
		public INode node { get; set; }
		public Geometry geometry { get; set; }
		public string geometryString { get; set; }
		public bool hasError { get; set; }
	}

	public class PreviewRenderData
	{
		public PreviewGeometryData shaderData { get; set; }
		public RenderTexture renderTexture { get; set; }
		public Texture texture { get; set; }
		public PreviewMode previewMode { get; set; }
		public OnPreviewChanged onPreviewChanged;

		public void NotifyPreviewChanged()
		{
			if (onPreviewChanged != null)
				onPreviewChanged();
		}
	}
}
