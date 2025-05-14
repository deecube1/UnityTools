using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Formats.Fbx.Exporter; // Requires FBX Exporter package

public class BreakMeshByMaterial : EditorWindow
{
    [MenuItem("deecube1/Art/Break Mesh and Export FBX")]
    private static void BreakSelectedMesh()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogError("No object selected. Please select a GameObject with a MeshRenderer.");
            return;
        }

        MeshFilter meshFilter = selected.GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = selected.GetComponent<MeshRenderer>();

        if (meshFilter == null || meshRenderer == null)
        {
            Debug.LogError("Selected object must have a MeshFilter and MeshRenderer.");
            return;
        }

        Mesh originalMesh = meshFilter.sharedMesh;
        if (originalMesh == null)
        {
            Debug.LogError("Selected object has no valid mesh.");
            return;
        }

        // Ask user for an export folder outside Unity
        string selectedFolder = EditorUtility.SaveFolderPanel("Select Folder to Export FBX Files", "", "");
        if (string.IsNullOrEmpty(selectedFolder))
        {
            Debug.LogWarning("Export canceled. No folder selected.");
            return;
        }

        Material[] materials = meshRenderer.sharedMaterials;
        Dictionary<Material, List<int>> subMeshTriangles = new Dictionary<Material, List<int>>();

        // Group triangles by material
        for (int i = 0; i < originalMesh.subMeshCount; i++)
        {
            Material mat = materials[i];
            int[] indices = originalMesh.GetTriangles(i);

            if (!subMeshTriangles.ContainsKey(mat))
                subMeshTriangles[mat] = new List<int>();

            subMeshTriangles[mat].AddRange(indices);
        }

        List<string> savedFiles = new List<string>();
        int partNumber = 1;

        foreach (var entry in subMeshTriangles)
        {
            List<int> triangles = entry.Value;

            Mesh newMesh = new Mesh
            {
                vertices = originalMesh.vertices,
                normals = originalMesh.normals,
                uv = originalMesh.uv
            };

            newMesh.triangles = triangles.ToArray();
            newMesh.RecalculateBounds();

            // Create sequential naming
            string fbxFileName = $"{selected.name}_Part{partNumber}.fbx";
            string fbxPath = Path.Combine(selectedFolder, fbxFileName);

            SaveMeshAsFbx(newMesh, fbxPath);
            savedFiles.Add(fbxPath);

            partNumber++;
        }

        Debug.Log("Meshes successfully exported as FBX:\n" + string.Join("\n", savedFiles));
    }

    private static void SaveMeshAsFbx(Mesh mesh, string path)
    {
        GameObject tempObject = new GameObject("TempMeshObject");
        MeshFilter mf = tempObject.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        ModelExporter.ExportObject(path, tempObject);
        GameObject.DestroyImmediate(tempObject);

        Debug.Log($"Saved FBX: {path}");
    }
}
