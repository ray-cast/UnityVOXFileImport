using System;
using System.IO;

using UnityEngine;
using UnityEditor;

using Cubizer;
using Cubizer.Model;

public class VOXFileLoader : EditorWindow
{
	public bool _isSelectCreatePrefab = true;
	public bool _isSelectCreateAssetbundle = true;

	[MenuItem("Tools/Cubizer/Show VOXFileLoader Inspector")]
	public static void ShowWindow()
	{
		VOXFileLoader.CreateInstance<VOXFileLoader>().Show();
	}

	[MenuItem("Tools/Cubizer/Load .vox file as Prefab")]
	public static void LoadVoxelFileAsPrefab()
	{
		var filepath = EditorUtility.OpenFilePanel("Load .vox file", "", "vox");
		if (filepath != null && filepath != "")
		{
			if (!filepath.Contains(".vox"))
			{
				EditorUtility.DisplayDialog("Invalid File", "The end of the path wasn't \".vox\"", "Ok");
				return;
			}

			VoxFileImport.LoadVoxelFileAsPrefab(filepath);
		}
	}

	[MenuItem("Tools/Cubizer/Load .vox file as GameObject")]
	public static void LoadVoxelFileAsGameObject()
	{
		var filepath = EditorUtility.OpenFilePanel("Load .vox file", "", "vox");
		if (filepath != null && filepath != "")
		{
			if (!filepath.Contains(".vox"))
			{
				EditorUtility.DisplayDialog("Invalid File", "The end of the path wasn't \".vox\"", "Ok");
				return;
			}

			VoxFileImport.LoadVoxelFileAsGameObject(filepath);
		}
	}

	public void OnGUI()
	{
		GUILayout.Label("Selected Object:", EditorStyles.boldLabel);

		this._isSelectCreatePrefab = EditorGUILayout.Foldout(this._isSelectCreatePrefab, "Create Model from .vox file");
		if (this._isSelectCreatePrefab)
		{
			if (GUILayout.Button("Create Prefab from .vox file"))
				CreateVoxelPrefabsFromSelection();

			if (GUILayout.Button("Create Prefab LOD from .vox file"))
				CreateVoxelPrefabsFromSelection();

			if (GUILayout.Button("Create GameObject from .vox file"))
				CreateVoxelGameObjectFromSelection();

			if (GUILayout.Button("Create GameObject LOD from .vox file"))
				CreateVoxelGameObjectFromSelection();
		}

		this._isSelectCreateAssetbundle = EditorGUILayout.Foldout(this._isSelectCreateAssetbundle, "Create AssetBundle");
		if (this._isSelectCreateAssetbundle)
		{
			if (GUILayout.Button("Selection To StreamingAssets folder"))
				CreateAssetBundlesFromSelectionToStreamingAssets();

			if (GUILayout.Button("Selection To Selected Folder"))
				CreateAssetBundlesWithFolderPanel();
		}
	}

	private static bool CreateVoxelPrefabsFromSelection()
	{
		var SelectedAsset = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.DeepAssets);
		if (SelectedAsset.Length == 0)
		{
			EditorUtility.DisplayDialog("No Object Selected", "Please select any .vox file to create to prefab", "Ok");
			return false;
		}

		foreach (var asset in SelectedAsset)
		{
			var path = AssetDatabase.GetAssetPath(asset);
			if (Path.GetExtension(path) != ".vox")
			{
				EditorUtility.DisplayDialog("Invalid File", "The end of the path wasn't \".vox\"", "Ok");
				return false;
			}

			if (path.Remove(0, path.LastIndexOf('.')) == ".vox")
				VoxFileImport.LoadVoxelFileAsPrefab(path);
		}

		return true;
	}

	private static bool CreateVoxelGameObjectFromSelection()
	{
		var SelectedAsset = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.DeepAssets);
		if (SelectedAsset.Length == 0)
		{
			EditorUtility.DisplayDialog("No Object Selected", "Please select any .vox file to create to prefab", "Ok");
			return false;
		}

		foreach (var asset in SelectedAsset)
		{
			var path = AssetDatabase.GetAssetPath(asset);
			if (Path.GetExtension(path) != ".vox")
			{
				EditorUtility.DisplayDialog("Invalid File", "The end of the path wasn't \".vox\"", "Ok");
				return false;
			}

			if (path.Remove(0, path.LastIndexOf('.')) == ".vox")
			{
				VoxFileImport.LoadVoxelFileAsGameObject(path);
			}
		}

		return true;
	}

	private static void CreateAssetBundlesFromSelectionToStreamingAssets()
	{
		var SelectedAsset = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.DeepAssets);

		foreach (var obj in SelectedAsset)
		{
			string targetPath = Application.dataPath + "/StreamingAssets/" + obj.name + ".assetbundle";

			if (BuildPipeline.BuildAssetBundle(obj, null, targetPath, BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets, BuildTarget.StandaloneWindows))
				UnityEngine.Debug.Log(obj.name + ": loaded successfully");
			else
				UnityEngine.Debug.Log(obj.name + ": failed to load");
		}

		AssetDatabase.Refresh();
	}

	private static void CreateAssetBundlesWithFolderPanel()
	{
		var SelectedPath = EditorUtility.SaveFolderPanel("Save Resource", "", "New Resource");
		if (SelectedPath.Length == 0)
			return;

		var SelectedAsset = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.DeepAssets);

		foreach (var obj in SelectedAsset)
		{
			string targetPath = SelectedPath + obj.name + ".assetbundle";

			if (BuildPipeline.BuildAssetBundle(obj, null, targetPath, BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets, BuildTarget.StandaloneWindows))
				UnityEngine.Debug.Log(obj.name + ": loaded successfully");
			else
				UnityEngine.Debug.Log(obj.name + ": failed to load");
		}

		AssetDatabase.Refresh();
	}
}