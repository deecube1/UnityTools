using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class PrefabBuilderProfile
{
    public string prefabName = "";
    public string savePath = "Assets/Art/Prefabs/";
    public string colliderPrefix = "CM";
    public string colliderType = "BoxCollider";
    public string tag = "Untagged";
    public int layer = 0;
    public bool removeMaterials = false;
    public bool removeEmptyObjects = false;
    public List<GameObject> selectedObjects = new List<GameObject>();
}

public class PrefabBuilderWindow : EditorWindow
{
    private PrefabBuilderProfile profile = new PrefabBuilderProfile();
    private Vector2 scrollPos;
    private string profileName = "DefaultProfile";
    private string profileFolder = "";


    private bool dryRun = false;
    private List<GameObject> previewObjects = new List<GameObject>();

    [MenuItem("deecube1/Art/Prefab Builder")]
    public static void ShowWindow()
    {
        var window = GetWindow<PrefabBuilderWindow>("Prefab Builder");

        string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(window));
        string scriptDir = Path.GetDirectoryName(scriptPath);
        window.profileFolder = Path.Combine(scriptDir, "PrefabBuilderData");

        if (!Directory.Exists(window.profileFolder))
            Directory.CreateDirectory(window.profileFolder);
    }

    void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Width(position.width), GUILayout.Height(position.height));

        GUILayout.Label("Prefab Builder Profile", EditorStyles.boldLabel);
        profileName = EditorGUILayout.TextField("Profile Name", profileName);

        if (GUILayout.Button("Save Profile")) SaveProfile();
        GUILayout.Space(5);
        GUILayout.Label("Load Profile", EditorStyles.boldLabel);
        TextAsset jsonAsset = (TextAsset)EditorGUILayout.ObjectField("JSON Profile", null, typeof(TextAsset), false);
        if (GUILayout.Button("Load Profile") && jsonAsset != null)
        {
            string json = jsonAsset.text;
            profile = JsonUtility.FromJson<PrefabBuilderProfile>(json);
            Debug.Log("Profile loaded from asset: " + jsonAsset.name);
        }
        profile.tag = EditorGUILayout.TagField("Tag", profile.tag);
        profile.layer = EditorGUILayout.LayerField("Layer", profile.layer);
        profile.removeMaterials = EditorGUILayout.Toggle("Remove Materials", profile.removeMaterials);
        profile.removeEmptyObjects = EditorGUILayout.Toggle("Remove Empty Objects", profile.removeEmptyObjects);

        dryRun = EditorGUILayout.Toggle("Dry Run Mode", dryRun);

        GUILayout.Space(10);
        GUILayout.Label("Select Objects (Meshes or Prefabs)", EditorStyles.boldLabel);
        int newCount = Mathf.Max(0, EditorGUILayout.IntField("Count", profile.selectedObjects.Count));
        while (newCount > profile.selectedObjects.Count)
            profile.selectedObjects.Add(null);
        while (newCount < profile.selectedObjects.Count)
            profile.selectedObjects.RemoveAt(profile.selectedObjects.Count - 1);

        for (int i = 0; i < profile.selectedObjects.Count; i++)
        {
            profile.selectedObjects[i] = (GameObject)EditorGUILayout.ObjectField("Object " + (i + 1), profile.selectedObjects[i], typeof(GameObject), false);
        }

        GUILayout.Space(20);
        if (GUILayout.Button("Build Prefabs"))
        {
            if (dryRun) PreviewChanges();
            else BuildPrefabs();
        }

        if (dryRun)
        {
            GUILayout.Space(20);
            GUILayout.Label("Dry Run Preview", EditorStyles.boldLabel);
            foreach (var obj in previewObjects)
            {
                if (obj != null)
                {
                    EditorGUILayout.LabelField(obj.name);
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }

    int ColliderTypeIndex()
    {
        switch (profile.colliderType)
        {
            case "BoxCollider": return 0;
            case "SphereCollider": return 1;
            case "CapsuleCollider": return 2;
            case "MeshCollider": return 3;
            default: return 0;
        }
    }

    void SaveProfile()
    {
        if (!Directory.Exists(profileFolder)) Directory.CreateDirectory(profileFolder);
        string json = JsonUtility.ToJson(profile, true);
        File.WriteAllText(Path.Combine(profileFolder, profileName + ".json"), json);
        Debug.Log("Profile saved.");
    }



    void PreviewChanges()
    {
        previewObjects.Clear();
        foreach (var go in profile.selectedObjects)
        {
            if (go != null) previewObjects.Add(go);
        }
    }

    void BuildPrefabs()
    {
        int total = profile.selectedObjects.Count;
        int current = 0;

        foreach (var go in profile.selectedObjects)
        {
            current++;
            EditorUtility.DisplayProgressBar("Building Prefabs", go?.name ?? "null", (float)current / total);

            if (go == null) continue;

            string prefabName = string.IsNullOrEmpty(profile.prefabName) ? go.name : profile.prefabName;
            string savePath = Path.Combine(profile.savePath, prefabName + ".prefab");

            if (File.Exists(savePath))
            {
                if (!EditorUtility.DisplayDialog("Prefab Exists", $"Prefab '{prefabName}' already exists. Skip or Overwrite?", "Skip", "Overwrite"))
                    continue;
            }

            GameObject newPrefab = PrefabUtility.SaveAsPrefabAsset(go, savePath);
            ApplyChanges(newPrefab);
            PrefabUtility.SaveAsPrefabAsset(newPrefab, savePath);
        }

        EditorUtility.ClearProgressBar();
        Debug.Log("Prefab build complete.");
    }

    void ApplyChanges(GameObject root)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child == root.transform) continue;

            if (!string.IsNullOrEmpty(profile.colliderPrefix) && child.name.StartsWith(profile.colliderPrefix))
            {
                if (profile.colliderType == "BoxCollider") child.gameObject.AddComponent<BoxCollider>();
                else if (profile.colliderType == "SphereCollider") child.gameObject.AddComponent<SphereCollider>();
                else if (profile.colliderType == "CapsuleCollider") child.gameObject.AddComponent<CapsuleCollider>();
                else if (profile.colliderType == "MeshCollider") child.gameObject.AddComponent<MeshCollider>();
            }

            if (profile.removeMaterials && child.TryGetComponent(out MeshRenderer mr))
                mr.sharedMaterials = new Material[0];

            if (!string.IsNullOrEmpty(profile.tag)) child.tag = profile.tag;
            child.gameObject.layer = profile.layer;
        }

        if (profile.removeEmptyObjects)
        {
            foreach (Transform child in children)
            {
                if (child.childCount == 0 && child.GetComponents<Component>().Length <= 1)
                    DestroyImmediate(child.gameObject);
            }
        }
    }
}
