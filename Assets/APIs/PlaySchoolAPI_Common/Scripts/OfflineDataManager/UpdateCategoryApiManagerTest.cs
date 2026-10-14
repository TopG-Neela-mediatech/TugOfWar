using System.IO;
using UnityEngine;
using SimpleJSON;
using System;



[Serializable]
public class RecentGameDataTest
{
    public int TestId;
    public int Scores;
    public int TotalQuestions;
    public float TimeSpentInSeconds;
    public int Stars;
    public int Attempts;
    public int Medal;
}


public class UpdateCategoryApiManagerTest 
{
    private static string updatedPlayDataPathTest;

    public RecentGameDataTest _newGamesDataTest;

    public int _testId;
            private float sessionStartTime; // For tracking the session's start time.
    private float totalSessionTime; // Total time spent in the session.
    

   
    public UpdateCategoryApiManagerTest(int testId)
    {
          if(!PlayerPrefs.HasKey(TMKOCPlaySchoolConstants.currentStudentPlaying)){
        PlayerPrefs.SetString(TMKOCPlaySchoolConstants.currentStudentPlaying , "TestData");
        }

        updatedPlayDataPathTest = GetFileName(PlayerPrefs.GetString(TMKOCPlaySchoolConstants.currentStudentPlaying));

        _testId = testId;
            sessionStartTime = Time.time;
    }

    public static string GetFileName(string StudentName)
    {
        return Path.Combine(Application.persistentDataPath, $"{StudentName} resentGametest.json");
    }
    public static bool fileExistant()
    {
        return (File.Exists(GetFileName(PlayerPrefs.GetString(TMKOCPlaySchoolConstants.currentStudentPlaying))));
    }


    public void SetGameDataMore(int scores  ,int  totalQuestions , int testStar)
        {
        
               totalSessionTime = Time.time - sessionStartTime;
        sessionStartTime = Time.time ; // Reset for next calculation.


            if(_newGamesDataTest == null)
            {
                _newGamesDataTest = new RecentGameDataTest();
            }
            int attempts = 1 ;
             _newGamesDataTest.TotalQuestions =totalQuestions;
                  int _medals;

             switch (testStar)
                {
                    case 5:
                        _medals = 1;
                        break;
                    case 4:
                        _medals = 2;
                        break;
                    case 3:
                        _medals = 3;
                        break;
                     default:
                    _medals = 0;
                        break;
                }

                  _newGamesDataTest.Medal = _medals;
               _newGamesDataTest.Stars = testStar;
                _newGamesDataTest.Scores = testStar * 20 ;
                  _newGamesDataTest.TimeSpentInSeconds =Mathf.FloorToInt(totalSessionTime);
   
               _newGamesDataTest.Attempts = attempts;
              _newGamesDataTest.TestId = _testId;

              SaveSpecificGameData(_newGamesDataTest);
        }
         public void SaveSpecificGameData(RecentGameDataTest newGameData)
    {
        JSONNode gameDataJson;

        if (File.Exists(updatedPlayDataPathTest))
        {
            string jsonData = File.ReadAllText(updatedPlayDataPathTest);
            gameDataJson = JSON.Parse(jsonData);
        }
        else
        {
            gameDataJson = new JSONArray();
        }

        bool gameExists = false;


        for (int i = 0; i < gameDataJson.Count; i++)
        {
            if (gameDataJson[i]["TestId"].AsInt == newGameData.TestId)
            {
            
                gameDataJson[i]["Scores"] += newGameData.Scores;
                gameDataJson[i]["TotalQuestions"] = newGameData.TotalQuestions;
                gameDataJson[i]["TimeSpentInSeconds"] += newGameData.TimeSpentInSeconds;
                gameDataJson[i]["Stars"] = newGameData.Stars;
                gameDataJson[i]["Attempts"] += newGameData.Attempts;
                gameDataJson[i]["Medal"] = newGameData.Medal;
                Debug.Log("Test Update Successfully");
                gameExists = true;
                break;
            }
        }

        // If the gameId does not exist, add new data
        if (!gameExists)
        {
            JSONObject newGameEntry = new JSONObject
            {
                ["TestId"] = newGameData.TestId,
                ["Scores"] = newGameData.Scores,
                ["TotalQuestions"] = newGameData.TotalQuestions,
                ["TimeSpentInSeconds"] = newGameData.TimeSpentInSeconds,
                ["Stars"] = newGameData.Stars,
                ["Attempts"] = newGameData.Attempts,
                ["Medal"] = newGameData.Medal
            };
      
                Debug.Log("Test Added Successfully");
            //gameDataJson.(0, newGameEntry); // Insert at first index
            gameDataJson.Add(newGameEntry);
        }

      
        File.WriteAllText(updatedPlayDataPathTest, gameDataJson.ToString());
    }

    public void CallTheApiIfOnline()
    {
          if (Application.internetReachability == NetworkReachability.NotReachable)return;
         if (File.Exists(updatedPlayDataPathTest))
        {
            string jsonData = File.ReadAllText(updatedPlayDataPathTest);
            //StartCoroutine(AddStudentGameByIdApi(jsonData));
        }
    }


}
    // form.AddField("StudentName", StudentName);
    //     form.AddField("Stars", stars.ToString());
    //     form.AddField("Medal", medal.ToString());
    //     form.AddField("Scores", scores.ToString());
    //     form.AddField("Attempts", attempts.ToString());
    //     form.AddField("TotalQuestions", totalQuestions.ToString());
    //     form.AddField("Streak", streak.ToString());
    //     form.AddField("TimeSpentInSeconds", timeSpentInSeconds.ToString());
    //     form.AddField("TestId", TestId.ToString());
