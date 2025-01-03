using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXRenderPipeline
{
    //[ExecuteAlways]
    [RequireComponent(typeof(Renderer))]
    public class SRPMaterialColorProperty : MonoBehaviour
    {
        [SerializeField]
        [ColorUsage(true, true)]
        private Color diffuseColor;

        private Renderer m_Renderer;
        private MaterialPropertyBlock m_Block;

        private void Awake()
        {
            m_Renderer = GetComponent<Renderer>();
            m_Block = new MaterialPropertyBlock();
            m_Renderer.GetPropertyBlock(m_Block);
        }

        // Update is called once per frame
        void Update()
        {
            m_Block.SetColor(BXShaderPropertyIDs._DiffuseColor_ID, diffuseColor);
            m_Renderer.SetPropertyBlock(m_Block);
        }

        private void OnValidate()
        {
            if(m_Block == null)
            {
                m_Renderer = GetComponent<Renderer>();
                m_Block = new MaterialPropertyBlock();
                m_Renderer.GetPropertyBlock(m_Block);
            }
            m_Block.SetColor(BXShaderPropertyIDs._DiffuseColor_ID, diffuseColor);
            m_Renderer.SetPropertyBlock(m_Block);
        }
    }

}