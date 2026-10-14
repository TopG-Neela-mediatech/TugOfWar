using UnityEngine;

public static class HelperGameTestDataSaver
{
    static GameCategoryDataManager gameCategoryManager;
    static UpdateCategoryApiManagerTest updateTestManager;

    static int testId;
    static int gameId;
    static string gameName;

    static bool initialized = false;

    public static void Init()
    {
        if (!PlayerPrefs.HasKey(TMKOCPlaySchoolConstants.currentStudentPlaying))
        {
            PlayerPrefs.SetString(TMKOCPlaySchoolConstants.currentStudentPlaying, "TestData");
        }

        gameId = PlayerPrefs.GetInt("currentGameId");
        testId = PlayerPrefs.GetInt("currentTestId");
        gameName = PlayerPrefs.GetString("currentGameName");

        gameCategoryManager = new GameCategoryDataManager(gameId, gameName);
        updateTestManager = new UpdateCategoryApiManagerTest(testId);

        initialized = true;
    }

    public static void TestFinished(int correctAnswers, int totalQuestions, int score)
    {
        if (!initialized)
        {
            Debug.LogError("HelperGameTestDataSaver not initialized");
            return;
        }

        // Save Test Star in gamedata.json
        gameCategoryManager.SaveTestStar(correctAnswers, totalQuestions);

        int testStar = gameCategoryManager.GetTeststar;

        if (testStar > 5)
            testStar = 5;

        // Save full test data in resentGametest.json
        updateTestManager.SetGameDataMore(score, totalQuestions, testStar);
    }

    public static void Reset()
    {
        initialized = false;
    }
}