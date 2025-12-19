using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor utility to recalculate normals for all imported FBX models in the project.
/// Use: Assets → Fix All Mesh Normals
/// </summary>
public class FixMeshNormals : Editor
{
    [MenuItem("Assets/Fix All Mesh Normals")]
    public static void RecalculateAllModelNormals()
    {
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/Art" });
        int fixedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;

            if (importer != null && importer.importNormals != ModelImporterNormals.Calculate)
            {
                importer.importNormals = ModelImporterNormals.Calculate;
                importer.importTangents = ModelImporterTangents.CalculateMikk;
                importer.SaveAndReimport();
                fixedCount++;
                Debug.Log($"[FixMeshNormals] Fixed: {path}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[FixMeshNormals] Recalculated normals for {fixedCount} models.");
    }
}
