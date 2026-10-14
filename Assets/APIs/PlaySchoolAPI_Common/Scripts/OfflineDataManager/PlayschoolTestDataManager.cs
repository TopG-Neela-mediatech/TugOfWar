using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayschoolTestDataManager
{
    private int m_TotalQuestions;
    private bool isTesting;

    public int TotalQuestions => m_TotalQuestions;

    private int GameId;

    public static Action OnTestDataManagerInitialized;


    public PlayschoolTestDataManager()
    {
      //  StudentGameProgressApi.OnStudentAPIInitialized += OnStudentAPIInitialized;

    }

    private void OnStudentAPIInitialized()
    {
      OnTestDataManagerInitialized?.Invoke();
        
    }

    public PlayschoolTestDataManager(int GameId, int maxTextQuestions, bool isTesting)
    {
        //StudentGameProgressApi.OnStudentAPIInitialized += OnStudentAPIInitialized;
        this.isTesting = isTesting;
        m_TotalQuestions = maxTextQuestions;
        this.GameId = GameId;

    }

    ~PlayschoolTestDataManager()
    {
        //StudentGameProgressApi.OnStudentAPIInitialized -= OnStudentAPIInitialized;

    }

    #region Stars

    public int GetStarsBasedOnAttempt(int attemptNumber, int questionRight)
    {
        float percentage = ((float)questionRight / m_TotalQuestions) * 100;

        switch (attemptNumber)
        {
            case 1:
                if (percentage >= 100)
                    return 5; // 100% correct answers
                else if (percentage >= 80)
                    return 4; // 80% to 99% correct answers
                else if (percentage >= 70)
                    return 3; // 70% to 79% correct answers
                else if (percentage >= 60)
                    return 0; // 60% to 69% correct answers
                else
                    return 0; // Less than 60% correct answers

            case 2:
                if (percentage >= 100)
                    return 4; // 100% correct answers
                else if (percentage >= 80)
                    return 3; // 80% to 99% correct answers
                else if (percentage >= 70)
                    return 2; // 70% to 79% correct answers
                else if (percentage >= 60)
                    return 0; // 60% to 69% correct answers
                else
                    return 0; // Less than 60% correct answers
            case 3:
                if (percentage >= 100)
                    return 3; // 100% correct answers
                else if (percentage >= 80)
                    return 2; // 80% to 99% correct answers
                else if (percentage >= 70)
                    return 1; // 70% to 79% correct answers
                else if (percentage >= 60)
                    return 0; // 60% to 69% correct answers
                else
                    return 0; // Less than 60% correct answers
        }

        return -1;
    }

    public int GetMedalsBasedOnAttempt(int attemptNumber, int questionRight)
    {
        float percentage = ((float)questionRight / m_TotalQuestions) * 100;

        switch (attemptNumber)
        {
            case 1:
                if (percentage >= 100)
                    return 1; // 100% correct answers
                else if (percentage >= 80)
                    return 2; // 80% to 99% correct answers
                else if (percentage >= 70)
                    return 3; // 70% to 79% correct answers
                else if (percentage >= 60)
                    return -1; // 60% to 69% correct answers
                else
                    return -1; // Less than 60% correct answers

            case 2:
                if (percentage >= 100)
                    return 2; // 100% correct answers
                else if (percentage >= 80)
                    return 3; // 80% to 99% correct answers
                else if (percentage >= 70)
                    return -1; // 70% to 79% correct answers
                else if (percentage >= 60)
                    return -1; // 60% to 69% correct answers
                else
                    return -1; // Less than 60% correct answers
            case 3:
                if (percentage >= 100)
                    return 3; // 100% correct answers
                else if (percentage >= 80)
                    return -1; // 80% to 99% correct answers
                else if (percentage >= 70)
                    return -1; // 70% to 79% correct answers
                else if (percentage >= 60)
                    return -1; // 60% to 69% correct answers
                else
                    return -1; // Less than 60% correct answers
        }

        return -1;
    }

    public int GetScore(int questionRight)
    {
        int percentage = (questionRight / m_TotalQuestions) * 100;
        // if(percentage > StudentGameProgressApi.Instance.CurrentGameTestData.data.scores)
        // {
        //     return percentage;
        // }else
        // {
        //  return   StudentGameProgressApi.Instance.CurrentGameTestData.data.scores;
        // }
        return -1;
    }

    public int GetCurrentAttempt()
    {
        return -1;
        //return StudentGameProgressApi.Instance.CurrentGameTestData.data.attempts;
    }

    #endregion

    #region  TestData
    public void FetchTestData(Action successCallback = null)
    {
        if (isTesting)
            return;
        string studentName = "";

#if PLAYSCHOOL_MAIN
        studentName = PlayerPrefs.GetString(TMKOCPlaySchoolConstants.currentStudentPlaying);
#else
        studentName = TMKOCPlaySchoolConstants.currentStudentName;

#endif
        // StudentGameProgressApi.Instance.GetStudentByTestsId(studentName, GameId,
        //          () =>
        //          {
        //              Debug.Log("Test Data Fetched from backend");
        //              // StudentGameData.Data tempData = StudentGameProgressApi.Instance.CurrentGameData.data;
        //              successCallback?.Invoke();
        //          });


    }



    public void SendTestData(int correctAnswer, Action successCallback = null)
    {
        if (isTesting)
            return;
        string studentName = "";
#if PLAYSCHOOL_MAIN
        studentName = PlayerPrefs.GetString(TMKOCPlaySchoolConstants.currentStudentPlaying);
#else
        studentName = TMKOCPlaySchoolConstants.currentStudentName;

#endif
        // StudentGameProgressApi.Instance.AddStudentByTestsId(studentName, GetStarsBasedOnAttempt(GetCurrentAttempt(), correctAnswer), GetMedalsBasedOnAttempt(GetCurrentAttempt(),correctAnswer), GetScore(correctAnswer),
        // GetCurrentAttempt()+1, m_TotalQuestions, 99,
        // 100, GameId,
        // () =>
        // {
        //     Debug.Log("Test Data Send successfully");
        //     successCallback?.Invoke();
        // });
    }



    #endregion
}


