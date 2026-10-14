#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
public static class UnifiedTracingRemove 
{
    [MenuItem("Tools/Find Bundle Assignments")]
        static void Find()
        {
            foreach (var path in AssetDatabase.GetAllAssetPaths())
            {
                var importer = AssetImporter.GetAtPath(path);

                if (importer != null && !string.IsNullOrEmpty(importer.assetBundleName))
                {
                    Debug.Log($"{path} -> {importer.assetBundleName}");
                }
            }
        }



        [MenuItem("Tools/Remove UnifiedRayTracing Bundle")]
        static void Remove()
        {
            int removed = 0;

            foreach (var path in AssetDatabase.GetAllAssetPaths())
            {
                var importer = AssetImporter.GetAtPath(path);

                if (importer == null)
                    continue;

                if (importer.assetBundleName == "unifiedraytracing")
                {
                    Debug.Log($"Removing AssetBundle from: {path}");

                    importer.assetBundleName = string.Empty;
                    importer.SaveAndReimport();

                    removed++;
                }
            }

            AssetDatabase.RemoveAssetBundleName("unifiedraytracing", true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Finished. Removed {removed} assignments.");
        }

        [MenuItem("Tools/Open UnifiedRayTracing Folder")]
        public static void OpenFolder()
        {
            string path = "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing";

            Object folder = AssetDatabase.LoadAssetAtPath<Object>(path);

            if (folder == null)
            {
                Debug.LogError("Folder not found.");
                return;
            }

            Selection.activeObject = folder;
            EditorGUIUtility.PingObject(folder);
        }
}
#endif