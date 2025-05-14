using UnityEditor;
using UnityEngine;

public class ColliderRemover : EditorWindow
{
    [MenuItem("deecube1/Art/Remove Colliders From Selection")]
    public static void ShowWindow()
    {
        GetWindow<ColliderRemover>("Remove Colliders");
    }

    private void OnGUI()
    {
        GUILayout.Label("Select Prefabs in the Project Window or Objects in the Scene and click the button below", EditorStyles.wordWrappedLabel);

        if (GUILayout.Button("Remove Mesh Colliders"))
        {
            RemoveColliders<MeshCollider>();
        }

        if (GUILayout.Button("Remove Box Colliders"))
        {
            RemoveColliders<BoxCollider>();
        }

        if (GUILayout.Button("Remove Sphere Colliders"))
        {
            RemoveColliders<SphereCollider>();
        }
    }

    private void RemoveColliders<T>() where T : Collider
    {
        RemoveCollidersFromSelectedPrefabs<T>();
        RemoveCollidersFromSceneObjects<T>();
    }

    private void RemoveCollidersFromSelectedPrefabs<T>() where T : Collider
    {
        var selectedPrefabs = Selection.GetFiltered<GameObject>(SelectionMode.Assets);

        if (selectedPrefabs.Length == 0)
        {
            UnityEngine.Debug.Log($"No prefabs selected in the Project window.");
            return;
        }

        foreach (var prefab in selectedPrefabs)
        {
            string assetPath = AssetDatabase.GetAssetPath(prefab);
            if (!string.IsNullOrEmpty(assetPath))
            {
                GameObject prefabInstance = PrefabUtility.LoadPrefabContents(assetPath);
                if (prefabInstance != null)
                {
                    T[] colliders = prefabInstance.GetComponentsInChildren<T>();

                    if (colliders.Length == 0)
                    {
                        UnityEngine.Debug.Log($"No {typeof(T).Name} found in prefab: {assetPath}");
                    }
                    else
                    {
                        foreach (var collider in colliders)
                        {
                            // Record changes to the prefab instance for manual saving or Auto Save to detect changes
                            Undo.RegisterCompleteObjectUndo(prefabInstance, "Remove Colliders");
                            DestroyImmediate(collider);
                        }

                        PrefabUtility.SaveAsPrefabAsset(prefabInstance, assetPath);
                        UnityEngine.Debug.Log($"Removed {typeof(T).Name} from prefab: {assetPath}");
                    }

                    PrefabUtility.UnloadPrefabContents(prefabInstance);
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void RemoveCollidersFromSceneObjects<T>() where T : Collider
    {
        var selectedObjects = Selection.GetFiltered<GameObject>(SelectionMode.Editable | SelectionMode.TopLevel);

        if (selectedObjects.Length == 0)
        {
            UnityEngine.Debug.Log($"No objects selected in the Scene or Hierarchy.");
            return;
        }

        foreach (var obj in selectedObjects)
        {
            T[] colliders = obj.GetComponentsInChildren<T>();

            if (colliders.Length == 0)
            {
                UnityEngine.Debug.Log($"No {typeof(T).Name} found in object: {obj.name}");
            }
            else
            {
                foreach (var collider in colliders)
                {
                    // Record changes to the scene object for Undo/Redo
                    Undo.RegisterCompleteObjectUndo(obj, "Remove Colliders");
                    DestroyImmediate(collider);
                }

                UnityEngine.Debug.Log($"Removed {typeof(T).Name} from object: {obj.name}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
