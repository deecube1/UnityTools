using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

public class CenterParentToChildrenEditor : EditorWindow
{
    [MenuItem("deecube1/Art/Center Parent to Children")]
    public static void ShowWindow()
    {
        GetWindow<CenterParentToChildrenEditor>("Center Parent to Children");
    }

    private GameObject parentObject;

    void OnGUI()
    {
        GUILayout.Label("Center Parent to Children", EditorStyles.boldLabel);

        parentObject = (GameObject)EditorGUILayout.ObjectField("Parent Object", parentObject, typeof(GameObject), true);

        if (GUILayout.Button("Center Parent"))
        {
            if (parentObject != null)
            {
                CenterParent();
            }
            else
            {
                UnityEngine.Debug.LogWarning("Please assign a parent object.");
            }
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Enable All Children"))
        {
            if (parentObject != null)
            {
                SetChildrenActive(true);
            }
            else
            {
                UnityEngine.Debug.LogWarning("Please assign a parent object.");
            }
        }

        if (GUILayout.Button("Disable All Children"))
        {
            if (parentObject != null)
            {
                SetChildrenActive(false);
            }
            else
            {
                UnityEngine.Debug.LogWarning("Please assign a parent object.");
            }
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Enable LOD1 Children"))
        {
            if (parentObject != null)
            {
                EnableLOD1Children();
            }
            else
            {
                UnityEngine.Debug.LogWarning("Please assign a parent object.");
            }
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Create Parent"))
        {
            CreateParentForSelectedObjects();
        }
    }

    void CenterParent()
    {
        Transform parentTransform = parentObject.transform;
        if (parentTransform.childCount == 0)
        {
            UnityEngine.Debug.LogWarning("No children found under the parent.");
            return;
        }

        Vector3 center = Vector3.zero;
        int childCount = 0;
        Transform[] children = new Transform[parentTransform.childCount];

        // Calculate the center of all children and store them
        for (int i = 0; i < parentTransform.childCount; i++)
        {
            Transform child = parentTransform.GetChild(i);
            center += child.position;
            children[i] = child;
        }

        center /= parentTransform.childCount;

        // Temporarily detach children
        foreach (Transform child in children)
        {
            Undo.SetTransformParent(child, null, "Detach Children");
        }

        // Move the parent to the center of the children
        Undo.RecordObject(parentTransform, "Center Parent");
        parentTransform.position = center;

        // Reattach children
        foreach (Transform child in children)
        {
            Undo.SetTransformParent(child, parentTransform, "Reattach Children");
        }
    }

    void SetChildrenActive(bool active)
    {
        Transform parentTransform = parentObject.transform;
        foreach (Transform child in parentTransform.GetComponentsInChildren<Transform>(true))
        {
            if (child != parentTransform)
            {
                Undo.RecordObject(child.gameObject, active ? "Enable Child" : "Disable Child");
                child.gameObject.SetActive(active);
                EditorUtility.SetDirty(child.gameObject); // Ensure changes are registered
            }
        }
    }

    void EnableLOD1Children()
    {
        Transform parentTransform = parentObject.transform;
        foreach (Transform child in parentTransform.GetComponentsInChildren<Transform>(true))
        {
            if (child != parentTransform && child.gameObject.name.EndsWith("_LOD1"))
            {
                EnableWithParents(child);
            }
        }
    }

    void EnableWithParents(Transform child)
    {
        while (child != null)
        {
            Undo.RecordObject(child.gameObject, "Enable LOD1 Child with Parents");
            child.gameObject.SetActive(true);
            EditorUtility.SetDirty(child.gameObject);
            child = child.parent;
        }
    }

    void CreateParentForSelectedObjects()
    {
        if (Selection.transforms.Length == 0)
        {
            UnityEngine.Debug.LogWarning("No objects selected.");
            return;
        }

        Vector3 center = Vector3.zero;
        foreach (Transform selected in Selection.transforms)
        {
            center += selected.position;
        }
        center /= Selection.transforms.Length;

        GameObject newParent = new GameObject("Bulk Created");
        Undo.RegisterCreatedObjectUndo(newParent, "Create new parent");
        newParent.transform.position = center;

        // Check if we are in Prefab Stage
        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage != null)
        {
            // If in Prefab Stage, set the parent to the prefab's root
            newParent.transform.SetParent(prefabStage.prefabContentsRoot.transform, false);
        }
        else
        {
            // Otherwise, check if all selected objects share the same parent
            Transform commonParent = Selection.transforms[0].parent;
            bool sameParent = true;
            foreach (Transform selected in Selection.transforms)
            {
                if (selected.parent != commonParent)
                {
                    sameParent = false;
                    break;
                }
            }

            if (sameParent && commonParent != null)
            {
                newParent.transform.SetParent(commonParent, false);
            }
            else
            {
                newParent.transform.SetParent(null, false); // Set to root if not the same parent
            }
        }

        // Move all selected objects under the new parent
        foreach (Transform selected in Selection.transforms)
        {
            Undo.SetTransformParent(selected, newParent.transform, "Reparent selected objects");
        }

        UnityEngine.Debug.Log("New parent created with selected objects.");
    }
}
