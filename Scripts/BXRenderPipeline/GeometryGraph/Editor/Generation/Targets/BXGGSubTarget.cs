using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    class BXGGSubTarget : SubTarget
    {
        internal override Type targetType => throw new NotImplementedException();

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            throw new NotImplementedException();
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            throw new NotImplementedException();
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<string> registerUndo)
        {
            throw new NotImplementedException();
        }

        public override bool IsActive()
        {
            throw new NotImplementedException();
        }

        public override void Setup(ref TargetSetupContext context)
        {
            throw new NotImplementedException();
        }
    }
}
