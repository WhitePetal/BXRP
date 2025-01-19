using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BXRenderPipeline
{
	public static class RendererExtensions
	{
		public static bool isLOD0(this Renderer renderer)
		{
			var trans = renderer.transform;
			// renderer transform is root
			if (trans.root == trans)
			{
				if (!trans.TryGetComponent<LODGroup>(out LODGroup lod))
					return true;

				var lod0Renderers = lod.GetLODs()?[0].renderers;
				if (lod0Renderers != null && lod0Renderers.Contains(renderer))
					return true;

				return false;
			}

			var parent = trans;
			bool finalResult = true;
			while(parent.parent != null)
			{
				parent = parent.parent;
				if(parent.TryGetComponent<LODGroup>(out LODGroup lod))
				{
					var lods = lod.GetLODs();
					if (lods == null || lods.Length == 0) continue;

					var lod0Renderers = lods[0].renderers;
					if (lod0Renderers != null && lod0Renderers.Contains(renderer))
						return true;
					for(int i = 1; i < lods.Length; ++i)
					{
						var lodRenderers = lods[i].renderers;
						// 物体可以同时作为LOD0 和 其它LOD
						// 因此当期为其它LOD时不能直接返回，而是先记录结果
						if (lodRenderers != null && lodRenderers.Contains(renderer))
							finalResult = false;
					}
				}
			}

			return finalResult;
		}
	}
}
