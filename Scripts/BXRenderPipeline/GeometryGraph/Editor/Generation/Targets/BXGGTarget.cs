using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace BXGeometryGraph
{
    sealed class BXGGTarget : Target
    {
        [SerializeField]
        JsonData<SubTarget> m_ActiveSubTarget;

        public BXGGTarget()
        {
            displayName = "BXGG";
            //m_SubTargets = TargetUtils.GetSubTargets(this);
            //m_SubTargetNames = m_SubTargets.Select(x => x.displayName).ToList();
            //TargetUtils.ProcessSubTargetList(ref m_ActiveSubTarget, ref m_SubTargets);
            //ProcessSubTargetDatas(m_ActiveSubTarget.value);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            context.AddBlock(BlockFields.GeometryDescription.Geometry);
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            var descs = context.blocks.Select(x => x.descriptor);
            // Core fields
            // Always force vertex as the shim between built-in cginc files and hlsl files requires this
            //context.AddField(Fields.GraphVertex);
            //context.AddField(Fields.GraphPixel);
            context.AddField(new FieldDescriptor("features", "graphGeometry", "FEATURES_GRAPH_GEOMETRY"));

            // SubTarget fields
            //m_ActiveSubTarget.value.GetFields(ref context);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<string> registerUndo)
        {
            return;
        }

        public override bool IsActive()
        {
            return true;
        }

        public override void Setup(ref TargetSetupContext context)
        {
            //// Setup the Target
            //context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);

            //// Setup the active SubTarget
            //TargetUtils.ProcessSubTargetList(ref m_ActiveSubTarget, ref m_SubTargets);
            //if (m_ActiveSubTarget.value == null)
            //    return;
            //m_ActiveSubTarget.value.target = this;
            //ProcessSubTargetDatas(m_ActiveSubTarget.value);
            //m_ActiveSubTarget.value.Setup(ref context);

            //// Override EditorGUI
            //if (!string.IsNullOrEmpty(m_CustomEditorGUI))
            //    context.AddCustomEditorForRenderPipeline(m_CustomEditorGUI, "");
        }

        public override bool WorksWithSRP(RenderPipelineAsset scriptableRenderPipeline)
        {
            return true;
        }
    }
}
