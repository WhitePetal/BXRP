using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BXGeometryGraph
{
    public class PropertyCollector
    {
        public struct TextureInfo
        {
            public string name;
            public int textureId;
            public bool modifiable;
        }

        private readonly List<IGeometryProperty> m_Properties = new List<IGeometryProperty>();

        public void AddGeometryProperty(IGeometryProperty chunk)
        {
            if (m_Properties.Any(x => x.referenceName == chunk.referenceName))
                return;
            m_Properties.Add(chunk);
        }

        public string GetPropertiesBlock(int baseIndentLevel)
        {
            var sb = new StringBuilder();
            foreach(var prop in m_Properties.Where(x => x.generatePropertyBlock))
            {
                for (var i = 0; i < baseIndentLevel; ++i)
                    sb.Append("\t");
                sb.AppendLine(prop.GetPropertyBlockString());
            }
            return sb.ToString();
        }

        public string GetPropertiesDeclaration(int baseIndentLevel)
        {
            var sb = new StringBuilder();
            foreach(var prop in m_Properties)
            {
                for (var i = 0; i < baseIndentLevel; ++i)
                    sb.Append("\t");
                sb.AppendLine(prop.GetPropertyDeclarationString());
            }
            return sb.ToString();
        }

        public List<TextureInfo> GetConfiguredTextures()
        {
            var result = new List<TextureInfo>();

            //foreach(var prop in m_Properties.OfType<textureshad>)
            // TODO
            return null;
        }
    }
}
