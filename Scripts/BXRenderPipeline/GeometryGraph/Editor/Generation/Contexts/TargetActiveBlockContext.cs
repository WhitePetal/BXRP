using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    [GenerationAPI]
    public class TargetActiveBlockContext
    {
        public List<BlockFieldDescriptor> activeBlocks { get; private set; }
        public List<BlockFieldDescriptor> currentBlocks { get; private set; }
        public PassDescriptor? pass { get; private set; }

        public TargetActiveBlockContext(List<BlockFieldDescriptor> currentBlocks, PassDescriptor? pass)
        {
            activeBlocks = new List<BlockFieldDescriptor>();
            this.currentBlocks = currentBlocks;
            this.pass = pass;
        }

        public void AddBlock(BlockFieldDescriptor block, bool conditional = true)
        {
            if (conditional == true)
            {
                activeBlocks.Add(block);
            }
        }
    }
}
