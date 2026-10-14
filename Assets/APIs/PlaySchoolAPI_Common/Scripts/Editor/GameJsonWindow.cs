#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;


public class GameJsonWindow : EditorWindow
{
    private Vector2 scroll;

    [MenuItem("Game Json/Update Game Json")]
    static void Init()
    {
        GameJsonWindow window = GetWindow<GameJsonWindow>();
        window.titleContent = new GUIContent("Game Data");
        window.Show();

    }
       private string gameName;
    private int gameId;
    private int completedLevel;
    private int maxLevel;
    void OpenThePanel()
    {
        // GUILayout.Label("Game Data Editor", EditorStyles.boldLabel);
         gameName = EditorGUILayout.TextField("Game Name", gameName);
        gameId = EditorGUILayout.IntField("Game ID", gameId);
        completedLevel = EditorGUILayout.IntField("Completed Level", completedLevel);
        maxLevel = EditorGUILayout.IntField("Max Level", maxLevel);

        if (GUILayout.Button("Save/Set Level"))
        {
             SaveLevel(gameId, completedLevel, maxLevel);
        }

        if (GUILayout.Button("Open Persistent Data Folder"))
        {
            OpenPersistentDataFolder();
        }


        if (GUILayout.Button("Delete Game Json"))
        {
            DeleteDataJson();
        }
    }

       private void SaveLevel(int gameId, int levelNum, int totalLevel)
    {
       // GameCategoryDataManager dataManager = new GameCategoryDataManager(gameId);
        GameCategoryDataManager dataManager = new GameCategoryDataManager(gameId , gameName);
       UpdateCategoryApiManager  _recentCategoryData = new UpdateCategoryApiManager(gameId);
        dataManager.SaveLevel(levelNum, totalLevel);
         int _star = dataManager.Getstar;
        if(_star == 5)
        {
            _recentCategoryData.SetGameDataMore(levelNum ,totalLevel , 5);
        }else{
            _recentCategoryData.SetGameDataMore(levelNum ,totalLevel  , _star);
        }
        Debug.Log($"Saved level {levelNum} with total levels {totalLevel}");
    }


    private void OnGUI()
    {


        scroll = EditorGUILayout.BeginScrollView(scroll);

        OpenThePanel();

        EditorGUILayout.EndScrollView();
    }

        private void OpenPersistentDataFolder()
    {
        string gameDataPath = Application.persistentDataPath;
        if (System.IO.Directory.Exists(gameDataPath))
        {
            EditorUtility.RevealInFinder(gameDataPath);
        }
        else
        {
            Debug.LogWarning("Persistent Data Path not found!");
        }
    }
    private void DeleteDataJson()
    {
        if(GameCategoryDataManager.fileExistant() && UpdateCategoryApiManager.fileExistant() )
        {
        System.IO.File.Delete(GameCategoryDataManager.GetFileName(PlayerPrefs.GetString(TMKOCPlaySchoolConstants.currentStudentPlaying)));
     System.IO.File.Delete(UpdateCategoryApiManager.GetFileName(PlayerPrefs.GetString(TMKOCPlaySchoolConstants.currentStudentPlaying)));
              Debug.Log("Json is Deleted!");

        }

        else
        {
            Debug.LogWarning("Json is not avalable!");
        }
    }


}
#endif