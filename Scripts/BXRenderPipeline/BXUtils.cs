using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace BXRenderPipeline
{
	public static class BXUtils
	{
        internal static Texture3D m_WhiteVolumeTexture;
        /// <summary>
        /// White 3D texture.
        /// </summary>
        internal static Texture3D whiteVolumeTexture
        {
            get
            {
                if (m_WhiteVolumeTexture == null)
                {
                    Color[] colors = { Color.white };
                    m_WhiteVolumeTexture = new Texture3D(1, 1, 1, GraphicsFormat.R8G8B8A8_SRGB, TextureCreationFlags.None);
                    m_WhiteVolumeTexture.SetPixels(colors, 0);
                    m_WhiteVolumeTexture.Apply();
                }

                return m_WhiteVolumeTexture;
            }
        }
    }
}