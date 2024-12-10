using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    sealed partial class GraphData : ISerializationCallbackReceiver
    {
        public static class GraphSetup
        {
            public static void SetupNode(AbstractGeometryNode node)
            {
                node.Setup();
            }

            public static void SetupGraph(GraphData graph)
            {
                GraphDataUtils.ApplyActionLeafFirst(graph, SetupNode);
            }
        }
    }
}
