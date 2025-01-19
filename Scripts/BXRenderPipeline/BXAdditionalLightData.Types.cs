using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXRenderPipeline.HighDefinition
{
	[Flags]
	public enum RenderingLayerMask
	{
		/// <summary>
		/// No rendering layer
		/// </summary>
		Nothing = 0,
		/// <summary>
		/// Rendering layer 1
		/// </summary>
		RenderingLayer1 = 1 << 0,
		/// <summary>
		/// Rendering layer 2
		/// </summary>
		RenderingLayer2 = 1 << 1,


		[HideInInspector]
		Everything = 0xFFF,
	}
}
