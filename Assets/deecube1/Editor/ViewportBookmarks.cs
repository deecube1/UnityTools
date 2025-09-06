using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class ViewportBookmarks : EditorWindow
{
    [System.Serializable]
    private class Bookmark
    {
        public string Name;
        public Vector3 Position;
        public Quaternion Rotation;
        public float FOV;
    }

    [System.Serializable]
    private class BookmarkList
    {
        public List<Bookmark> Bookmarks = new List<Bookmark>();
    }

    private BookmarkList bookmarkList = new BookmarkList();
    private string newBookmarkName = "New Bookmark";
    private string saveFilePath;

    [MenuItem("deecube1/ViewportBookmarks")]
    public static void ShowWindow()
    {
        GetWindow<ViewportBookmarks>("Viewport Bookmarks");
    }

    private void OnEnable()
    {
        string scriptDirectory = Path.GetDirectoryName(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this)));
        saveFilePath = Path.Combine(scriptDirectory, "ViewportBookmarks.json");
        LoadBookmarks();
    }

    private void OnGUI()
    {
        if (SceneView.lastActiveSceneView == null || SceneView.lastActiveSceneView.camera == null)
        {
            EditorGUILayout.HelpBox("No active SceneView found. Please open a SceneView to use this tool.", MessageType.Warning);
            return;
        }

        GUILayout.Label("Save Current View", EditorStyles.boldLabel);

        newBookmarkName = EditorGUILayout.TextField("Bookmark Name", newBookmarkName);

        if (GUILayout.Button("Save/Bookmark View"))
        {
            SaveCurrentView();
        }

        GUILayout.Space(10);

        GUILayout.Label("Bookmarks", EditorStyles.boldLabel);

        for (int i = 0; i < bookmarkList.Bookmarks.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label((i + 1).ToString(), GUILayout.Width(20));

            if (GUILayout.Button(bookmarkList.Bookmarks[i].Name))
            {
                MoveToBookmark(i);
            }

            if (GUILayout.Button("Delete", GUILayout.Width(60)))
            {
                DeleteBookmark(i);
            }

            EditorGUILayout.EndHorizontal();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Reset Camera"))
        {
            ResetCamera();
        }
    }

    private void SaveCurrentView()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        Camera sceneCamera = sceneView.camera;

        Bookmark bookmark = new Bookmark
        {
            Name = newBookmarkName,
            Position = sceneCamera.transform.position,
            Rotation = sceneCamera.transform.rotation,
            FOV = sceneCamera.fieldOfView
        };

        bookmarkList.Bookmarks.Add(bookmark);
        SaveBookmarks();
    }

    private void MoveToBookmark(int index)
    {
        Bookmark bookmark = bookmarkList.Bookmarks[index];
        SceneView sceneView = SceneView.lastActiveSceneView;

        Transform dummy = null;
        try
        {
            dummy = SetupDummyObject(bookmark.Position, bookmark.Rotation);
            sceneView.AlignViewToObject(dummy);
            sceneView.camera.fieldOfView = bookmark.FOV;
            sceneView.Repaint();
        }
        finally
        {
            CleanupDummyObject(dummy);
        }
    }

    private void DeleteBookmark(int index)
    {
        bookmarkList.Bookmarks.RemoveAt(index);
        SaveBookmarks();
    }

    private void ResetCamera()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;

        Transform dummy = null;
        try
        {
            dummy = SetupDummyObject(Vector3.zero, Quaternion.identity);
            sceneView.AlignViewToObject(dummy);
            sceneView.camera.fieldOfView = 60f; // default FOV
            sceneView.Repaint();
        }
        finally
        {
            CleanupDummyObject(dummy);
        }
    }

    private void SaveBookmarks()
    {
        string json = JsonUtility.ToJson(bookmarkList);
        File.WriteAllText(saveFilePath, json);
    }

    private void LoadBookmarks()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            bookmarkList = JsonUtility.FromJson<BookmarkList>(json);
        }
    }

    private Transform SetupDummyObject(Vector3 position, Quaternion rotation)
    {
        var dummy = new GameObject("ViewportBookmarks_Dummy");
        dummy.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave; // keep hierarchy clean
        dummy.transform.position = position;
        dummy.transform.rotation = rotation;
        return dummy.transform;
    }

    private void CleanupDummyObject(Transform dummy)
    {
        if (dummy != null)
        {
            // Destroy immediately in editor
            Object.DestroyImmediate(dummy.gameObject);
        }
    }
}
