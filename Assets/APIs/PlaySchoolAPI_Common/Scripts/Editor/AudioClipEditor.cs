using UnityEditor;
using UnityEngine;

public class AudioClipEditor : EditorWindow
{
    private bool forceToMono = false;
    private int quality = 100;

    [MenuItem("Tools/Audio Clip Editor")]
    public static void ShowWindow()
    {
        GetWindow<AudioClipEditor>("Audio Clip Editor");
    }

    private void OnGUI()
    {
        GUILayout.Label("Audio Clip Settings", EditorStyles.boldLabel);

        forceToMono = EditorGUILayout.Toggle("Force to Mono", forceToMono);
       // normalize = EditorGUILayout.Toggle("Normalize", normalize);
        quality = EditorGUILayout.IntSlider("Quality", quality, 1, 100);

        if (GUILayout.Button("Apply to All Audio Clips"))
        {
            ApplySettingsToAllAudioClips();
        }
    }

    private void ApplySettingsToAllAudioClips()
    {
        string[] guids = AssetDatabase.FindAssets("t:AudioClip");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;

            if (importer != null)
            {
                importer.forceToMono = forceToMono;
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.CompressedInMemory;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = quality / 100f;
                //settings.normalize = normalize;
                importer.defaultSampleSettings = settings;

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Applied settings to all audio clips.");
    }
}
