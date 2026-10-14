using System.IO;
using UnityEngine;
using System;
using SimpleJSON;
using System.Collections.Generic;


[Serializable]
public class getTheLoadedProgress
{
     public string gameName;
    public int gameId;
    public int completedLevels;
    public int totalLevels;
}


[Serializable]
public class GameCategoryDataSaver
{
    public int gameId;
    public int completedLevels;
    public int totalLevels;
    public int star;
    public int testStar;
    public int score;
}

public  class GameCategoryDataManager
{
    private  static string gameDataPath;
    private  GameCategoryDataSaver _newGameData;
    private  GameCategoryDataSaver _GameDataload;
    public  int GetCompletedLevel => _GameDataload.completedLevels;
    public  int Getstar => _newGameData.star;
    public  int GetLoadedstar => _GameDataload.star;
    public int GetTeststar => _newGameData.testStar;
    public int GetLoadedTeststar => _GameDataload.testStar;
    public int GameId;
     public string GameName;
    public GameCategoryDataManager(int gameId , string gameName)
    {
        if (!PlayerPrefs.HasKey(TMKOCPlaySchoolConstants.currentStudentPlaying))
        {
            PlayerPrefs.SetString(TMKOCPlaySchoolConstants.currentStudentPlaying, "TestData");
        }
        gameDataPath = GetFileName(PlayerPrefs.GetString(TMKOCPlaySchoolConstants.currentStudentPlaying));
         GameName = gameName;
        GameId = gameId;
        LoadSpecificGameData();
     
    }


    public static string GetFileName(string StudentName)
    {
        return Path.Combine(Application.persistentDataPath, $"{StudentName} gamedata.json");
    }
    public static bool fileExistant()
    {
        return (File.Exists(GetFileName(PlayerPrefs.GetString(TMKOCPlaySchoolConstants.currentStudentPlaying))));
    }

    public void SaveTestStar(int answerRight , int totalQuestion)
    {
        if (_newGameData == null)
        {
            _newGameData = new GameCategoryDataSaver();
        }
        int _testStar = CalculateStars(answerRight, totalQuestion);

        if (_testStar > GetLoadedTeststar) 
        {
            if (GetTeststar == 5 || _newGameData.testStar == 5)
            {
                _newGameData.testStar = 5;
            }
            else
            {
                _newGameData.testStar = _testStar;
            }
        }
        else
        {
            _newGameData.testStar = GetLoadedTeststar;
        }
     
        _newGameData.gameId = GameId;

        SaveSpecificGameDataForTest(_newGameData);
    }

    public  void SaveLevel(int levelNum , int totalLevel)
        {
            // Award from the shared save path because many games call this
            // directly instead of HelperGameCategoryDataSaver.
            _ = StarWallet.Balance;
            int previousCompletedLevel = GetCompletedLevel;

            if (_newGameData == null)
            {
                _newGameData = new GameCategoryDataSaver();
            }
     
             int _star = CalculateStars(levelNum , totalLevel);
            if(GetLoadedstar == 5 ||  _newGameData.star == 5)
            {
                _newGameData.star = 5;
                   
            }else{
    
            _newGameData.star = _star;
            }
            _newGameData.completedLevels = levelNum;
            _newGameData.totalLevels = totalLevel;
            _newGameData.gameId = GameId;

            SaveSpecificGameData(_newGameData);
        StarWallet.AwardProgress(
            GameId,
            previousCompletedLevel,
            levelNum,
            totalLevel);
    }

    public int CalculateStars(int completedLevel, int totalLevel)
            {
                float percentage = (float)completedLevel / totalLevel * 100;
                if (percentage >= 20 && percentage < 40) return 1;
                else if (percentage >= 40 && percentage <60) return 2;
                else if (percentage >= 60 && percentage < 80) return 3;
                else if (percentage >= 80 && percentage < 100) return 4;
                else if (percentage == 100) return 5;

                return 0;
            }
    // public  void LoadSpecificGameData()
    // {
    //     string getFileNameWithStudent = GetFileName(PlayerPrefs.GetString(TMKOCPlaySchoolConstants.currentStudentPlaying));
    //     if (File.Exists(getFileNameWithStudent))
    //     {
    //         string jsonData = File.ReadAllText(getFileNameWithStudent);
    //         JSONNode loadedJson = JSON.Parse(jsonData);

    //         if (loadedJson != null)
    //         {
    //             foreach (JSONNode gameData in loadedJson.AsArray)
    //             {
    //                 if (gameData["gameId"].AsInt == GameId)
    //                 {
    //                     _GameDataload = new GameCategoryDataSaver
    //                     {
    //                         gameId = gameData["gameId"].AsInt,
    //                         completedLevels = gameData["completedLevels"].AsInt,
    //                        star = gameData["star"].AsInt,
    //                        testStar = gameData["testStar"].AsInt,
    //                         score = gameData["score"].AsInt
    //                     };
    //                    // GetCompletedLevel = _GameDataload.completedLevels;
    //                     Debug.Log($"Loaded Data for {GameId}");
    //                     return;
    //                 }
    //             }
    //         }
    //           _GameDataload = new GameCategoryDataSaver();
    //         Debug.Log($"No data found for {GameId} ID {GameId}");
    //     }else{
            
    //             _GameDataload = new GameCategoryDataSaver();
    //          Debug.Log($"No File Found in Path a");
    //     }
    // }


    public void LoadSpecificGameData()
    {
        if (!File.Exists(gameDataPath))
        {
            _GameDataload = new GameCategoryDataSaver();
            Debug.Log("No File Found");
            return;
        }

        string jsonData = File.ReadAllText(gameDataPath);
        JSONNode allGameData = JSON.Parse(jsonData);

        if (allGameData != null && allGameData.HasKey(GameName))
        {
            JSONArray gameDataArray = allGameData[GameName].AsArray;
            foreach (JSONNode gameData in gameDataArray)
            {
                if (gameData["gameId"].AsInt == GameId)
                {
                    _GameDataload = new GameCategoryDataSaver
                    {
                        gameId = gameData["gameId"].AsInt,
                        completedLevels = gameData["completedLevels"].AsInt,
                        totalLevels = gameData["totalLevels"].AsInt,
                        star = gameData["star"].AsInt,
                        testStar = gameData["testStar"].AsInt,
                        score = gameData["score"].AsInt
                    };
                    Debug.Log($"Loaded Data for {GameName}, GameId: {GameId}");
                    return;
                }
            }
        }

        _GameDataload = new GameCategoryDataSaver();
        Debug.Log($"No data found for {GameName}, GameId: {GameId}");
    }
public static  List<(int gameId, int completedLevels, int totalLevels , int star , int testStar)> LoadTheCompletedLevel(string gameName)
{
    // Ensure gameDataPath is initialized
   gameDataPath = GetFileName(PlayerPrefs.GetString(TMKOCPlaySchoolConstants.currentStudentPlaying));

    if (!File.Exists(gameDataPath))
    {
       // Debug.Log($"Game data file does not exist at path: {gameName}");
        return new List<(int, int, int , int , int)>();
    }

    // Read JSON data
    string jsonData = File.ReadAllText(gameDataPath);
    JSONNode allGameData = JSON.Parse(jsonData);

    // Check for game name data
    if (allGameData != null && allGameData.HasKey(gameName))
    {
        JSONArray gameDataArray = allGameData[gameName].AsArray;
        List<(int , int , int  , int ,int)> gameDataList = new List<(int, int, int , int ,int)>();

        foreach (JSONNode gameData in gameDataArray)
        {
            int gameId = gameData["gameId"].AsInt;
            int completedLevels = gameData["completedLevels"].AsInt;
            int totalLevels = gameData["totalLevels"].AsInt;
              int star = gameData["star"].AsInt;
             int testStar = gameData["testStar"].AsInt;
//            Debug.Log($"Game Name: {gameName}, GameId: {gameId}, Completed Levels: {completedLevels}, Total Levels: {totalLevels}");
            gameDataList.Add((gameId, completedLevels, totalLevels , star , testStar));
        }

        return gameDataList;
    }
    else
    {
//        Debug.LogWarning($"No data found for {gameName} in the game data file.");
        return new List<(int, int, int , int , int)>();
    }
}

     private  void SaveSpecificGameData(GameCategoryDataSaver newGameData)
    {  JSONNode allGameData = File.Exists(gameDataPath)
            ? JSON.Parse(File.ReadAllText(gameDataPath))
            : new JSONObject();

        if (!allGameData.HasKey(GameName))
        {
            allGameData[GameName] = new JSONArray();
        }

        JSONArray gameDataArray = allGameData[GameName].AsArray;
        bool gameExists = false;

        for (int i = 0; i < gameDataArray.Count; i++)
        {
            if (gameDataArray[i]["gameId"].AsInt == newGameData.gameId)
            {
                gameDataArray[i]["completedLevels"] = newGameData.completedLevels;
                gameDataArray[i]["star"] = newGameData.star;
                gameDataArray[i]["totalLevels"] = newGameData.totalLevels;
                gameDataArray[i]["score"] = newGameData.score;
                gameExists = true;
                Debug.Log("Game Updated Successfully");
                break;
            }
        }

        if (!gameExists)
        {
            JSONObject newGameEntry = new JSONObject
            {
                ["gameId"] = newGameData.gameId,
                ["completedLevels"] = newGameData.completedLevels,
                ["totalLevels"] = newGameData.totalLevels,
                ["star"] = newGameData.star,
                ["testStar"] = newGameData.testStar,
                ["score"] = newGameData.score
            };
            gameDataArray.Add(newGameEntry);
            Debug.Log("Game Added Successfully");
        }

        allGameData[GameName] = gameDataArray;
        File.WriteAllText(gameDataPath, allGameData.ToString());
        Debug.Log("Game data saved or updated!");
    }
     private  void SaveSpecificGameDataForTest(GameCategoryDataSaver newGameData)
    {  JSONNode allGameData = File.Exists(gameDataPath)
            ? JSON.Parse(File.ReadAllText(gameDataPath))
            : new JSONObject();

        if (!allGameData.HasKey(GameName))
        {
            allGameData[GameName] = new JSONArray();
        }

        JSONArray gameDataArray = allGameData[GameName].AsArray;
        bool gameExists = false;

        for (int i = 0; i < gameDataArray.Count; i++)
        {
            if (gameDataArray[i]["gameId"].AsInt == newGameData.gameId)
            {
                
                gameDataArray[i]["testStar"] = newGameData.testStar;
                gameExists = true;
                Debug.Log("Game test added succesfuly Updated Successfully");
                break;
            }
        }

        if (!gameExists)
        {
            JSONObject newGameEntry = new JSONObject
            {
                ["gameId"] = newGameData.gameId,
                ["completedLevels"] = newGameData.completedLevels,
                ["totalLevels"] = newGameData.totalLevels,
                ["star"] = newGameData.star,
                ["testStar"] = newGameData.testStar,
                ["score"] = newGameData.score
            };
            gameDataArray.Add(newGameEntry);
            Debug.Log("Game test Added Successfully");
        }

        allGameData[GameName] = gameDataArray;
        File.WriteAllText(gameDataPath, allGameData.ToString());
        Debug.Log("Game data for test saved or updated!");
    }
}
