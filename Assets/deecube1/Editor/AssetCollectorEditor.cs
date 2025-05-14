using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class AssetCollectorEditor : EditorWindow
{
    private List<string> collectedAssets = new List<string>();
    private Vector2 scrollPosition;
    private string txtFilePath;
    private string dataFolderPath;

    [MenuItem("deecube1/Asset Collector")]
    public static void ShowWindow()
    {
        GetWindow<AssetCollectorEditor>("Asset Collector");
    }

    private void OnEnable()
    {
        // Locate this script's folder
        string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
        string scriptDirectory = Path.GetDirectoryName(scriptPath);

        // Prepare subfolder path
        dataFolderPath = Path.Combine(scriptDirectory, "AssetCollectorData");

        // Create folder if it doesn't exist
        if (!Directory.Exists(dataFolderPath))
        {
            Directory.CreateDirectory(dataFolderPath);
            UnityEngine.Debug.Log($"Created folder: {dataFolderPath}");
        }

        // Set .txt file path
        txtFilePath = Path.Combine(dataFolderPath, "CollectedAssetsData.txt");

        // Load existing data if file exists
        if (File.Exists(txtFilePath))
        {
            string[] lines = File.ReadAllLines(txtFilePath);
            collectedAssets = new List<string>(lines);
            UnityEngine.Debug.Log($"Loaded collected assets from {txtFilePath}");
        }
        else
        {
            collectedAssets = new List<string>();
            UnityEngine.Debug.Log($"No data file found. New file will be created at {txtFilePath} when assets are collected.");
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Asset Collector", EditorStyles.boldLabel);

        if (GUILayout.Button("Export"))
        {
            ExportCollectedAssets();
        }

        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(400));

        if (GUILayout.Button("Collect"))
        {
            CollectSelectedAssets();
        }

        // Red 'X' delete style
        GUIStyle redButtonStyle = new GUIStyle(GUI.skin.button)
        {
            normal = { textColor = Color.red },
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };

        for (int i = 0; i < collectedAssets.Count; i++)
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button(Path.GetFileName(collectedAssets[i]), GUILayout.ExpandWidth(true)))
            {
                HighlightAssetInProjectWindow(collectedAssets[i]);
            }

            if (GUILayout.Button("X", redButtonStyle, GUILayout.Width(30)))
            {
                RemoveAsset(i);
            }

            GUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();
    }

    private void CollectSelectedAssets()
    {
        var selectedAssets = Selection.objects;
        if (selectedAssets.Length == 0)
        {
            UnityEngine.Debug.LogError("No asset selected. Please select an asset.");
            return;
        }

        bool newAssetsAdded = false;

        foreach (var asset in selectedAssets)
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);

            if (!collectedAssets.Contains(assetPath))
            {
                collectedAssets.Add(assetPath);
                File.AppendAllText(txtFilePath, assetPath + "\n");
                UnityEngine.Debug.Log($"Collected asset: {assetPath}");
                newAssetsAdded = true;
            }
            else
            {
                UnityEngine.Debug.LogWarning($"Asset {assetPath} is already collected.");
            }
        }

        if (!newAssetsAdded)
        {
            UnityEngine.Debug.Log("No new assets were added.");
        }
    }

    private void RemoveAsset(int index)
    {
        collectedAssets.RemoveAt(index);
        SaveAssetsToTxt();
    }

    private void HighlightAssetInProjectWindow(string assetPath)
    {
        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
        if (asset != null)
        {
            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;
        }
    }

    private void SaveAssetsToTxt()
    {
        File.WriteAllLines(txtFilePath, collectedAssets);
        UnityEngine.Debug.Log($"Saved collected assets to {txtFilePath}");
    }

    private void ExportCollectedAssets()
    {
        if (collectedAssets.Count == 0)
        {
            UnityEngine.Debug.LogError("No assets collected to export.");
            return;
        }

        SelectCollectedAssetsInProjectWindow();
        EditorApplication.ExecuteMenuItem("Assets/Export Package...");
    }

    private void SelectCollectedAssetsInProjectWindow()
    {
        List<UnityEngine.Object> selectedObjects = new List<UnityEngine.Object>();

        foreach (var assetPath in collectedAssets)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (asset != null)
            {
                selectedObjects.Add(asset);
            }
        }

        if (selectedObjects.Count > 0)
        {
            Selection.objects = selectedObjects.ToArray();
            EditorGUIUtility.PingObject(selectedObjects[0]);
        }

        UnityEngine.Debug.Log($"{selectedObjects.Count} collected assets selected in the Project window.");
    }
}
