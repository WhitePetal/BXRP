using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BXRenderPipeline
{
	// A StreamableAsset is an asset that is converted to a Streaming Asset for builds
	[Serializable]
	//[UnityEngine.Scripting.APIUpdating.MovedFrom(false, "UnityEngine.Rendering", "Unity.RenderPipelines.Core.Runtime", "ProbeVolumeBakingSet.StreamableAsset")]
	internal class ProbeVolumeStreamableAsset
	{
		[Serializable]
		//[UnityEngine.Scripting.APIUpdating.MovedFrom(false, "UnityEngine.Rendering", "Unity.RenderPipelines.Core.Runtime", "ProbeVolumeBakingSet.StreamableAsset.StreamableCellDesc")]
		public struct StreamableCellDesc
		{
			public int offset; // Offset of the cell within the file
			public int elementCount; // Number of elements in the cell (can be data chunks, bircks, debug info, etc)
		}

		[SerializeField]
		[FormerlySerializedAs("assetGUID")]
		private string m_AssetGUID = ""; // In the editor, allows us to load the asset through the AssetDatabase

		[SerializeField]
		[FormerlySerializedAs("streamableAssetPath")]
		private string m_StreambaleAssetPath = ""; // At runtime, path of the asset within the StreamingAssets data folder

		[SerializeField]
		[FormerlySerializedAs("elementSize")]
		private int m_ElementSize; // Size of an element. Can be a data chunk, a brick, etc

		[SerializeField]
		[FormerlySerializedAs("streamableCellDescs")]
		SerializedDictionary<int, StreamableCellDesc> m_StreamableCellDescs = new SerializedDictionary<int, StreamableCellDesc>();

		[SerializeField]
		private TextAsset m_Asset;

		public string assetGUID { get => m_AssetGUID; }
		public TextAsset asset { get => m_Asset; }
		public int elementSize { get => m_ElementSize; }
		public SerializedDictionary<int, StreamableCellDesc> streamableCellDescs { get => m_StreamableCellDescs; }

		private string m_FianlAssetPath;

		private FileHandle m_AssetFileHandle;

		public ProbeVolumeStreamableAsset(string apvStreamingAssetsPath, SerializedDictionary<int, StreamableCellDesc> cellDescs, int elementSize, string bakingSetGUID, string assetGUID)
		{
			m_AssetGUID = assetGUID;
			m_StreamableCellDescs = cellDescs;
			m_ElementSize = elementSize;
			m_StreambaleAssetPath = Path.Combine(Path.Combine(apvStreamingAssetsPath, bakingSetGUID), m_AssetGUID + ".bytes");
#if UNITY_EDITOR
			EnsureAssetLoaded();
#endif
		}

		internal void RefreshAssetPath()
		{
#if UNITY_EDITOR
			m_FianlAssetPath = AssetDatabase.GUIDToAssetPath(m_AssetGUID);
#else
			m_FianlAssetPath = Path.Combine(Application.streamingAssetsPath, m_StreambaleAssetPath);
#endif
		}

		public string GetAssetPath()
		{
			// Avoid GCAlloc every frame this is called
			if (string.IsNullOrEmpty(m_FianlAssetPath))
				RefreshAssetPath();

			return m_FianlAssetPath;
		}

		public unsafe bool FileExists()
		{
#if UNITY_EDITOR
			if (File.Exists(GetAssetPath()))
				return true;
			// File may not exist if it was moved, refresh path in this case
			RefreshAssetPath();
			return File.Exists(GetAssetPath());
#else
			// When not using streaming assets, this reference should always be valid
			if (m_Asset != null)
				return true;

			FileInfoResult result;
			AsyncReadManager.GetFileInfo(GetAssetPath(), &result).JobHandle.Complete();
			return result.FileState == FileState.Exists;
#endif
		}

#if UNITY_EDITOR
		// Ensures that the asset is referenced via Unity's serialization layer.
		public void EnsureAssetLoaded()
		{
			m_Asset = AssetDatabase.LoadAssetAtPath<TextAsset>(GetAssetPath());
		}

		public void RenameAsset(string newName)
		{
			AssetDatabase.RenameAsset(AssetDatabase.GUIDToAssetPath(m_AssetGUID), newName);
			m_FianlAssetPath = "";
		}

		// Temporarily clear the asset reference. Used to prevent serialization of the asset when we are using the StreamingAssets codepath
		public void ClearAssetReferenceForBuild()
		{
			m_Asset = null;
		}
#endif

		public long GetFileSize()
		{
			return new FileInfo(GetAssetPath()).Length;
		}

		public bool IsOpen()
		{
			return m_AssetFileHandle.IsValid();
		}

		public FileHandle OpenFile()
		{
			if (m_AssetFileHandle.IsValid())
				return m_AssetFileHandle;

			m_AssetFileHandle = AsyncReadManager.OpenFileAsync(GetAssetPath());
			return m_AssetFileHandle;
		}

		public void CloseFile()
		{
			if (m_AssetFileHandle.IsValid() && m_AssetFileHandle.JobHandle.IsCompleted)
				m_AssetFileHandle.Close();

			m_AssetFileHandle = default(FileHandle);
		}

		public bool IsValid()
		{
			return !string.IsNullOrEmpty(m_AssetGUID);
		}

		public void Dispose()
		{
			if (m_AssetFileHandle.IsValid())
			{
				m_AssetFileHandle.Close().Complete();
				m_AssetFileHandle = default(FileHandle);
			}
		}
	}
}
