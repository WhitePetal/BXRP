using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class ObjectRenderTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnWillRenderObject()
    {
        // OnWillRenderObject calling in Renderpipeline Cull()
        // 0 means dont render
        //GetComponent<MeshRenderer>().renderingLayerMask = 0;
        //RenderTexture rt = new RenderTexture();
        //NativeArray<Vector4> output = new NativeArray<Vector4>();
        //NativeSlice<Vector4> slice = new NativeSlice<Vector4>();
        //AsyncGPUReadback.RequestIntoNativeArray(ref output, rt);
    }
}
