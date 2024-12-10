using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using BXGraphing;
using UnityEngine;

namespace BXGeometryGraph
{
    public class SlotConfigurationException : Exception
    {
        public SlotConfigurationException(string message) : base(message)
        {

        }
    }

    internal static class NodeUtils
    {
        public static IEnumerable<T> GetSlots<T>(this AbstractGeometryNode node) where T : GeometrySlot
        {
            var slots = new List<T>();
            node.GetSlots(slots);
            return slots;
        }

        public static IEnumerable<T> GetInputSlots<T>(this AbstractGeometryNode node) where T : GeometrySlot
        {
            var slots = new List<T>();
            node.GetInputSlots(slots);
            return slots;
        }

        public static IEnumerable<T> GetInputSlots<T>(this AbstractGeometryNode node, GeometrySlot startingSlot) where T : GeometrySlot
        {
            var slots = new List<T>();
            node.GetInputSlots(startingSlot, slots);
            return slots;
        }

        public static IEnumerable<T> GetOutputSlots<T>(this AbstractGeometryNode node) where T : GeometrySlot
        {
            var slots = new List<T>();
            node.GetOutputSlots(slots);
            return slots;
        }

        public static IEnumerable<T> GetOutputSlots<T>(this AbstractGeometryNode node, GeometrySlot startingSlot) where T : GeometrySlot
        {
            var slots = new List<T>();
            node.GetOutputSlots(startingSlot, slots);
            return slots;
        }

        public static void SlotConfigurationExceptionIfBadConfiguration(INode node, IEnumerable<int> expectedInputSlots, IEnumerable<int> expectedOutputSlots)
        {
            var missingSlots = new List<int>();

            var inputSlots = expectedInputSlots as IList<int> ?? expectedInputSlots.ToList();
            missingSlots.AddRange(inputSlots.Except(node.GetInputSlots<ISlot>().Select(x => x.id)));

            var outputSlots = expectedOutputSlots as IList<int> ?? expectedOutputSlots.ToList();
            missingSlots.AddRange(outputSlots.Except(node.GetOutputSlots<ISlot>().Select(x => x.id)));

            if (missingSlots.Count == 0)
                return;

            var toPrint = missingSlots.Select(x => x.ToString());

            throw new SlotConfigurationException(string.Format("Missing slots {0} on node {1}", string.Join(", ", toPrint.ToArray()), node));
        }

        public static IEnumerable<IEdge> GetAllEdges(INode node)
        {
            var result = new List<IEdge>();
            var validSlots = ListPool<ISlot>.Get();

            validSlots.AddRange(node.GetInputSlots<ISlot>());
            for(int index = 0; index < validSlots.Count; ++index)
            {
                var inputSlot = validSlots[index];
                result.AddRange(node.owner.GetEdges(inputSlot.slotReference));
            }

            validSlots.Clear();
            validSlots.AddRange(node.GetOutputSlots<ISlot>());
            for(int index = 0; index < validSlots.Count; ++index)
            {
                var outputSlot = validSlots[index];
                result.AddRange(node.owner.GetEdges(outputSlot.slotReference));
            }

            ListPool<ISlot>.Release(validSlots);
            return result;
        }

        // CollectNodesNodeFeedsInto looks at the current node and calculates
        // which child nodes it depends on for it's calculation.
        // Results are returned depth first so by processing each node in
        // order you can generate a valid code block.
        public enum IncludeSelf
        {
            Include,
            Exclude
        }

        public static void DepthFirstCollectNodesFromNode(List<INode> nodeList, INode node, IncludeSelf includeSelf = IncludeSelf.Include, List<int> slotIds = null)
        {
            if (node == null)
                return;

            if (nodeList.Contains(node))
                return;

            var ids = node.GetInputSlots<ISlot>().Select(x => x.id);
            if (slotIds != null)
                ids = node.GetInputSlots<ISlot>().Where(x => slotIds.Contains(x.id)).Select(x => x.id);

            foreach(var slot in ids)
            {
                foreach(var edge in node.owner.GetEdges(node.GetSlotReference(slot)))
                {
                    var outputNode = node.owner.GetNodeFromGuid(edge.outputSlot.nodeGuid);
                    DepthFirstCollectNodesFromNode(nodeList, outputNode);
                }
            }

            if (includeSelf == IncludeSelf.Include)
                nodeList.Add(node);
        }

        public static void CollectNodesNodeFeedsInto(List<INode> nodeList, INode node, IncludeSelf includeSelf = IncludeSelf.Include)
        {
            if (node == null)
                return;

            if (nodeList.Contains(node))
                return;

            foreach(var slot in node.GetOutputSlots<ISlot>())
            {
                foreach(var edge in node.owner.GetEdges(slot.slotReference))
                {
                    var inputNode = node.owner.GetNodeFromGuid(edge.inputSlot.nodeGuid);
                    CollectNodesNodeFeedsInto(nodeList, inputNode);
                }
            }

            if (includeSelf == IncludeSelf.Include)
                nodeList.Add(node);
        }

        public static string GetHLSLSafeName(string input)
        {
            char[] arr = input.ToCharArray();
            arr = Array.FindAll<char>(arr, (c => (Char.IsLetterOrDigit(c))));
            return new string(arr);
        }

        public static string FloatToGeometryValue(float value)
        {
            if (Single.IsPositiveInfinity(value))
                return "1.#INF";
            else if (Single.IsNegativeInfinity(value))
                return "-1#INF";
            else if (Single.IsNaN(value))
                return "NAN";
            return value.ToString(CultureInfo.InvariantCulture);
        }

		public static string ConvertConcreteSlotValueTypeToString(AbstractGeometryNode.OutputPrecision p, ConcreteSlotValueType slotValue)
		{
			switch (slotValue)
			{
				case ConcreteSlotValueType.Boolean:
					return p.ToString();
				case ConcreteSlotValueType.Vector1:
					return p.ToString();
				case ConcreteSlotValueType.Vector2:
					return p + "2";
				case ConcreteSlotValueType.Vector3:
					return p + "3";
				case ConcreteSlotValueType.Vector4:
					return p + "4";
				case ConcreteSlotValueType.Texture2D:
					return "Texture2D";
				case ConcreteSlotValueType.Cubemap:
					return "Cubemap";
				case ConcreteSlotValueType.Gradient:
					return "Gradient";
				case ConcreteSlotValueType.Matrix2:
					return p + "2x2";
				case ConcreteSlotValueType.Matrix3:
					return p + "3x3";
				case ConcreteSlotValueType.Matrix4:
					return p + "4x4";
				case ConcreteSlotValueType.SamplerState:
					return "SamplerState";
				default:
					return "Error";
			}
		}

        public static string ConvertToValidHLSLIdentifier(string originalId /**, Func<string, bool> isDisallowedIdentifier = null **/)
        {
            // Converts "  1   var  * q-30 ( 0 ) (1)   " to "_1_var_q_30_0_1"
            if (originalId == null)
                originalId = "";

            var result = Regex.Replace(originalId, @"^[^A-Za-z0-9_]+|[^A-Za-z0-9_]+$", ""); // trim leading/trailing bad characters (excl '_').
            result = Regex.Replace(result, @"[^A-Za-z0-9]+", "_"); // replace sequences of bad characters with underscores (incl '_').

            for (int i = 0; result.Length == 0 || Char.IsDigit(result[0]) /** || IsHLSLKeyword(result) || (isDisallowedIdentifier?.Invoke(result) ?? false) **/;)
            {
                if (result.StartsWith("_"))
                    result += $"_{++i}";
                else
                    result = "_" + result;
            }
            return result;
        }

        //Go to the leaves of the node, then get all trees with those leaves
        private static HashSet<AbstractGeometryNode> GetForest(AbstractGeometryNode node)
        {
            var initial = new HashSet<AbstractGeometryNode> { node };

            var upstream = new HashSet<AbstractGeometryNode>();
            PreviewManager.PropagateNodes(initial, PreviewManager.PropagationDirection.Upstream, upstream);

            var forest = new HashSet<AbstractGeometryNode>();
            PreviewManager.PropagateNodes(upstream, PreviewManager.PropagationDirection.Downstream, forest);

            return forest;
        }

        internal static List<AbstractGeometryNode> GetParentNodes(AbstractGeometryNode node)
        {
            List<AbstractGeometryNode> nodeList = new List<AbstractGeometryNode>();
            var ids = node.GetInputSlots<GeometrySlot>().Select(x => x.id);
            foreach (var slot in ids)
            {
                if (node.owner == null)
                    break;
                foreach (var edge in node.owner.GetEdges(node.FindSlot<GeometrySlot>(slot).slotReference))
                {
                    var outputNode = ((Edge)edge).outputSlot.node;
                    if (outputNode != null)
                    {
                        nodeList.Add(outputNode);
                    }
                }
            }
            return nodeList;
        }

        private static bool ActiveLeafExists(AbstractGeometryNode node)
        {
            //if our active state has been explicitly set to a value use it
            switch (node.activeState)
            {
                case AbstractGeometryNode.ActiveState.Implicit:
                    break;
                case AbstractGeometryNode.ActiveState.ExplicitInactive:
                    return false;
                case AbstractGeometryNode.ActiveState.ExplicitActive:
                    return true;
            }


            List<AbstractGeometryNode> parentNodes = GetParentNodes(node);
            //at this point we know we are not explicitly set to a state,
            //so there is no reason to be inactive
            if (parentNodes.Count == 0)
            {
                return true;
            }

            bool output = false;
            foreach (var parent in parentNodes)
            {
                output |= ActiveLeafExists(parent);
                if (output)
                {
                    break;
                }
            }
            return output;
        }

        private static List<AbstractGeometryNode> GetChildNodes(AbstractGeometryNode node)
        {
            List<AbstractGeometryNode> nodeList = new List<AbstractGeometryNode>();
            var slots = node.GetOutputSlots<GeometrySlot>();
            var edges = new List<IEdge>();
            foreach (var slot in slots)
            {
                node.owner.GetEdges(slot, edges);
                foreach (var edge in edges)
                {
                    var inputNode = ((Edge)edge).inputSlot.node;
                    if (inputNode != null)
                    {
                        nodeList.Add(inputNode);
                    }
                }
                edges.Clear();
            }
            return nodeList;
        }

        private static bool ActiveRootExists(AbstractGeometryNode node)
        {
            //if our active state has been explicitly set to a value use it
            switch (node.activeState)
            {
                case AbstractGeometryNode.ActiveState.Implicit:
                    break;
                case AbstractGeometryNode.ActiveState.ExplicitInactive:
                    return false;
                case AbstractGeometryNode.ActiveState.ExplicitActive:
                    return true;
            }

            List<AbstractGeometryNode> childNodes = GetChildNodes(node);
            //at this point we know we are not explicitly set to a state,
            //so there is no reason to be inactive
            if (childNodes.Count == 0)
            {
                return true;
            }

            bool output = false;
            foreach (var child in childNodes)
            {
                output |= ActiveRootExists(child);
                if (output)
                {
                    break;
                }
            }
            return output;
        }

        private static void ActiveTreeExists(AbstractGeometryNode node, out bool activeLeaf, out bool activeRoot, out bool activeTree)
        {
            activeLeaf = ActiveLeafExists(node);
            activeRoot = ActiveRootExists(node);
            activeTree = activeRoot && activeLeaf;
        }

        //First pass check if node is now active after a change, so just check if there is a valid "tree" : a valid upstream input path,
        // and a valid downstream output path, or "leaf" and "root". If this changes the node's active state, then anything connected may
        // change as well, so update the "forrest" or all connectected trees of this nodes leaves.
        // NOTE: I cannot think if there is any case where the entirety of the connected graph would need to change, but if there are bugs
        // on certain nodes farther away from the node not updating correctly, a possible solution may be to get the entirety of the connected
        // graph instead of just what I have declared as the "local" connected graph
        public static void ReevaluateActivityOfConnectedNodes(AbstractGeometryNode node, PooledHashSet<AbstractGeometryNode> changedNodes = null)
        {
            var forest = GetForest(node);
            ReevaluateActivityOfNodeList(forest, changedNodes);
        }

        public static void ReevaluateActivityOfNodeList(IEnumerable<AbstractGeometryNode> nodes, PooledHashSet<AbstractGeometryNode> changedNodes = null)
        {
            bool getChangedNodes = changedNodes != null;
            foreach (AbstractGeometryNode n in nodes)
            {
                if (n.activeState != AbstractGeometryNode.ActiveState.Implicit)
                    continue;
                ActiveTreeExists(n, out _, out _, out bool at);
                if (n.isActive != at && getChangedNodes)
                {
                    changedNodes.Add(n);
                }
                n.SetActive(at, false);
            }
        }
    }
}
