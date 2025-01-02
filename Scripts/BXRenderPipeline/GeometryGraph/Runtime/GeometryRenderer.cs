using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace BXGeometryGraph.Runtime
{
    [AddComponentMenu("BXGeometry/GeometryRenderer")]
    //[ExecuteAlways]
    public class GeometryRenderer : MonoBehaviour
    {
        [SerializeField]
        private GeometrySO sharedGeometrySO;

        [SerializeField]
        private Material material;

        internal GeometrySO m_GeometrySO;

        public GeometryData data;

        private JobHandle jobHandle;

        private bool init;

        public GeometrySO geometrySO
        {
            get
            {
                if (m_GeometrySO == sharedGeometrySO || m_GeometrySO == null)
                {
                    m_GeometrySO = Instantiate<GeometrySO>(sharedGeometrySO);
                    m_GeometrySO.ClearStates();
                }
                return m_GeometrySO;
            }
        }

        private void Awake()
        {
            m_GeometrySO = sharedGeometrySO;
            m_GeometrySO.Deserialize();
            data = new GeometryData();
            data.Init();
            init = true;
        }

        private void Update()
        {
            data.Clear();
            Schedule();
        }

        public void Render(CommandBuffer cmd)
        {
            if (!init) return;
            Compelete();
            Assert.IsNotNull(material, "GeometryRenderer's mat is null!");
            MeshData meshData = data.meshs[0];
            Mesh mesh = new Mesh();
            mesh.SetVertices(meshData.positions);
            mesh.SetIndices(meshData.corner_verts, MeshTopology.Quads, 0);
            cmd.DrawMesh(mesh, transform.localToWorldMatrix, material, 0, 0);
        }

        public void Schedule()
        {
            jobHandle = m_GeometrySO.data.ouputJob.Schedule(ref data);
        }

        public void Compelete()
        {
            jobHandle.Complete();
        }

        private void OnDestroy()
        {
            data.Dispose();
        }
    }
}