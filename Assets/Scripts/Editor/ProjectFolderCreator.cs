#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;


public static class ProjectFolderCreator
{
    [MenuItem("Tools/Project/Create Default Folders")]
    public static void CreateDefaultFolders()
    {
        string[] paths = new string[]
        {
            "Assets/_Project",
            "Assets/_Project/Art",
            "Assets/_Project/Audio",
            "Assets/_Project/Materials",
            "Assets/_Project/Prefabs",
            "Assets/_Project/Scenes",
            "Assets/_Project/Scripts",
            "Assets/_Project/Scripts/Runtime",
            "Assets/_Project/Scripts/Editor",
            "Assets/_Project/Settings",
            "Assets/Samples"
        };


        int created = 0;
        foreach (string p in paths)
        {
            if (Directory.Exists(p) == false)
            {
                Directory.CreateDirectory(p);
                created++;
            }
        }


        AssetDatabase.Refresh();
        Debug.Log($"[ProjectFolderCreator] Created {created} folders (idempotent).");
    }
}
#endif