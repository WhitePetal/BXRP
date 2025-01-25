using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.Rendering.DebugUI;

namespace BXRenderPipeline
{
    /// <summary>
    /// Widget that will show into the Runtime UI only
    /// Warning the user if the Runtime Debug Shaders variants are being stripped from the build.
    /// </summary>
    public class RuntimeDebugShadersMessageBox : MessageBox
    {
        /// <summary>
        /// Constructs a <see cref="RuntimeDebugShadersMessageBox"/>
        /// </summary>
        public RuntimeDebugShadersMessageBox()
        {
            displayName =
                "Warning: the debug shader variants are missing. Ensure that the \"Strip Runtime Debug Shaders\" option is disabled in the SRP Graphics Settings.";
            style = DebugUI.MessageBox.Style.Warning;
            isHiddenCallback = () =>
            {
#if !UNITY_EDITOR
                    //if (GraphicsSettings.TryGetRenderPipelineSettings<ShaderStrippingSetting>(out var shaderStrippingSetting))
                    //    return !shaderStrippingSetting.stripRuntimeDebugShaders;
                return false;
#endif
                return true;
            };
        }
    }
}
