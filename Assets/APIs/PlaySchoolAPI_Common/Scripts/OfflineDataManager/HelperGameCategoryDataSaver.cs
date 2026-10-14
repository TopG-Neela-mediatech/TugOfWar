using UnityEngine;

public static class HelperGameCategoryDataSaver
{
    static GameCategoryDataManager gameCategory;
    static UpdateCategoryApiManager updateManager;

    static int totalLevels;
    static bool initialized = false;

    static int GameId;
    static string GameName;

    public static void Init(int _totalLevels)
    {
        if (!PlayerPrefs.HasKey(TMKOCPlaySchoolConstants.currentStudentPlaying))
        {
            PlayerPrefs.SetString(TMKOCPlaySchoolConstants.currentStudentPlaying, "TestData");
        }

        GameId = PlayerPrefs.GetInt("currentGameId");
        GameName = PlayerPrefs.GetString("currentGameName");

        totalLevels = _totalLevels;

        // 🔥 Force reload every time
        gameCategory = new GameCategoryDataManager(GameId, GameName);
        updateManager = new UpdateCategoryApiManager(GameId);

        initialized = true;
    }

    public static int GetStartLevel()
    {
        if (!initialized)
        {
            Debug.LogError("HelperGameCategoryDataSaver not initialized");
            return 0;
        }

        int level = gameCategory.GetCompletedLevel;
        Debug.Log(" indeside "+ level);
        if (level >= totalLevels)
            level = 0;

        return level;
    }

    public static void LevelCompleted(int levelNumber)
    {
        if (!initialized)
        {
            Debug.LogError("HelperGameCategoryDataSaver not initialized");
            return;
        }
        // Save Progress
        gameCategory.SaveLevel(levelNumber, totalLevels);

        // Get Star
        int star = gameCategory.Getstar;

        // Ensure max star
        if (star > 5)
            star = 5;

        updateManager.SetGameDataMore(levelNumber, totalLevels, star);

    }

    public static void AddAttempt()
    {
        if (!initialized)
        {
            Debug.LogError("HelperGameCategoryDataSaver not initialized");
            return;
        }

        updateManager.SetAttemps();
    }
}
