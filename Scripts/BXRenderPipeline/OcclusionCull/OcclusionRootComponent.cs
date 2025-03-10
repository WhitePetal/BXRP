using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace BXRenderPipeline.OcclusionCulling
{
    public partial class OcclusionRootComponent : MonoBehaviour
    {
        public OcclusionCullStreamableAsset cellsAsset;

        private int m_TreeDepth;
        private AABB m_AABB;
        private float3 m_CellSize;
        private int m_CellCount;

        private NativeArray<bool> m_Tree;

#if UNITY_EDITOR
        private OctreeNode m_TreeRoot;
        private MeshGizmo cellMeshGizmos;
        private Queue<OctreeNode> m_Queue;
        private List<Collider> m_Colliders;
#endif

        private static readonly float3[] k_TreeNodeOffsets = new float3[]
        {
            new float3(0, 0, 0), new float3(1, 0, 0), new float3(1, 0, 1), new float3(0, 0, 1),
            new float3(0, 1, 0), new float3(1, 1, 0), new float3(1, 1, 1), new float3(0, 1, 1),
        };
        private static readonly Color[] k_DepthColors = new Color[]
        {
            Color.red, Color.yellow, Color.green, Color.cyan,
            Color.blue, Color.red, Color.yellow, Color.green
        };

        // Start is called before the first frame update
        void Start()
        {
            m_TreeDepth = 5;

            m_AABB = new AABB
            {
                Center = float3.zero,
                Extents = new float3(512f, 512f, 512f)
            };
            Debug.Log("AABB MAX: " + m_AABB.Max);
            Debug.Log("AABB MIN: " + m_AABB.Min);
            m_CellSize = m_AABB.Size /  math.ceilpow2(m_TreeDepth - 1);
            m_CellCount = ((int)math.pow(8, m_TreeDepth - 1) - 1) / 7;
            m_Tree = new NativeArray<bool>(m_CellCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            for(int i = 0; i < m_CellCount; ++i)
			{
                m_Tree[i] = true;
			}

            m_TreeRoot = new OctreeNode(m_AABB);
#if UNITY_EDITOR
            m_TreeRoot.m_Depth = 0;

            m_Queue = new Queue<OctreeNode>();
            m_Queue.Enqueue(m_TreeRoot);
            while(m_Queue.Count > 0)
			{
                OctreeNode parentNode = m_Queue.Dequeue();
                int k = parentNode.m_Depth;
                if (parentNode.m_Depth == m_TreeDepth - 1)
                    break;
                parentNode.m_Children = new OctreeNode[8];
                float3 parentExtent = parentNode.m_AABB.Extents;
                float3 parentMin = parentNode.m_AABB.Min;
                for(int i = 0; i < 8; ++i)
				{
                    AABB cAABB = new AABB();
                    float3 size = parentExtent;
                    cAABB.Extents = size * 0.5f;
                    cAABB.Center = k_TreeNodeOffsets[i] * size + parentMin + cAABB.Extents;
                    var child = new OctreeNode(cAABB);
                    child.m_Depth = k + 1;
                    parentNode.m_Children[i] = child;
                    m_Queue.Enqueue(child);
				}
            }
            m_Queue.Clear();

            m_Colliders = new List<Collider>(math.ceilpow2(transform.childCount));
            GetComponentsInChildren<Collider>(m_Colliders);
#endif
        }

		private void Update()
		{
            if (!baked)
			{
                if (baking)
                    BakeStep();
                return;
			}
            m_Queue.Enqueue(m_TreeRoot);
            Camera cam = Camera.main;
            OctreeNode finalNode = null;
            while (m_Queue.Count > 0)
            {
                OctreeNode node = m_Queue.Dequeue();
				if (node.m_AABB.Contains(cam.transform.position))
                {
                    finalNode = node;
                    if(node.m_Children != null)
					{
                        for (int i = 0; i < node.m_Children.Length; ++i)
                            m_Queue.Enqueue(node.m_Children[i]);
					}
				}
            }
            if(finalNode != null)
			{
                for(int i = 0; i < m_Colliders.Count; ++i)
				{
                    m_Colliders[i].gameObject.SetActive(false);
				}
                if(finalNode.m_Masks != null)
				{
                    for (int i = 0; i < finalNode.m_Masks.Count; ++i)
                    {
                        int k = finalNode.m_Masks[i];
                        m_Colliders[k].gameObject.SetActive(true);
                    }
                }
			}
            m_Queue.Clear();
        }

		private void OnDestroy()
		{
            m_Tree.Dispose();
		}

		private void OnDrawGizmos()
		{
#if UNITY_EDITOR
            if (m_CellCount <= 0 || m_Tree.Length <= 0)
                return;

            if (cellMeshGizmos == null)
                cellMeshGizmos = new MeshGizmo(m_CellCount);

            cellMeshGizmos.Clear();
            Queue<OctreeNode> nodeQueue = new Queue<OctreeNode>();
            nodeQueue.Enqueue(m_TreeRoot);
            while(nodeQueue.Count > 0)
			{
                var node = nodeQueue.Dequeue();
                cellMeshGizmos.AddWireCube(node.m_AABB.Center, node.m_AABB.Size, k_DepthColors[node.m_Depth]);
                if(node.m_Children != null)
				{
                    for(int i = 0; i < node.m_Children.Length; ++i)
					{
                        nodeQueue.Enqueue(node.m_Children[i]);
					}
				}
            }

            cellMeshGizmos.RenderWireframe(Matrix4x4.identity, gizmoName: "OcclusionCulling Tree Gizmo Rendering");
            cellMeshGizmos.Clear();
        }
#endif
    }
}
