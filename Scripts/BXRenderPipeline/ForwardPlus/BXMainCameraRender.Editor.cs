#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using BXRenderPipeline;

namespace BXRenderPipelineForward
{
    public partial class BXMainCameraRender
    {
        private string SampleName { get; set; }

        private void PreparBuffer()
		{
            Profiler.BeginSample("Editor Only");
            commandBuffer.name = SampleName = camera.name;
            Profiler.EndSample();
		}

        private void PreparForSceneWindow()
		{
            if(camera.cameraType == CameraType.SceneView)
			{
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
			}
		}

        private void DrawUnsupportShader()
        {
            DrawingSettings drawingSettings = new DrawingSettings(BXRenderPipeline.BXRenderPipeline.forwardShaderTagIds[0], new SortingSettings(camera))
            {
                overrideMaterial = material_error
            };
            FilteringSettings filteringSettings = FilteringSettings.defaultValue;
            for (int i = 1; i < BXRenderPipeline.BXRenderPipeline.legacyShaderTagIds.Length; ++i)
            {
                drawingSettings.SetShaderPassName(i, BXRenderPipeline.BXRenderPipeline.legacyShaderTagIds[i]);
            }
            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        }

        private void DrawGizmosBeforePostProcess()
        {
            if (camera.cameraType == CameraType.SceneView)
            {
                context.DrawWireOverlay(camera);
            }

            if (Handles.ShouldRenderGizmos())
            {
                context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            }
        }

        private void DrawGizmosAfterPostProcess()
		{
            if (Handles.ShouldRenderGizmos())
            {
                context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
            }
        }
    }
}
#endif