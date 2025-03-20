using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BXRenderPipeline
{
    /// <summary>
    /// This attribute allows you to add commands to the <b>Add Override</b> popup menu
    /// on Volumes
    /// To filter BXVolumeComponentMenu based on current Render Pipeline, add SupportedOnRenderPipeline attribute to the class alongside with this attribute
    /// </summary>
    public class BXVolumeComponentMenu : Attribute
    {
        /// <summary>
        /// The name of the entry in the override list. You can use slashes to create sub-menus
        /// </summary>
        public readonly string menu;

        // TODO: Add support for component icons

        /// <summary>
        /// Creates a new <see cref="BXVolumeComponentMenu"/> instance
        /// </summary>
        /// <param name="menu">The name of the entry in the override list. You can use slashes to create sub-menus.</param>
        public BXVolumeComponentMenu(string menu)
        {
            this.menu = menu;
        }
    }

    /// <summary>
    /// This attribute allows you to add commands to the <b>Add Override</b> popup menu
    /// on Volumes and specify for which render pipelines will be supported
    /// </summary>
    [Obsolete(@"BXVolumeComponentMenuForRenderPipelineAttribute is deprecated. Use VolumeComponentMenu with SupportedOnCurrentPipeline instead. #from(2023.1)", false)]
    public class BXVolumeComponentMenuForRenderPipeline : BXVolumeComponentMenu
    {
        /// <summary>
        /// The list of pipeline types that the target class supports
        /// </summary>
        public Type[] pipelineTypes { get; }


        /// <summary>
        /// Creates a new <see cref="BXVolumeComponentMenuForRenderPipeline"/> instance.
        /// </summary>
        /// <param name="menu">The name of the entry in the override list. You can use slashes to
        /// create sub-menus.</param>
        /// <param name="pipelineTypes">The list of pipeline types that the target class supports</param>
        public BXVolumeComponentMenuForRenderPipeline(string menu, params Type[] pipelineTypes)
            : base(menu)
        {
            if (pipelineTypes == null)
                throw new Exception("Specify a list of supported pipeline");

            // Make sure that we only allow the class types that inherit from the render pipeline
            foreach (var t in pipelineTypes)
            {
                if (!typeof(RenderPipeline).IsAssignableFrom(t))
                    throw new Exception(
                        $"You can only specify types that inherit from {typeof(RenderPipeline)}, please check {t}");
            }

            this.pipelineTypes = pipelineTypes;
        }
    }

    /// <summary>
    /// An attribute to hide the volume component to be added through `Add Override` button on the volume component list
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    [Obsolete("VolumeComponentDeprecated has been deprecated (UnityUpgradable) -> [UnityEngine] UnityEngine.HideInInspector", false)]
    public sealed class BXVolumeComponentDeprecated : Attribute
    {
    }

    /// <summary>
    /// The base class for all the components that can be part of a <see cref="BXRenderSettingsVolume"/>
    /// </summary>
    [Serializable]
    public abstract class BXVolumeComponment : ScriptableObject
    {
        public bool enable;
        [HideInInspector]
        public bool enableRuntime;

        public abstract RenderFeatureStep RenderFeatureStep
		{
            get;
		}

        public abstract void OverrideData(BXVolumeComponment component, float interpFactor);

        public abstract void OnRender(CommandBuffer cmd, BXMainCameraRenderBase render);

        public abstract void OnDisabling(float interpFactor);

        public abstract void BeEnabled();

        public abstract void BeDisabled();

        public abstract void RefreshData();
    }
}
