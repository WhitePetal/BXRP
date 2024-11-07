using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXRenderPipeline
{
    [RequireComponent(typeof(Renderer))]
    public class BXRenderer : MonoBehaviour
    {
        private Renderer m_Renderer;
        private int m_InstanceID;

        public uint renderLayerMask;

        private void Awake()
        {
            m_Renderer = GetComponent<Renderer>();
            m_InstanceID = m_Renderer.GetInstanceID();
        }

        // OnWillRenderObject calling in Renderpipeline Cull()
        private void OnWillRenderObject()
        {
            BXHiZManager.instance.Register(m_Renderer, m_InstanceID);
            // 0 means dont render
            //GetComponent<MeshRenderer>().renderingLayerMask = 0;
            //RenderTexture rt = new RenderTexture();
            //NativeArray<Vector4> output = new NativeArray<Vector4>();
            //NativeSlice<Vector4> slice = new NativeSlice<Vector4>();
            //AsyncGPUReadback.RequestIntoNativeArray(ref output, rt);
        }
    }
}
