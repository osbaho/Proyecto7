using System.IO;
using UnityEditor;
using UnityEngine;

public class FixImportLoop : EditorWindow
{
    [MenuItem("Tools/Fix Import Loop")]
    public static void FixLoop()
    {
        // 1. Delete Ghost Assets (obstacles folder)
        string ghostPath = "Assets/Art/obstacles";
        if (AssetDatabase.IsValidFolder(ghostPath))
        {
            AssetDatabase.DeleteAsset(ghostPath);
            Debug.Log($"Deleted ghost asset: {ghostPath}");
        }

        // 2. Fix Specific Models
        FixModel("Assets/Art/Items/Banana/Banana.fbx");
        FixModel("Assets/Art/Items/Fungus/hongo.fbx");
        FixModel("Assets/Art/Items/turtle shell/Turtle shell.fbx");

        // 3. Cleanup Recursive Folders
        CleanupRecursiveFolders("Assets/Art/Items");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Fix Import Loop Complete. Please check console for remaining errors.");
    }

    private static void FixModel(string path)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer != null)
        {
            bool changed = false;

            // Fix Normals
            if (importer.importNormals != ModelImporterNormals.Calculate)
            {
                importer.importNormals = ModelImporterNormals.Calculate;
                changed = true;
            }

            // Fix Infinite Loop Causes
            if (importer.materialImportMode != ModelImporterMaterialImportMode.None)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                changed = true;
            }
            if (importer.importCameras)
            {
                importer.importCameras = false;
                changed = true;
            }
            if (importer.importLights)
            {
                importer.importLights = false;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
                Debug.Log($"Fixed settings for: {path}");
            }
        }
        else
        {
            Debug.LogWarning($"Could not find model at: {path}");
        }
    }

    private static void CleanupRecursiveFolders(string rootPath)
    {
        string[] folders = Directory.GetDirectories(rootPath, "*.fbm", SearchOption.AllDirectories);
        foreach (string folder in folders)
        {
            try
            {
                // Verify it is a valid directory before deleting
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                    string metaFile = folder + ".meta";
                    if (File.Exists(metaFile))
                    {
                        File.Delete(metaFile);
                    }
                    Debug.Log($"Deleted recursive folder: {folder}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to delete {folder}: {e.Message}");
            }
        }
    }
}
