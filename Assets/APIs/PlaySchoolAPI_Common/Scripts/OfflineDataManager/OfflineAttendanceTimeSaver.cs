using System;
using System.IO;
using SimpleJSON;
using UnityEngine;

[Serializable]
public class OfflineDataManager
{
    public DateTime Date;
    public int TimeInSeconds;
}

public class OfflineAttendanceTimeSaver
{
       private   string attendanceDataPath;
      private  OfflineDataManager offlineDataManager;


        private  float sessionStartTime; // For tracking the session's start time.
    private  float totalSessionTime; // Total time spent in the session.

    public OfflineAttendanceTimeSaver()
    {
        attendanceDataPath = GetFileName(PlayerPrefs.GetString(TMKOCPlaySchoolConstants.currentStudentPlaying));
      
          sessionStartTime = Time.time;
          Debug.Log(" Start OfflineAttendanceTimeSaver ");
        
    }
      public static string GetFileName(string StudentName)
      
    {
        return Path.Combine(Application.persistentDataPath, $"{StudentName} offScreenTime.json");
    }
    public static bool fileExistant()
    {
        return (File.Exists(GetFileName(PlayerPrefs.GetString(TMKOCPlaySchoolConstants.currentStudentPlaying))));
    }


    public   void SaveTimeInJson()
    {
          totalSessionTime = Time.time - sessionStartTime;
        sessionStartTime = Time.time ; 
        
        if (offlineDataManager == null)
        {
            offlineDataManager = new OfflineDataManager();
        }
       

       offlineDataManager.Date = DateTime.Now.Date;
        offlineDataManager.TimeInSeconds =  Mathf.FloorToInt(totalSessionTime);

          string studentKey = PlayerPrefs.GetString(TMKOCPlaySchoolConstants.currentStudentPlaying);

        PlayerPrefs.SetString(studentKey + "_Date", offlineDataManager.Date.ToString());
        PlayerPrefs.SetFloat(studentKey + "_TimeInSeconds", offlineDataManager.TimeInSeconds);
        
        PlayerPrefs.Save();
        Debug.Log("Saved in PlayerPrefs: " + offlineDataManager.Date + " - " + offlineDataManager.TimeInSeconds);

        SaveSpecificTimeSpent(offlineDataManager);
    }


 
    private void SaveSpecificTimeSpent(OfflineDataManager newGameData)
    {
        JSONNode gameDataJson;
        if (File.Exists(attendanceDataPath))
        {
            string jsonData = File.ReadAllText(attendanceDataPath);
            gameDataJson = JSON.Parse(jsonData);
        }
        else
        {
            gameDataJson = new JSONArray();
        }

        JSONArray attendaceDataArray = gameDataJson.AsArray;
        bool gameExists = false;
        if (attendaceDataArray == null) return;

        DateTime parsedDate;
        //string dateFormat = "MM/dd/yyyy"; // Specify the format of the date string
        string targetDateFormat = "yyyy-MM-ddTHH:mm:ss"; // New desired format for saving the date

        for (int i = 0; i < attendaceDataArray.Count; i++)
        {
            string todayDateStr = attendaceDataArray[i]["Date"];

            // Try to parse the date with the old format
            if (DateTime.TryParseExact(todayDateStr, targetDateFormat, null, System.Globalization.DateTimeStyles.None, out parsedDate))
            {
                // Compare only the date part (ignore time)
                if (parsedDate.Date == newGameData.Date.Date)  // Compare only the date part
                {
                    attendaceDataArray[i]["TimeInSeconds"] += newGameData.TimeInSeconds;
                    gameExists = true;
                    Debug.Log("Time Spent Updated Successfully");
                    break;
                }
            }
            else
            {
                Debug.LogWarning($"Invalid date format for TodayDate: {todayDateStr}");
            }
        }

        if (!gameExists)
        {
            // If the date is not found, create a new entry with the new format
            JSONObject newTimeSpentEntry = new JSONObject
            {
                ["Date"] = newGameData.Date.ToString(targetDateFormat),  // Save date in new format
                ["TimeInSeconds"] = newGameData.TimeInSeconds,
            };
            attendaceDataArray.Add(newTimeSpentEntry);
            Debug.Log("Attendance Added Successfully");
        }

        // Save the updated data to the file
        File.WriteAllText(attendanceDataPath, attendaceDataArray.ToString());
        Debug.Log("Attendance data saved or updated!");
    }
}
