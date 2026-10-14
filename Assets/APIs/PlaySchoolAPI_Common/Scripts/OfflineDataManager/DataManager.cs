using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//TUmse apna
public class DataManager
{
//     StudentGameData studentGameData;
//     public StudentGameData StudentGameData => studentGameData;


//     // public int GameId;
//     // public int totalLevels;
//     long previousSessionTime;
//     public long PreviousSessionTime => previousSessionTime;
//     float startGameTime;
//     public float StartGameTime => startGameTime;
//     public bool isTesting;

//     public static Action OnDataManagerInitialized;
//     public DataManager() 
//     {
//         StudentGameProgressApi.OnStudentAPIInitialized += OnStudentAPIInitialized;

//      }
//     public DataManager(int GameId, float startGameTime, int maxLevel, bool isTesting)
//     {
//         studentGameData = new StudentGameData();
//         studentGameData.data = new StudentGameData.Data();
//         StudentGameProgressApi.OnStudentAPIInitialized += OnStudentAPIInitialized;

//         studentGameData.data.id = GameId;
//         this.startGameTime = startGameTime;
//         studentGameData.data.totalLevel = maxLevel;
//         this.isTesting = false;
//         Debug.Log("Max Level is" + studentGameData.data.totalLevel);
//     }

//     ~DataManager()
//     {
//         StudentGameProgressApi.OnStudentAPIInitialized -= OnStudentAPIInitialized;

//     }

//     private void OnStudentAPIInitialized()
//     {
//       OnDataManagerInitialized?.Invoke();
//     }

//     #region GameData
//     public void FetchData(Action successCallback = null)
//     {
//         if (isTesting)
//             return;
//         string studentName = "";
// #if PLAYSCHOOL_MAIN
//         studentName = PlayerPrefs.GetString(TMKOCPlaySchoolConstants.currentStudentPlaying);
// #else
//         studentName = TMKOCPlaySchoolConstants.currentStudentName;

// #endif

//         StudentGameProgressApi.Instance.GetStudentByGameId(studentName, studentGameData.data.id,
//              () =>
//              {
//                  Debug.Log("Data fetched from backend");
//                  // StudentGameData.Data tempData = StudentGameProgressApi.Instance.CurrentGameData.data;
//                  previousSessionTime = StudentGameProgressApi.Instance.CurrentGameData.data.timeSpentInSeconds;
//                  studentGameData.data.attempts = StudentGameProgressApi.Instance.CurrentGameData.data.attempts;
//                  studentGameData.data.completedLevel = StudentGameProgressApi.Instance.CurrentGameData.data.completedLevel;
//                  successCallback?.Invoke();
//              });
//         startGameTime = Time.time;
//     }
//     public void SendData(Action successCallback = null)
//     {
//         if (isTesting)
//             return;
//         string studentName = "";
// #if PLAYSCHOOL_MAIN
//         studentName = PlayerPrefs.GetString(TMKOCPlaySchoolConstants.currentStudentPlaying);
// #else
//         studentName = TMKOCPlaySchoolConstants.currentStudentName;

// #endif
//         int star = StudentGameProgressApi.Instance.CalculateStars(studentGameData.data.completedLevel, studentGameData.data.totalLevel);
//         if (studentGameData.data.attempts >= 1)
//             star = 5;
//         long currentSesstionTime = StudentGameProgressApi.Instance.EndGame(startGameTime);
//         currentSesstionTime += previousSessionTime;
//         // Debug.Log("Max Level is" + studentGameData.totalLevel);

//         StudentGameProgressApi.Instance.AddStudentByGameId(studentName,
//                    star, studentGameData.data.completedLevel, studentGameData.data.totalLevel, studentGameData.data.attempts, currentSesstionTime, 10, studentGameData.data.id,
//                     () =>
//                    {
//                        Debug.Log("Data sent Successfully");
//                        successCallback?.Invoke();
//                    });
//     }
//     public void OnLevelCompleted()
//     {
//         studentGameData.data.completedLevel++;
//     }
//     public void OnDecrementLevel()
//     {
//         studentGameData.data.completedLevel--;
//     }
//     /// <summary>
//     /// Call when user has finished all the levels
//     /// </summary>
//     public void SetCompletedLevel(int level)
//     {
//         studentGameData.data.completedLevel = level;
//     }
//     public void OnGameCompleted()
//     {
//         studentGameData.data.attempts++;
//         if (studentGameData.data.attempts >= 1)
//         {
//             studentGameData.data.completedLevel = 0;
//         }
//         SendData();
//     }
//     public void SetMaxLevels(int maxLevels)
//     {
//         studentGameData.data.totalLevel = maxLevels;
//     }

//     public void GoBackToPlaySchool()
//     {

//     }

//     #endregion


}










