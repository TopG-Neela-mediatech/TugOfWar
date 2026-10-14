using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SimpleJSON;
using UnityEngine;

public enum StarChangeReason
{
    Migration,
    StarterGrant,
    LevelCompleted,
    GameCompleted,
    DailyReward,
    PlayroomPurchase,
    Other
}

/// <summary>
/// The single source of truth for spendable stars. Game progress/rating data
/// remains in GameCategoryDataManager and must not be used as a balance.
/// </summary>
public static class StarWallet
{
    [Serializable]
    private sealed class WalletData
    {
        public int version = CurrentVersion;
        public int balance;
        public List<string> processedTransactions = new List<string>();
    }

    private const int CurrentVersion = 2;
    private const int DefaultStarterStars = 500;
    private const string LegacyOwnerKey = "UnifiedStarLegacyOwner";

    private static WalletData data;
    private static string loadedProfile;
    private static int starterStars = DefaultStarterStars;

    public static event Action<int, int, StarChangeReason> BalanceChanged;

    // Analytics lives in Assembly-CSharp, which this assembly cannot reference.
    // The wallet reports; whoever cares subscribes.
    public static event Action<int, StarChangeReason> StarAwarded;
    public static event Action<string, int, int, int> StarAwardSkipped;

    public static int Balance
    {
        get
        {
            EnsureLoaded();
            return Mathf.Max(0, data.balance);
        }
    }

    public static string CurrentProfile => ResolveProfile();

    public static void ConfigureStarterStars(int amount)
    {
        starterStars = Mathf.Max(0, amount);
    }

    public static bool Credit(
        int amount,
        string transactionId,
        StarChangeReason reason)
    {
        if (amount <= 0 || string.IsNullOrWhiteSpace(transactionId))
            return false;

        EnsureLoaded();
        if (HasProcessedInternal(transactionId))
            return false;

        int previous = data.balance;
        data.balance = Mathf.Max(0, data.balance + amount);
        Remember(transactionId);
        if (!Save())
        {
            data.balance = previous;
            data.processedTransactions.Remove(transactionId);
            return false;
        }
        BalanceChanged?.Invoke(previous, data.balance, reason);
        StarAwarded?.Invoke(amount, reason);
        return true;
    }

    public static bool IncreaseActivityStars(int amount, int level = 0)
    {
        int gameId = PlayerPrefs.GetInt("currentGameId", 0);
        return Credit(
            amount,
            $"activity:{gameId}:level:{level}",
            StarChangeReason.LevelCompleted);
    }

    public static bool TrySpend(
        int amount,
        string transactionId,
        StarChangeReason reason = StarChangeReason.PlayroomPurchase)
    {
        if (amount < 0 || string.IsNullOrWhiteSpace(transactionId))
            return false;

        EnsureLoaded();
        if (HasProcessedInternal(transactionId))
            return true;
        if (data.balance < amount)
            return false;

        int previous = data.balance;
        data.balance -= amount;
        Remember(transactionId);
        if (!Save())
        {
            data.balance = previous;
            data.processedTransactions.Remove(transactionId);
            return false;
        }
        BalanceChanged?.Invoke(previous, data.balance, reason);
        return true;
    }

    public static int AwardProgress(
        int gameId,
        int previousCompletedLevel,
        int completedLevel,
        int totalLevels)
    {
        if (gameId <= 0 || totalLevels <= 0)
        {
            // Silent before: the two commonest ways a game earns nothing.
            LogAwardSkipped(
                gameId <= 0 ? "zero_game_id" : "zero_total_levels",
                gameId,
                previousCompletedLevel,
                completedLevel);
            return 0;
        }

        int previous = Mathf.Clamp(previousCompletedLevel, 0, totalLevels);
        int current = Mathf.Clamp(completedLevel, 0, totalLevels);
        int awarded = 0;

        for (int level = previous + 1; level <= current; level++)
        {
            if (Credit(
                    1,
                    $"game:{gameId}:level:{level}",
                    StarChangeReason.LevelCompleted))
            {
                awarded++;
            }
        }

        if (previous < totalLevels && current >= totalLevels &&
            Credit(
                5,
                $"game:{gameId}:completed",
                StarChangeReason.GameCompleted))
        {
            awarded += 5;
        }

        if (awarded == 0)
        {
            LogAwardSkipped(
                previous >= current ? "no_new_levels" : "already_processed",
                gameId,
                previous,
                current);
        }

        return awarded;
    }

    private static void LogAwardSkipped(
        string reason,
        int gameId,
        int previousLevel,
        int completedLevel)
    {
        StarAwardSkipped?.Invoke(
            reason, gameId, previousLevel, completedLevel);
    }

    public static bool HasProcessed(string transactionId)
    {
        EnsureLoaded();
        return HasProcessedInternal(transactionId);
    }

    public static void ReloadForCurrentProfile()
    {
        data = null;
        loadedProfile = null;
        EnsureLoaded();
    }

    public static void ResetToStarterStars()
    {
        EnsureLoaded();
        int previous = data.balance;
        data = new WalletData { balance = starterStars };
        Save();
        BalanceChanged?.Invoke(previous, data.balance, StarChangeReason.StarterGrant);
    }

    private static void EnsureLoaded()
    {
        string profile = ResolveProfile();
        if (data != null && string.Equals(
                loadedProfile, profile, StringComparison.Ordinal))
        {
            return;
        }

        loadedProfile = profile;
        data = null;

        string path = GetWalletPath(profile);
        if (File.Exists(path))
        {
            try
            {
                data = JsonUtility.FromJson<WalletData>(File.ReadAllText(path));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[StarWallet] Could not load wallet: {exception.Message}");
            }
        }

        if (data == null)
        {
            data = BuildMigratedWallet(profile);
            Save();
        }

        data.processedTransactions ??= new List<string>();
        data.balance = Mathf.Max(0, data.balance);
        if (data.version < 2)
        {
            var recordedTransactions = new List<string>();
            CalculateRecordedGameplayStars(profile, recordedTransactions);
            foreach (string transaction in recordedTransactions)
            {
                if (!data.processedTransactions.Contains(transaction))
                    data.processedTransactions.Add(transaction);
            }
        }

        data.version = CurrentVersion;
        Save();
    }

    private static WalletData BuildMigratedWallet(string profile)
    {
        var processedTransactions = new List<string>();
        int gameplayStars = CalculateRecordedGameplayStars(
            profile,
            processedTransactions);
        int balance = gameplayStars + starterStars;

        string legacyOwner = PlayerPrefs.GetString(LegacyOwnerKey, string.Empty);
        if (string.IsNullOrEmpty(legacyOwner))
        {
            legacyOwner = profile;
            PlayerPrefs.SetString(LegacyOwnerKey, profile);
            PlayerPrefs.Save();
        }

        if (string.Equals(legacyOwner, profile, StringComparison.Ordinal))
        {
            balance += PlayerPrefs.GetInt("RewardedStar", 0);
            balance -= PlayerPrefs.GetInt("PlayroomSpentStars", 0);
            balance -= ReadNewPlayroomSpentStars();
        }

        return new WalletData
        {
            version = CurrentVersion,
            balance = Mathf.Max(0, balance),
            processedTransactions = processedTransactions
        };
    }

    private static int CalculateRecordedGameplayStars(
        string profile,
        ICollection<string> processedTransactions)
    {
        var progress = new Dictionary<int, (int completed, int total)>();

        string categoryPath = Path.Combine(
            Application.persistentDataPath,
            $"{profile} gamedata.json");
        if (File.Exists(categoryPath))
        {
            try
            {
                JSONNode root = JSON.Parse(File.ReadAllText(categoryPath));
                if (root != null)
                {
                    foreach (KeyValuePair<string, JSONNode> category in root)
                    {
                        foreach (JSONNode game in category.Value.AsArray)
                        {
                            MergeProgress(
                                progress,
                                game["gameId"].AsInt,
                                game["completedLevels"].AsInt,
                                game["totalLevels"].AsInt);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[StarWallet] Could not migrate game progress: {exception.Message}");
            }
        }

        string recentPath = Path.Combine(
            Application.persistentDataPath,
            $"{profile} resentGameData.dat");
        if (File.Exists(recentPath))
        {
            try
            {
                string encrypted = File.ReadAllText(recentPath);
                JSONNode recent = JSON.Parse(EncyptedDecryptedData.Decrypt(encrypted));
                foreach (JSONNode game in recent.AsArray)
                {
                    MergeProgress(
                        progress,
                        game["GameId"].AsInt,
                        game["CompletedLevel"].AsInt,
                        game["TotalLevel"].AsInt);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[StarWallet] Could not migrate recent progress: {exception.Message}");
            }
        }

        int total = 0;
        foreach (KeyValuePair<int, (int completed, int total)> game in progress)
        {
            int savedCompleted = game.Value.completed;
            int levels = game.Value.total;
            int completed = Mathf.Clamp(
                savedCompleted,
                0,
                Mathf.Max(0, levels));
            total += completed;

            for (int level = 1; level <= completed; level++)
                processedTransactions.Add($"game:{game.Key}:level:{level}");

            if (levels > 0 && completed >= levels)
            {
                total += 5;
                processedTransactions.Add($"game:{game.Key}:completed");
            }
        }

        return Mathf.Max(0, total);
    }

    private static void MergeProgress(
        IDictionary<int, (int completed, int total)> progress,
        int gameId,
        int completed,
        int total)
    {
        if (gameId <= 0 || total <= 0)
            return;

        completed = Mathf.Clamp(completed, 0, total);
        if (!progress.TryGetValue(gameId, out var previous))
        {
            progress[gameId] = (completed, total);
            return;
        }

        progress[gameId] = (
            Mathf.Max(previous.completed, completed),
            Mathf.Max(previous.total, total));
    }

    private static int ReadNewPlayroomSpentStars()
    {
        string path = Path.Combine(Application.persistentDataPath, "PlayroomSave.json");
        if (!File.Exists(path))
            return 0;

        try
        {
            return Mathf.Max(0, JSON.Parse(File.ReadAllText(path))["spentStars"].AsInt);
        }
        catch
        {
            return 0;
        }
    }

    private static bool HasProcessedInternal(string transactionId)
    {
        return data.processedTransactions != null &&
               data.processedTransactions.Contains(transactionId);
    }

    private static void Remember(string transactionId)
    {
        data.processedTransactions ??= new List<string>();
        data.processedTransactions.Add(transactionId);
    }

    private static bool Save()
    {
        string path = GetWalletPath(loadedProfile);
        string temporaryPath = path + ".tmp";

        try
        {
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(data));
            if (File.Exists(path))
            {
                File.Copy(temporaryPath, path, true);
                File.Delete(temporaryPath);
            }
            else
            {
                File.Move(temporaryPath, path);
            }

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[StarWallet] Could not save wallet: {exception.Message}");
            return false;
        }
    }

    private static string GetWalletPath(string profile)
    {
        return Path.Combine(
            Application.persistentDataPath,
            $"StarWallet_{SafeFilePart(profile)}.json");
    }

    private static string ResolveProfile()
    {
        string profile = PlayerPrefs.GetString(
            TMKOCPlaySchoolConstants.currentStudentPlaying,
            "TestData");
        return string.IsNullOrWhiteSpace(profile) ? "TestData" : profile.Trim();
    }

    private static string SafeFilePart(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (char character in value)
        {
            builder.Append(Array.IndexOf(Path.GetInvalidFileNameChars(), character) >= 0
                ? '_'
                : character);
        }

        return builder.ToString();
    }
}
