//using UnityEditor;
//using UnityEngine;
//using Unity.EditorCoroutines.Editor;
//public static class HeroMetadataExporter
//{
//    [MenuItem("Tools/Upload Base Data")]
//    public static void UploadBaseData()
//    {
//        ExportHeroMetadata exporter = null;
//        string[] guids = AssetDatabase.FindAssets("Export Base Data", new[]{ "Assets/Editor" });
//        if (guids.Length > 0)
//        {
//            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
//            exporter = AssetDatabase.LoadAssetAtPath<ExportHeroMetadata>(path);
//        }

//        if (exporter == null)
//        {
//            Debug.LogError("Cant find asset export");
//            return;
//        }
//#if UNITY_EDITOR
//        EditorCoroutineUtility.StartCoroutine(exporter.UploadCoroutineInEditor(), exporter);
//#endif
//    }
//}
