using System.Collections;
using System.Collections.Generic;
using System.Text;
using BXGraphing;
using UnityEngine;

namespace BXGeometryGraph
{
    [System.Serializable]
    public class TextureGeometryProperty : AbstractGeometryProperty<SerializableTexture>
    {
        [SerializeField]
        private bool m_Modifiable = true;

        public TextureGeometryProperty()
        {
            value = new SerializableTexture();
            displayName = "Texture";
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Texture; }
        }

        public bool modifiable
        {
            get { return m_Modifiable; }
            set { m_Modifiable = value; }
        }

        public override Vector4 defaultValue
        {
            get { return new Vector4(); }
        }

        public override string GetPropertyBlockString()
        {
            var result = new StringBuilder();
            if (!m_Modifiable)
            {
                result.Append("[NonModifiableTextureData] ");
            }
            result.Append("[NoScaleOffset ");

            result.Append(referenceName);
            result.Append("(\"");
            result.Append(displayName);
            result.Append("\", 2D) = \"white\" {}");
            return result.ToString();
        }

        public override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return string.Format("TEXTURE2D{0}{1} SAMPLER(sampler{0}{1})", referenceName, delimiter);
        }

        public override string GetPropertyAsArgumentString()
        {
            return string.Format("TEXTURE2D_ARGS{0}, sampler{0}", referenceName);
        }

        public override PreviewProperty GetPreviewGeometryProperty()
        {
            return new PreviewProperty(PropertyType.Texture)
            {
                name = referenceName,
                textureValue = value.texture
            };
        }

        public override INode ToConcreteNode()
        {
            return new Texture2DAssetNode { texture = value.texture };
        }

        public override IGeometryProperty Copy()
        {
            var copied = new TextureGeometryProperty();
            copied.displayName = displayName;
            copied.value = value;
            return copied;
        }
    }
}
