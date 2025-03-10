using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace BXRenderPipeline.OcclusionCulling
{
    public class OctreeNode
    {
        public AABB m_AABB;

        public OctreeNode[] m_Children;

#if UNITY_EDITOR
        public int m_Depth;
#endif

        public List<int> m_Masks;

        public OctreeNode(AABB aabb, int childCount = 0)
		{
            m_AABB = aabb;
            if(childCount > 0)
                m_Children = new OctreeNode[childCount];
		}
    }
}
