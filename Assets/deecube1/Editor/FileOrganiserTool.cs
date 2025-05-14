using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class MappingEntry
{
    public string prefix;
    public string destination;
    public string extension;
}

public class FileOrganiserTool : EditorWindow
{
    private DefaultAsset selectedFolder;
    private TextAsset mappingFileAsset;
    private string newMappingFileName = "Mapping";
    private bool dryRun = true;
    private Vector2 scroll;
    private Vector2 mappingScroll;
    private List<MappingEntry> customMappings = new();
    private List<string> moveLogs = new();

    [MenuItem("deecube1/File Organiser")]
    public static void ShowWindow()
    {
        GetWindow<FileOrganiserTool>("File Organiser");
    }

    private void OnGUI()
    {
        GUILayout.Label("Folder Selection", EditorStyles.boldLabel);
        selectedFolder = (DefaultAsset)EditorGUILayout.ObjectField("Scan Folder", selectedFolder, typeof(DefaultAsset), false);

        GUILayout.Label("Custom Prefix Mappings", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add New Mapping"))
            customMappings.Add(new MappingEntry());
        GUILayout.Space(10);
        newMappingFileName = EditorGUILayout.TextField("Save As", newMappingFileName);
        if (GUILayout.Button("Save"))
            SaveMappingsToFile(newMappingFileName);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        mappingFileAsset = (TextAsset)EditorGUILayout.ObjectField("Load Mapping File", mappingFileAsset, typeof(TextAsset), false);
        if (GUILayout.Button("Load") && mappingFileAsset != null)
            LoadMappingsFromFile(mappingFileAsset);
        EditorGUILayout.EndHorizontal();

        mappingScroll = EditorGUILayout.BeginScrollView(mappingScroll, GUILayout.Height(150));
        for (int i = 0; i < customMappings.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            customMappings[i].prefix = EditorGUILayout.TextField(customMappings[i].prefix, GUILayout.Width(100));
            customMappings[i].destination = EditorGUILayout.TextField(customMappings[i].destination);
            customMappings[i].extension = EditorGUILayout.TextField(customMappings[i].extension, GUILayout.Width(80));
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                customMappings.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        dryRun = EditorGUILayout.Toggle("Dry Run (Preview Only)", dryRun);

        if (GUILayout.Button("Organise Files"))
            OrganiseAssets();

        GUILayout.Label("Logs", EditorStyles.boldLabel);
        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(150));
        foreach (string log in moveLogs)
            EditorGUILayout.LabelField(log);
        EditorGUILayout.EndScrollView();
    }

    private void SaveMappingsToFile(string fileName)
    {
        if (!fileName.EndsWith(".json"))
            fileName += ".json";

        string toolPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
        string toolDir = Path.GetDirectoryName(toolPath);
        string mappingDir = Path.Combine(toolDir, "FO_MappingData");

        if (!Directory.Exists(mappingDir))
        {
            Directory.CreateDirectory(mappingDir);
            AssetDatabase.Refresh();
        }

        string path = Path.Combine(mappingDir, fileName);
        File.WriteAllText(path, JsonUtility.ToJson(new Wrapper { mappings = customMappings }, true));
        AssetDatabase.Refresh();
        Debug.Log($"Mappings saved to {path}");
    }

    private void LoadMappingsFromFile(TextAsset fileAsset)
    {
        string path = AssetDatabase.GetAssetPath(fileAsset);
        if (!File.Exists(path))
        {
            Debug.LogWarning("Mapping file not found.");
            return;
        }
        var wrapper = JsonUtility.FromJson<Wrapper>(File.ReadAllText(path));
        customMappings = wrapper.mappings;
        Debug.Log("Mappings loaded from " + path);
    }

    [System.Serializable]
    private class Wrapper
    {
        public List<MappingEntry> mappings = new();
    }

    private void OrganiseAssets()
    {
        moveLogs.Clear();
        if (selectedFolder == null)
        {
            Debug.LogError("No folder selected.");
            moveLogs.Add("[Error] No folder selected.");
            return;
        }

        string folderPath = AssetDatabase.GetAssetPath(selectedFolder);
        string[] guids = AssetDatabase.FindAssets("", new[] { folderPath });
        bool anyMatched = false;

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            string ext = Path.GetExtension(assetPath).ToLower();
            string fileName = Path.GetFileNameWithoutExtension(assetPath);

            foreach (var mapping in customMappings)
            {
                if (fileName.StartsWith(mapping.prefix) && (string.IsNullOrEmpty(mapping.extension) || ext == mapping.extension.ToLower()))
                {
                    anyMatched = true;
                    string targetFolder = mapping.destination;
                    if (!AssetDatabase.IsValidFolder(targetFolder))
                    {
                        string[] parts = targetFolder.Split('/');
                        string buildPath = parts[0];
                        for (int i = 1; i < parts.Length; i++)
                        {
                            string next = buildPath + "/" + parts[i];
                            if (!AssetDatabase.IsValidFolder(next))
                                AssetDatabase.CreateFolder(buildPath, parts[i]);
                            buildPath = next;
                        }
                    }

                    string newPath = Path.Combine(targetFolder, Path.GetFileName(assetPath));
                    if (!dryRun)
                    {
                        string result = AssetDatabase.MoveAsset(assetPath, newPath);
                        if (!string.IsNullOrEmpty(result))
                        {
                            Debug.LogError($"[Failed] {assetPath} → {newPath}: {result}");
                            moveLogs.Add($"[Failed] {assetPath} → {newPath}");
                        }
                        else
                        {
                            Debug.Log($"[Moved] {assetPath} → {newPath}");
                            moveLogs.Add($"[Moved] {assetPath} → {newPath}");
                        }
                    }
                    else
                    {
                        moveLogs.Add($"[Preview] {assetPath} → {newPath}");
                    }
                    break;
                }
            }
        }

        if (!anyMatched)
        {
            string note = "[Note] No matching mapping found for assets in the selected folder.";
            Debug.LogWarning(note);
            moveLogs.Add(note);
        }

        if (!dryRun) AssetDatabase.Refresh();
    }
}
