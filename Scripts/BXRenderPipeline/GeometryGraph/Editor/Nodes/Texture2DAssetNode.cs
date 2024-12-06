using System.Collections;
using System.Collections.Generic;
using BXGraphing;
using UnityEngine;

namespace BXGeometryGraph
{
    [Title("Input", "Texture", "Texture 2D Asset")]
    public class Texture2DAssetNode : AbstractGeometryNode, IPropertyFromNode
    {
        public const int OutputSlotId = 0;

        const string kOutputSlotName = "Out";

        public Texture2DAssetNode()
        {
            name = "Texture 2D Asset";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Texture-2D-Asset-Node"; }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            // TODO
            //AddSlot(new Texture2DMaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        [SerializeField]
        private SerializableTexture m_Texture = new SerializableTexture();

        // TODO
        //[TextureControl("")]
        public Texture texture
        {
            get { return m_Texture.texture; }
            set
            {
                if (m_Texture.texture == value)
                    return;
                m_Texture.texture = value;
                Dirty(ModificationScope.Node);
            }
        }

        public override void CollectGeometryProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            properties.AddGeometryProperty(new TextureGeometryProperty()
            {
                overrideReferenceName = GetVariableNameForSlot(OutputSlotId),
                generatePropertyBlock = true,
                value = m_Texture,
                modifiable = false
            });
        }

        public override void CollectPreviewGeometryProperties(List<PreviewProperty> properties)
        {
            properties.Add(new PreviewProperty(PropertyType.Texture)
            {
                name = GetVariableNameForSlot(OutputSlotId),
                textureValue = texture
            });
        }

        public IGeometryProperty AsGeometryProperty()
        {
            var prop = new TextureGeometryProperty { value = m_Texture };
            if (texture != null)
                prop.displayName = texture.name;
            return prop;
        }

        public int outputSlotID { get { return OutputSlotId; } }
    }
}
