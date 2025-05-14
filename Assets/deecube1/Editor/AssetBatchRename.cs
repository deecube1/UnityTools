using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class AssetBatchRename : EditorWindow
{
    private string baseName = "NewName";
    private int startNumber = 1;
    private List<UnityEngine.Object> selectedObjects = new List<UnityEngine.Object>();
    private string firstPreviewName = string.Empty;
    private string lastPreviewName = string.Empty;

    [MenuItem("deecube1/Asset Batch Rename")]
    public static void ShowWindow()
    {
        GetWindow<AssetBatchRename>("Batch Rename Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("Batch Rename Tool", EditorStyles.boldLabel);

        // Base name field
        baseName = EditorGUILayout.TextField("Base Name", baseName);

        // Starting number field
        startNumber = EditorGUILayout.IntField("Start Number", startNumber);

        // Preview Button
        if (GUILayout.Button("Preview Names"))
        {
            GeneratePreview();
        }

        // Display Preview: First and Last names only
        if (!string.IsNullOrEmpty(firstPreviewName) && !string.IsNullOrEmpty(lastPreviewName))
        {
            GUILayout.Label("Preview:");
            GUILayout.Label($"First Object: {firstPreviewName}", EditorStyles.label);
            GUILayout.Label($"Last Object: {lastPreviewName}", EditorStyles.label);
        }

        // Batch Rename Button
        if (GUILayout.Button("Batch Rename Selected"))
        {
            BatchRename();
        }
    }

    private void GeneratePreview()
    {
        selectedObjects = new List<UnityEngine.Object>(Selection.objects);

        if (selectedObjects.Count == 0)
        {
            UnityEngine.Debug.LogWarning("No objects selected for renaming!");
            firstPreviewName = string.Empty;
            lastPreviewName = string.Empty;
            return;
        }

        int number = startNumber;
        int padding = GetNumberPadding(startNumber, selectedObjects.Count);

        // Generate the first and last names for preview
        firstPreviewName = $"{baseName}_{number.ToString($"D{padding}")}";
        lastPreviewName = $"{baseName}_{(number + selectedObjects.Count - 1).ToString($"D{padding}")}";
    }

    private void BatchRename()
    {
        if (selectedObjects.Count == 0)
        {
            UnityEngine.Debug.LogWarning("No objects selected for renaming!");
            return;
        }

        Undo.RegisterCompleteObjectUndo(selectedObjects.ToArray(), "Batch Rename");

        int number = startNumber;
        int padding = GetNumberPadding(startNumber, selectedObjects.Count);

        for (int i = 0; i < selectedObjects.Count; i++)
        {
            string formattedNumber = number.ToString($"D{padding}");
            string newName = $"{baseName}_{formattedNumber}";
            UnityEngine.Object obj = selectedObjects[i];

            if (obj is GameObject gameObject)
            {
                gameObject.name = newName;
            }
            else if (AssetDatabase.Contains(obj))
            {
                string assetPath = AssetDatabase.GetAssetPath(obj);
                AssetDatabase.RenameAsset(assetPath, newName);
            }

            number++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        UnityEngine.Debug.Log("Batch rename completed!");
    }

    private int GetNumberPadding(int start, int count)
    {
        // Calculate the largest number that will be generated
        int maxNumber = start + count - 1;

        // Determine the number of digits in the largest number
        int maxDigits = Mathf.Max(maxNumber.ToString().Length, 2); // Default to 2 digits if smaller
        return maxDigits;
    }
}
