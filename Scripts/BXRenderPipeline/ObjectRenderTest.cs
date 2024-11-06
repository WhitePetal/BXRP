using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using BXRenderPipeline;

public class ObjectRenderTest : MonoBehaviour
{
    private void OnEnable()
    {

    }

    private void OnDisable()
    {
        
    }

    private void OnWillRenderObject()
    {
        BXHiZManager.instance.Register(this);
        // OnWillRenderObject calling in Renderpipeline Cull()
        // 0 means dont render
        //GetComponent<MeshRenderer>().renderingLayerMask = 0;
        //RenderTexture rt = new RenderTexture();
        //NativeArray<Vector4> output = new NativeArray<Vector4>();
        //NativeSlice<Vector4> slice = new NativeSlice<Vector4>();
        //AsyncGPUReadback.RequestIntoNativeArray(ref output, rt);
    }
}
