using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXRenderPipeline
{
    // TODO: 2023 unity 版本 在 C 层实现了 RenderingLayerMask，因此更高版本不需要这个
    public struct RenderingLayerMask
    {
        private uint m_Bits;

        public static RenderingLayerMask defaultRenderingLayerMask { get; } = new() { m_Bits = 1u };

        internal const int maxRenbderingLayerSize = 32;

        public static implicit operator uint(RenderingLayerMask mask)
        {
            return mask.m_Bits;
        }

        public static implicit operator RenderingLayerMask(uint intVal)
        {
            RenderingLayerMask mask;
            mask.m_Bits = intVal;
            return mask;
        }

        public static implicit operator int(RenderingLayerMask mask)
        {
            return unchecked((int)mask.m_Bits);
        }

        public static implicit operator RenderingLayerMask(int intVal)
        {
            RenderingLayerMask mask;
            mask.m_Bits = unchecked((uint)intVal);
            return mask;
        }

        public uint value
        {
            get => m_Bits;
            set => m_Bits = value;
        }

        // TODO: implement in C#

        //[NativeMethod("GetDefaultRenderingLayerValue")]
        //static extern uint Internal_GetDefaultRenderingLayerValue();

        //// Given a layer number, returns the name of the layer as defined in either a Builtin or a User Layer in the [[wiki:class-TagManager|Tag Manager]]
        //[StaticAccessor("GetTagManager()", StaticAccessorType.Dot)]
        //[NativeMethod("RenderingLayerToString")]
        //public static extern string RenderingLayerToName(int layer);

        //// Given a layer name, returns the layer index as defined by either a Builtin or a User Layer in the [[wiki:class-TagManager|Tag Manager]]
        //[StaticAccessor("GetTagManager()", StaticAccessorType.Dot)]
        //[NativeMethod("StringToRenderingLayer")]
        //public static extern int NameToRenderingLayer(string layerName);

        //// Given a set of layer names, returns the equivalent layer mask for all of them.
        //public static uint GetMask(params string[] renderingLayerNames)
        //{
        //    if (renderingLayerNames == null)
        //        throw new ArgumentNullException(nameof(renderingLayerNames));

        //    uint mask = 0;
        //    for (var i = 0; i < renderingLayerNames.Length; i++)
        //    {
        //        var layer = NameToRenderingLayer(renderingLayerNames[i]);
        //        if (layer != -1)
        //            mask |= 1u << layer;
        //    }

        //    return mask;
        //}

        //// Given a span of layer names, returns the equivalent layer mask for all of them.
        //public static uint GetMask(ReadOnlySpan<string> renderingLayerNames)
        //{
        //    if (renderingLayerNames == null)
        //        throw new ArgumentNullException(nameof(renderingLayerNames));

        //    uint mask = 0;
        //    foreach (var name in renderingLayerNames)
        //    {
        //        var layer = NameToRenderingLayer(name);
        //        if (layer != -1)
        //            mask |= 1u << layer;
        //    }

        //    return mask;
        //}

        //[StaticAccessor("GetTagManager()", StaticAccessorType.Dot)]
        //public static extern int GetDefinedRenderingLayerCount();

        //[StaticAccessor("GetTagManager()", StaticAccessorType.Dot)]
        //public static extern int GetLastDefinedRenderingLayerIndex();

        //[StaticAccessor("GetTagManager()", StaticAccessorType.Dot)]
        //public static extern uint GetDefinedRenderingLayersCombinedMaskValue();

        //[StaticAccessor("GetTagManager()", StaticAccessorType.Dot)]
        //public static extern string[] GetDefinedRenderingLayerNames();

        //[StaticAccessor("GetTagManager()", StaticAccessorType.Dot)]
        //public static extern int[] GetDefinedRenderingLayerValues();

        //[StaticAccessor("GetTagManager()", StaticAccessorType.Dot)]
        //public static extern int GetRenderingLayerCount();
    }
}
