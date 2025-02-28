using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using System.Runtime.InteropServices;

public class XEngineEditorWindow : EditorWindow
{
	[DllImport("XEngine.dll")]
	private static extern void StartXEngine(IntPtr parentHWnd, [MarshalAs(UnmanagedType.LPWStr)]string workDir);

	[DllImport("user32.dll")]
	private static extern IntPtr GetActiveWindow();

	private static XEngineEditorWindow wnd;

	[MenuItem("Window/XEngine")]
	public static void ShowWindow()
	{
		wnd = GetWindow<XEngineEditorWindow>();
	}

	private void OnGUI()
	{
		if(GUILayout.Button("Start XEngine"))
		{
			StartXEngine(GetActiveWindow(), System.IO.Directory.GetCurrentDirectory());
		}
	}
}
