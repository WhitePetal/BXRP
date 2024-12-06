using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BXGeometryGraph
{
    [System.Serializable]
    public class SerializableTexture
    {
        [SerializeField]
        private string m_SerializedTexture;

        [SerializeField]
        private string m_Guid;

        [SerializeField]
        private Texture m_Texture;

        [System.Serializable]
        class TextureHelper
        {
            public Texture texture;
        }

        public Texture texture
        {
            get
            {
                if (!string.IsNullOrEmpty(m_SerializedTexture))
                {
                    var textureHelper = new TextureHelper();
                    EditorJsonUtility.FromJsonOverwrite(m_SerializedTexture, textureHelper);
                    m_SerializedTexture = null;
                    m_Guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(textureHelper.texture));
                    m_Texture = textureHelper.texture;
                }
                else if(!string.IsNullOrEmpty(m_Guid) && m_Texture == null)
                {
                    m_Texture = AssetDatabase.LoadAssetAtPath<Texture>(AssetDatabase.GUIDToAssetPath(m_Guid));
                }
                return m_Texture;
            }
            set
            {
                m_SerializedTexture = null;
                m_Guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value));
                m_Texture = value;
            }
        }
    }
}
