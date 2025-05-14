using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DreamQuest.Art
{
    public class MeshAssetInfoExporter : EditorWindow
    {
        [MenuItem("deecube1/Art/Export Mesh Asset Info")]
        public static void ExportMeshAssetInfo()
        {
            string filePath = EditorUtility.SaveFilePanel("Save Mesh Asset Info", "", "MeshAssetInfo.csv", "csv");
            if (string.IsNullOrEmpty(filePath))
                return;

            List<string> lines = new List<string>();
            lines.Add("Mesh Name,Location,Materials,Shaders");

            string[] guids = AssetDatabase.FindAssets("t:Mesh");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (mesh != null)
                {
                    string meshName = mesh.name;
                    string location = Path.GetDirectoryName(path).Replace("Assets/", "");
                    List<string> materials = new List<string>();
                    List<string> shaders = new List<string>();

                    Material[] meshMaterials = AssetDatabase.LoadAllAssetsAtPath(path)
                        .OfType<Material>()
                        .ToArray();

                    foreach (Material material in meshMaterials)
                    {
                        if (material != null && !materials.Contains(material.name))
                        {
                            materials.Add(material.name);
                            Shader shader = material.shader;
                            if (shader != null && !shaders.Contains(shader.name))
                                shaders.Add(shader.name);
                        }
                    }

                    string materialsStr = string.Join(",", materials.ToArray());
                    string shadersStr = string.Join(",", shaders.ToArray());

                    lines.Add(string.Format("{0},{1},{2},{3}", meshName, location, materialsStr, shadersStr));
                }
            }

            File.WriteAllLines(filePath, lines.ToArray());
            UnityEngine.Debug.Log("Mesh asset info exported to: " + filePath);
        }
    }
}
