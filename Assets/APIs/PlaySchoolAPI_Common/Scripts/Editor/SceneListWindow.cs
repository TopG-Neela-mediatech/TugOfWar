#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class SceneListWindow : EditorWindow
{
    private Vector2 scroll;
    private List<string> scenePaths = new List<string>();
    private List<bool> sceneLoaded = new List<bool>();

    [MenuItem("Tools/Scene List")]
    static void Init()
    {
        SceneListWindow window = GetWindow<SceneListWindow>();
        window.titleContent = new GUIContent("Scene List");
        window.LoadScenes();
        window.Show();
    }

    void LoadScenes()
    {
        scenePaths.Clear();
        sceneLoaded.Clear();
        for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
        {
            var scene = EditorBuildSettings.scenes[i];
            scenePaths.Add(scene.path);
            sceneLoaded.Add(SceneManager.GetSceneByPath(scene.path).isLoaded);
        }
    }

    private void OnGUI()
    {
        if (scenePaths.Count == 0)
        {
            GUILayout.Label("No Scenes in Build Settings");
            return;
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);

        for (int i = 0; i < scenePaths.Count; i++)
        {
            EditorGUILayout.BeginHorizontal(GUI.skin.button);
            if (GUILayout.Button(System.IO.Path.GetFileNameWithoutExtension(scenePaths[i])))
            {
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                EditorSceneManager.OpenScene(scenePaths[i], OpenSceneMode.Single);
            }

              bool isSceneLoaded = SceneManager.GetSceneByPath(scenePaths[i]).isLoaded;
                bool isActiveScene = SceneManager.GetActiveScene().path.Equals(scenePaths[i]);

            if (!isSceneLoaded && !isActiveScene)
            {
                if (GUILayout.Button("+", GUILayout.Width(40)))
                {
                    EditorSceneManager.OpenScene(scenePaths[i], OpenSceneMode.Additive);
                    sceneLoaded[i] = true;
                }
            }
           
            else if (isSceneLoaded && !isActiveScene)
                {
                    if (GUILayout.Button("-", GUILayout.Width(40)))
                    {
                        SceneManager.UnloadSceneAsync(SceneManager.GetSceneByPath(scenePaths[i]));
                        sceneLoaded[i] = false;
                    }
                }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private void OnEnable()
    {
        LoadScenes();
    }

    private void OnFocus()
    {
        LoadScenes();
    }
}
#endif