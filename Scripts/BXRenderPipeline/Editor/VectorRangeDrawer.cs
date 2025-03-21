﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BXRenderPipeline
{
	public class VectorRangeDrawer : MaterialPropertyDrawer
	{
		private Vector2 range0, range1, range2, range3;
		public VectorRangeDrawer(float min0, float max0, float min1, float max1, float min2, float max2, float min3, float max3)
		{
			range0 = new Vector2(min0, max0);
			range1 = new Vector2(min1, max1);
			range2 = new Vector2(min2, max2);
			range3 = new Vector2(min3, max3);
		}

		public VectorRangeDrawer(float fmin0, float min0, float fmax0, float max0, float fmin1, float min1, float fmax1, float max1, float fmin2, float min2, float fmax2, float max2, float fmin3, float min3, float fmax3, float max3)
		{
			min0 = (int)fmin0 == 1 ? min0 : -min0;
			min1 = (int)fmin1 == 1 ? min1 : -min1;
			min2 = (int)fmin2 == 1 ? min2 : -min2;
			min3 = (int)fmin3 == 1 ? min3 : -min3;
			max0 = (int)fmax0 == 1 ? max0 : -max0;
			max1 = (int)fmax1 == 1 ? max1 : -max1;
			max2 = (int)fmax2 == 1 ? max2 : -max2;
			max3 = (int)fmax3 == 1 ? max3 : -max3;

			range0 = new Vector2(min0, max0);
			range1 = new Vector2(min1, max1);
			range2 = new Vector2(min2, max2);
			range3 = new Vector2(min3, max3);
		}

		public VectorRangeDrawer(float min0, float max0, float min1, float max1, float min2, float max2)
		{
			range0 = new Vector2(min0, max0);
			range1 = new Vector2(min1, max1);
			range2 = new Vector2(min2, max2);
			range3 = Vector2.zero;
		}
		public VectorRangeDrawer(float min0, float max0, float min1, float max1)
		{
			range0 = new Vector2(min0, max0);
			range1 = new Vector2(min1, max1);
			range2 = Vector2.zero;
			range3 = Vector2.zero;
		}
		public VectorRangeDrawer(float min0, float max0)
		{
			range0 = new Vector2(min0, max0);
			range1 = Vector2.zero;
			range2 = Vector2.zero;
			range3 = Vector2.zero;
		}

		public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
		{
			return base.GetPropertyHeight(prop, label, editor);
		}

		public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
		{
			Vector4 value = prop.vectorValue;
			string[] names = prop.displayName.Split('_');
			int count = 0;
			EditorGUI.BeginChangeCheck();
			if (range0.x < range0.y)
			{
				EditorGUI.LabelField(position, names[0]);
				int len = names[0].Length * 20;
				value.x = EditorGUI.Slider(new Rect(position.x + len, position.y, position.width - len, position.height), value.x, range0.x, range0.y);
				position.y += position.height + 5;
				++count;
			}
			if (range1.x < range1.y)
			{
				EditorGUI.LabelField(position, names[1]);
				int len = names[1].Length * 20;
				value.y = EditorGUI.Slider(new Rect(position.x + len, position.y, position.width - len, position.height), value.y, range1.x, range1.y);
				position.y += position.height + 5;
				++count;
			}
			if (range2.x < range2.y)
			{
				EditorGUI.LabelField(position, names[2]);
				int len = names[2].Length * 20;
				value.z = EditorGUI.Slider(new Rect(position.x + len, position.y, position.width - len, position.height), value.z, range2.x, range2.y);
				position.y += position.height + 5;
				++count;
			}
			if (range3.x < range3.y)
			{
				EditorGUI.LabelField(position, names[3]);
				int len = names[3].Length * 20;
				value.w = EditorGUI.Slider(new Rect(position.x + len, position.y, position.width - len, position.height), value.w, range3.x, range3.y);
				position.y += position.height + 5;
				++count;
			}
			if (EditorGUI.EndChangeCheck())
			{
				prop.vectorValue = value;
			}
			GUILayout.Space(count * 20 + 10);
		}
	}
}