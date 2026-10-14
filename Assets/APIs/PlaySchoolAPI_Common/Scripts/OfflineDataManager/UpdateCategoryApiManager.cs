using System;

using System.IO;

using UnityEngine;

using SimpleJSON;
using System.Collections.Generic;
[Serializable]
public class RecentGameData
{
    public int GameId;
    public int Score;
    public int TotalLevel;
    public int CompletedLevel;
    public float TimeSpentInSeconds;
    public int Stars;
    public int Attempts;
}



public class UpdateCategoryApiManager 
{

   
    private static string updatedPlayDataPath;

    public RecentGameData _newGamesData;

    public int gameId;
        private float sessionStartTime; // For tracking the session's start time.
    private float totalSessionTime; // Total time spent in the session.

    //private const string EncryptionKey = "suryathebosslengendpubgbot";

    public UpdateCategoryApiManager(int _gameId)
    {

          if(!PlayerPrefs.HasKey(TMKOCPlaySchoolConstants.currentStudentPlaying)){
        PlayerPrefs.SetString(TMKOCPlaySchoolConstants.currentStudentPlaying , "TestData");
        }

          if(_newGamesData == null)
        {
            _newGamesData = new RecentGameData();
        }

        updatedPlayDataPath = GetFileName(PlayerPrefs.GetString(TMKOCPlaySchoolConstants.currentStudentPlaying));
        gameId = _gameId;
             sessionStartTime = Time.time;
     
    }
    public static string GetFileName(string StudentName)
    {
        return Path.Combine(Application.persistentDataPath, $"{StudentName} resentGameData.dat");
    }
    public static bool fileExistant()
    {
        return (File.Exists(GetFileName(PlayerPrefs.GetString(TMKOCPlaySchoolConstants.currentStudentPlaying))));
    }

     public void SetAttemps()
    {
       
        Debug.Log("as set attemps " );
         _newGamesData.GameId = gameId;
     
        SaveSpecificAttemps(_newGamesData);

    }

    public void SetGameDataMore(int levelNum  ,int  totalLevel , int star)
        {
        
            
                  
        totalSessionTime = Time.time - sessionStartTime;
        sessionStartTime = Time.time ; // Reset for next calculation.

            if(_newGamesData == null)
            {
                _newGamesData = new RecentGameData();
            }
       
      
         
      
                    _newGamesData.CompletedLevel = levelNum;
                _newGamesData.TotalLevel = totalLevel;
                _newGamesData.Stars = star;

        
          
                  _newGamesData.TimeSpentInSeconds = Mathf.FloorToInt(totalSessionTime);
   
               //_newGamesData.Attempts = attempts;
              _newGamesData.GameId = gameId;

              SaveSpecificGameData(_newGamesData);
        }
 
   public void SaveSpecificGameData(RecentGameData newGameData)
    {
        JSONNode gameDataJson;

        if (File.Exists(updatedPlayDataPath))
        {
          //  string jsonData = File.ReadAllText(updatedPlayDataPath);
             string encryptedData = File.ReadAllText(updatedPlayDataPath);
            string jsonData = EncyptedDecryptedData.Decrypt(encryptedData);
            gameDataJson = JSON.Parse(jsonData);
        }
        else
        {
            gameDataJson = new JSONArray();
        }

        bool gameExists = false;


        for (int i = 0; i < gameDataJson.Count; i++)
        {
            if (gameDataJson[i]["GameId"].AsInt == newGameData.GameId)
            {
           
                gameDataJson[i]["TotalLevel"] = newGameData.TotalLevel;
                gameDataJson[i]["CompletedLevel"] = newGameData.CompletedLevel;
                gameDataJson[i]["TimeSpentInSeconds"] += newGameData.TimeSpentInSeconds;
                gameDataJson[i]["Stars"] = newGameData.Stars;
                gameDataJson[i]["Attempts"] += newGameData.Attempts;
                gameDataJson[i]["Score"] += newGameData.Score;
                Debug.Log("Update The  data");
                gameExists = true;
                break;
            }
        }

        // If the gameId does not exist, add new data
        if (!gameExists)
        {
            JSONObject newGameEntry = new JSONObject
            {
                ["GameId"] = newGameData.GameId,
                ["TotalLevel"] = newGameData.TotalLevel,
                ["CompletedLevel"] = newGameData.CompletedLevel,
                ["TimeSpentInSeconds"] = newGameData.TimeSpentInSeconds,
                ["Stars"] = newGameData.Stars,
                ["Attempts"] = newGameData.Attempts,
                ["Score"] = newGameData.Score
            };
            //gameDataJson.(0, newGameEntry); // Insert at first index
            gameDataJson.Add(newGameEntry);
        }

              string encryptedJson = EncyptedDecryptedData.Encrypt(gameDataJson.ToString());
        File.WriteAllText(updatedPlayDataPath, encryptedJson);
    }

         private void SaveSpecificAttemps(RecentGameData newGameData)
    {
        JSONNode gameDataJson;

        if (File.Exists(updatedPlayDataPath))
        {
          string encryptedData = File.ReadAllText(updatedPlayDataPath);
            string jsonData = EncyptedDecryptedData.Decrypt(encryptedData);
            gameDataJson = JSON.Parse(jsonData);
        }
        
        else
        {
            gameDataJson = new JSONArray();
        }

        bool gameExists = false;


        for (int i = 0; i < gameDataJson.Count; i++)
        {
            if (gameDataJson[i]["GameId"].AsInt == newGameData.GameId)
            {
           
                
                gameDataJson[i]["Attempts"] += 1;
          
                gameExists = true;
                break;
            }
        }

        // If the gameId does not exist, add new data
        if (!gameExists)
        {
            JSONObject newGameEntry = new JSONObject
            {
                  ["GameId"] = newGameData.GameId,
                ["TotalLevel"] = newGameData.TotalLevel,
                ["CompletedLevel"] = newGameData.CompletedLevel,
                ["TimeSpentInSeconds"] = newGameData.TimeSpentInSeconds,
                ["Stars"] = newGameData.Stars,
                ["Attempts"] = 1,
                ["Score"] = newGameData.Score
            };
        
            gameDataJson.Add(newGameEntry);
        }

          
              string encryptedJson = EncyptedDecryptedData.Encrypt(gameDataJson.ToString());
             File.WriteAllText(updatedPlayDataPath, encryptedJson);
    }

     
          public static List<RecentGameData> LoadAllGamePlayData()
        {
            string filePath = GetFileName(PlayerPrefs.GetString(TMKOCPlaySchoolConstants.currentStudentPlaying));

            List<RecentGameData> gameDataList = new List<RecentGameData>();

            if (!File.Exists(filePath))
                return gameDataList;

            string encryptedData = File.ReadAllText(filePath);
            string jsonData = EncyptedDecryptedData.Decrypt(encryptedData);

            JSONNode jsonNode = JSON.Parse(jsonData);

            if (jsonNode == null)
                return gameDataList;

            foreach (JSONNode game in jsonNode.AsArray)
            {
                RecentGameData data = new RecentGameData
                {
                    GameId = game["GameId"].AsInt,
                    TotalLevel = game["TotalLevel"].AsInt,
                    CompletedLevel = game["CompletedLevel"].AsInt,
                    Stars = game["Stars"].AsInt,
                    Attempts = game["Attempts"].AsInt,
                    Score = game["Score"].AsInt,
                    TimeSpentInSeconds = game["TimeSpentInSeconds"].AsFloat
                };

                gameDataList.Add(data);
            }

            return gameDataList;
        }




}

