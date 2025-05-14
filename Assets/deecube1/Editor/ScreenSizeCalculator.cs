using UnityEditor;
using UnityEngine;

public class ScreenSizeCalculator : EditorWindow
{
    private GameObject selectedObject;
    private Camera selectedCamera;
    private float screenSizePercentage = 0f;

    [MenuItem("deecube1/Screen Size Calculator")]
    public static void ShowWindow()
    {
        GetWindow<ScreenSizeCalculator>("Screen Size Calculator");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Selected Object", EditorStyles.boldLabel);

        if (Selection.activeGameObject != null)
        {
            selectedObject = Selection.activeGameObject;
            EditorGUILayout.LabelField("Object Name: " + selectedObject.name);
        }
        else
        {
            EditorGUILayout.LabelField("No object selected.");
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Camera Picker", EditorStyles.boldLabel);
        selectedCamera = (Camera)EditorGUILayout.ObjectField("Camera", selectedCamera, typeof(Camera), true);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Calculate Screen Size", EditorStyles.boldLabel);

        if (GUILayout.Button("Calculate Size (Selected Camera)"))
        {
            if (selectedObject != null && selectedCamera != null)
            {
                if (IsVisibleFrom(selectedObject.GetComponent<Renderer>(), selectedCamera))
                {
                    screenSizePercentage = CalculateScreenSizeBasedOnResolution(selectedObject, selectedCamera);
                }
                else
                {
                    screenSizePercentage = 0f;
                    Debug.LogWarning("Object is not visible from the selected camera.");
                }
            }
        }

        if (GUILayout.Button("Calculate Size (Scene Camera)"))
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (selectedObject != null && sceneView != null)
            {
                Camera sceneCamera = sceneView.camera;
                if (IsVisibleFrom(selectedObject.GetComponent<Renderer>(), sceneCamera))
                {
                    screenSizePercentage = CalculateScreenSizeBasedOnResolution(selectedObject, sceneCamera);
                }
                else
                {
                    screenSizePercentage = 0f;
                    Debug.LogWarning("Object is not visible from the Scene View camera.");
                }
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Screen Size (%): " + screenSizePercentage.ToString("F2") + "%");

        Repaint();
    }

    private float CalculateScreenSizeBasedOnResolution(GameObject obj, Camera cam)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogError("Selected object has no Renderer component.");
            return 0f;
        }

        Bounds bounds = renderer.bounds;

        Vector3[] points = new Vector3[8];
        Vector3 c = bounds.center;
        Vector3 e = bounds.extents;

        points[0] = cam.WorldToScreenPoint(c + new Vector3(e.x, e.y, e.z));
        points[1] = cam.WorldToScreenPoint(c + new Vector3(-e.x, e.y, e.z));
        points[2] = cam.WorldToScreenPoint(c + new Vector3(e.x, -e.y, e.z));
        points[3] = cam.WorldToScreenPoint(c + new Vector3(e.x, e.y, -e.z));
        points[4] = cam.WorldToScreenPoint(c + new Vector3(-e.x, -e.y, -e.z));
        points[5] = cam.WorldToScreenPoint(c + new Vector3(e.x, -e.y, -e.z));
        points[6] = cam.WorldToScreenPoint(c + new Vector3(-e.x, e.y, -e.z));
        points[7] = cam.WorldToScreenPoint(c + new Vector3(-e.x, -e.y, e.z));

        Vector3 min = points[0];
        Vector3 max = points[0];
        for (int i = 1; i < points.Length; i++)
        {
            min = Vector3.Min(min, points[i]);
            max = Vector3.Max(max, points[i]);
        }

        float objectWidth = Mathf.Abs(max.x - min.x);
        float objectHeight = Mathf.Abs(max.y - min.y);
        float objectArea = objectWidth * objectHeight;
        float screenArea = Screen.width * Screen.height;

        float percentage = (objectArea / screenArea) * 100f;
        return Mathf.Clamp(percentage, 0f, 100f);
    }

    private bool IsVisibleFrom(Renderer renderer, Camera camera)
    {
        if (renderer == null || camera == null) return false;
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
        return GeometryUtility.TestPlanesAABB(planes, renderer.bounds);
    }
}
