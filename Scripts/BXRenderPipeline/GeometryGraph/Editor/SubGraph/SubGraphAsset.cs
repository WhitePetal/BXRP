using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace BXGeometryGraph
{
    [Serializable]
    struct FunctionPair
    {
        public string key;              // aka function name
        public string value;            // aka function code
        public int graphPrecisionFlags; // Flags<GraphPrecision> indicating which precision variants are requested by the subgraph

        public FunctionPair(string key, string value, int graphPrecisionFlags)
        {
            this.key = key;
            this.value = value;
            this.graphPrecisionFlags = graphPrecisionFlags;
        }
    }

    [Serializable]
    class SlotCapability
    {
        public string slotName;
        public GeometryStageCapability capabilities = GeometryStageCapability.All;
    }

    [Serializable]
    class SlotDependencyPair
    {
        public string inputSlotName;
        public string outputSlotName;
    }

    // Cached run-time information for slot dependency tracking within a sub-graph
    class SlotDependencyInfo
    {
        internal string slotName;
        internal GeometryStageCapability capabilities = GeometryStageCapability.All;
        internal HashSet<string> dependencies = new HashSet<string>();

        internal void AddDepencencySlotName(string slotName)
        {
            dependencies.Add(slotName);
        }

        internal bool ContainsSlot(GeometrySlot slot)
        {
            return dependencies.Contains(slot.RawDisplayName());
        }
    }

    class SubGraphData : JsonObject
    {
        public List<JsonData<AbstractGeometryProperty>> inputs = new List<JsonData<AbstractGeometryProperty>>();
        public List<JsonData<ShaderKeyword>> keywords = new List<JsonData<ShaderKeyword>>();
        public List<JsonData<GeometryDropdown>> dropdowns = new List<JsonData<GeometryDropdown>>();
        public List<JsonData<AbstractGeometryProperty>> nodeProperties = new List<JsonData<AbstractGeometryProperty>>();
        public List<JsonData<GeometrySlot>> outputs = new List<JsonData<GeometrySlot>>();
        public List<JsonData<Target>> unsupportedTargets = new List<JsonData<Target>>();
    }

    class SubGraphAsset : ScriptableObject, ISerializationCallbackReceiver
    {
        public bool isValid;

        public long processedAt;

        public string functionName;

        public string inputStructName;

        public string hlslName;

        public string assetGuid;

        public GeometryGraphRequirements requirements;

        public string path;

        public List<FunctionPair> functions = new List<FunctionPair>();

        public IncludeCollection includes;

        public List<string> vtFeedbackVariables = new List<string>();

        private SubGraphData m_SubGraphData;


        [HideInInspector]
        [SerializeField]
        private SerializationHelper.JSONSerializedElement m_SerializedSubGraphData;

        public DataValueEnumerable<AbstractGeometryProperty> inputs => m_SubGraphData.inputs.SelectValue();

        public DataValueEnumerable<ShaderKeyword> keywords => m_SubGraphData.keywords.SelectValue();

        public DataValueEnumerable<GeometryDropdown> dropdowns => m_SubGraphData.dropdowns.SelectValue();

        public DataValueEnumerable<AbstractGeometryProperty> nodeProperties => m_SubGraphData.nodeProperties.SelectValue();

        public DataValueEnumerable<GeometrySlot> outputs => m_SubGraphData.outputs.SelectValue();

        public DataValueEnumerable<Target> unsupportedTargets => m_SubGraphData.unsupportedTargets.SelectValue();

        public List<string> children = new List<string>(); // guids of direct USED SUBGRAPH file dependencies

        public List<string> descendents = new List<string>(); // guids of ALL file dependencies at any level, SHOULD LIST EVEN MISSING DESCENDENTS

        public List<SlotCapability> inputCapabilities = new List<SlotCapability>();
        public List<SlotCapability> outputCapabilities = new List<SlotCapability>();
        // Every unique input/output dependency pair
        public List<SlotDependencyPair> slotDependencies = new List<SlotDependencyPair>();

        Dictionary<string, SlotDependencyInfo> m_InputDependencies = new Dictionary<string, SlotDependencyInfo>();
        Dictionary<string, SlotDependencyInfo> m_OutputDependencies = new Dictionary<string, SlotDependencyInfo>();

        public SlotDependencyInfo GetInputDependencies(string slotName)
        {
            m_InputDependencies.TryGetValue(slotName, out SlotDependencyInfo result);
            return result;
        }

        public SlotDependencyInfo GetOutputDependencies(string slotName)
        {
            m_OutputDependencies.TryGetValue(slotName, out SlotDependencyInfo result);
            return result;
        }

        // this is the precision that the entire subgraph is set to (indicates whether the graph is hard-coded or switchable)
        public GraphPrecision subGraphGraphPrecision;

        // this is the precision of the subgraph outputs
        // NOTE: this may not be the same as subGraphGraphPrecision
        // for example, a graph could allow switching precisions for internal calculations,
        // but the output of the graph is always full float
        // NOTE: we don't currently have a way to select the graph precision for EACH output
        // there's a single shared precision for all of them
        public GraphPrecision outputGraphPrecision;

        public PreviewMode previewMode;

        public void WriteData(IEnumerable<AbstractGeometryProperty> inputs, IEnumerable<ShaderKeyword> keywords, IEnumerable<GeometryDropdown> dropdowns, IEnumerable<AbstractGeometryProperty> nodeProperties, IEnumerable<GeometrySlot> outputs, IEnumerable<Target> unsupportedTargets)
        {
            if (m_SubGraphData == null)
            {
                m_SubGraphData = new SubGraphData();
                m_SubGraphData.OverrideObjectId(assetGuid, "_subGraphData");
            }

            m_SubGraphData.inputs.Clear();
            m_SubGraphData.keywords.Clear();
            m_SubGraphData.dropdowns.Clear();
            m_SubGraphData.nodeProperties.Clear();
            m_SubGraphData.outputs.Clear();
            m_SubGraphData.unsupportedTargets.Clear();

            foreach (var input in inputs)
            {
                m_SubGraphData.inputs.Add(input);
            }

            foreach (var keyword in keywords)
            {
                m_SubGraphData.keywords.Add(keyword);
            }

            foreach (var dropdown in dropdowns)
            {
                m_SubGraphData.dropdowns.Add(dropdown);
            }

            foreach (var nodeProperty in nodeProperties)
            {
                m_SubGraphData.nodeProperties.Add(nodeProperty);
            }

            foreach (var output in outputs)
            {
                m_SubGraphData.outputs.Add(output);
            }

            foreach (var unsupportedTarget in unsupportedTargets)
            {
                m_SubGraphData.unsupportedTargets.Add(unsupportedTarget);
            }
            var json = MultiJson.Serialize(m_SubGraphData);
            m_SerializedSubGraphData = new SerializationHelper.JSONSerializedElement() { JSONnodeData = json };
            m_SubGraphData = null;
        }

        public void OnBeforeSerialize()
        {

        }

        public void OnAfterDeserialize()
        {

        }

        public void LoadGraphData()
        {
            m_SubGraphData = new SubGraphData();
            if (!String.IsNullOrEmpty(m_SerializedSubGraphData.JSONnodeData))
            {
                MultiJson.Deserialize(m_SubGraphData, m_SerializedSubGraphData.JSONnodeData);
            }
        }

        internal void LoadDependencyData()
        {
            m_InputDependencies.Clear();
            m_OutputDependencies.Clear();

            foreach(var capabilityInfo in inputCapabilities)
            {
                var dependencyInfo = new SlotDependencyInfo();
                dependencyInfo.slotName = capabilityInfo.slotName;
                dependencyInfo.capabilities = capabilityInfo.capabilities;
                if (m_InputDependencies.ContainsKey(dependencyInfo.slotName))
                {
                    Debug.LogWarning($"SubGraph '{hlslName}' has multiple input slots named '{dependencyInfo.slotName}', which is unsupported.  Please assign the input slots unique names.");
                    continue;
                }
                m_InputDependencies.Add(dependencyInfo.slotName, dependencyInfo);
            }
            foreach(var capabilityInfo in outputCapabilities)
            {
                var dependencyInfo = new SlotDependencyInfo();
                dependencyInfo.slotName = capabilityInfo.slotName;
                dependencyInfo.capabilities = capabilityInfo.capabilities;
                if (m_OutputDependencies.ContainsKey(dependencyInfo.slotName))
                {
                    Debug.LogWarning($"SubGraph '{hlslName}' has multiple output slots named '{dependencyInfo.slotName}', which is unsupported.  Please assign the output slots unique names.");
                    continue;
                }
                m_OutputDependencies.Add(dependencyInfo.slotName, dependencyInfo);
            }
            foreach(var slotDependency in slotDependencies)
            {
                // This shouldn't fail since every input/output must be in the above lists...
                if (m_InputDependencies.ContainsKey(slotDependency.inputSlotName))
                    m_InputDependencies[slotDependency.inputSlotName].AddDepencencySlotName(slotDependency.outputSlotName);
                if (m_OutputDependencies.ContainsKey(slotDependency.outputSlotName))
                    m_OutputDependencies[slotDependency.outputSlotName].AddDepencencySlotName(slotDependency.inputSlotName);
            }
        }
    }
}
