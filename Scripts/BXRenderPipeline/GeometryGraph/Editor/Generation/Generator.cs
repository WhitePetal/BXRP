using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BXGeometryGraph.Runtime;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

namespace BXGeometryGraph
{
    class Generator
    {
        private GraphData m_GraphData;
        private AbstractGeometryNode m_OuputNode;
        private GenerationMode m_Mode;
        private string m_PrimaryGeoSOFullName;
        private AssetCollection m_AssetCollection;
        private bool m_HumanReadable;
        private BlockNode m_ActiveBlcok;
        private GeometrySO m_GeoSO;

        public GeometrySO geometrySO
        {
            get
            {
                return m_GeoSO;
            }
        }

        public Generator(GraphData graphData, AbstractGeometryNode outputNode, GenerationMode mode, string primaryGeoSOName, Target[] targets = null, AssetCollection assetCollection = null, bool humanReadable = false)
        {
            m_GraphData = graphData;
            m_OuputNode = outputNode;
            m_Mode = mode;

            if (!string.IsNullOrEmpty(graphData.path))
                m_PrimaryGeoSOFullName = graphData.path + "/" + primaryGeoSOName;
            else
                m_PrimaryGeoSOFullName = primaryGeoSOName;

            m_AssetCollection = assetCollection;
            m_HumanReadable = humanReadable;
            m_ActiveBlcok = m_GraphData.GetNodes<BlockNode>().ToList().FirstOrDefault();

            m_GeoSO = BuildGeometrySO();
        }

        private GeometrySO BuildGeometrySO()
        {
            GeometrySO so = ScriptableObject.CreateInstance<GeometrySO>();
            so.data = new GeometrySO.InnerData();

            bool ignoreActiveState = (m_Mode == GenerationMode.Preview);  // for previews, we ignore node active state
            so.data.ouputJob = BuildGeoJobFromBlockNode(m_ActiveBlcok);

            return so;
        }

        private AbstractGeometryJob BuildGeoJobFromBlockNode(BlockNode node)
        {
            // no where to start
            if (node == null)
                return null;

            AbstractGeometryJob job;
            AbstractGeometryJob[] depenedJobs = new AbstractGeometryJob[1];
            (ValueFrom geometryValueFrom, int geometryValueID) = GenerationUtils.GetSlotGeometryDataForGeoJob(node, 0, depenedJobs);
            job = new OutputJobManaged(node.objectId, geometryValueFrom, geometryValueID);
            job.depenedJobs = depenedJobs;

            return job;
        }
    }
}
