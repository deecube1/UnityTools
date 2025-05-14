using UnityEditor;
using UnityEngine;

public class ReplaceWithPrefabEditor : EditorWindow
{
    private GameObject prefabToReplaceWith;

    [MenuItem("deecube1/Art/Replace With Prefab")]
    public static void ShowWindow()
    {
        GetWindow<ReplaceWithPrefabEditor>("Replace With Prefab");
    }

    private void OnGUI()
    {
        GUILayout.Label("Select Prefab to Replace With", EditorStyles.boldLabel);

        prefabToReplaceWith = (GameObject)EditorGUILayout.ObjectField("Prefab", prefabToReplaceWith, typeof(GameObject), false);

        if (GUILayout.Button("Replace Selected Objects"))
        {
            ReplaceSelectedObjects();
        }
    }

    private void ReplaceSelectedObjects()
    {
        if (prefabToReplaceWith == null)
        {
            UnityEngine.Debug.LogError("No prefab selected to replace with.");
            return;
        }

        GameObject[] selectedObjects = Selection.gameObjects;

        Undo.RegisterCompleteObjectUndo(selectedObjects, "Replace Objects with Prefab");

        foreach (GameObject obj in selectedObjects)
        {
            Transform objTransform = obj.transform;
            GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefabToReplaceWith, objTransform.parent);

            newObject.transform.position = objTransform.position;
            newObject.transform.rotation = objTransform.rotation;
            newObject.transform.localScale = objTransform.localScale;

            Undo.RegisterCreatedObjectUndo(newObject, "Replace Objects with Prefab");
            Undo.DestroyObjectImmediate(obj);
        }
    }
}
