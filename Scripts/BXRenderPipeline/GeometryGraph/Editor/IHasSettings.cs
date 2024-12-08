using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
	public interface IHasSettings
	{
		VisualElement CreateSettingsElement();
	}
}
