using System;
using UnityEngine;

/// <summary>Persistent player profile: wallet, best score and purchased upgrade levels.</summary>
[Serializable]
public class SaveData
{
    public int coins;
    public int bestScore;
    public int totalRuns;
    public int[] upgradeLevels = new int[UpgradeCatalog.Count];
    public bool soundEnabled = true;
}

public static class SaveSystem
{
    private const string PrefKey = "flappy_save_v1";

    public static SaveData Data { get; private set; } = new SaveData();

    public static event Action DataChanged;

    public static void Load()
    {
        Data = new SaveData();
        try
        {
            if (PlayerPrefs.HasKey(PrefKey))
            {
                var loaded = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(PrefKey));
                if (loaded != null)
                {
                    Data = loaded;
                    // heal array size if UpgradeCatalog grows between versions
                    if (Data.upgradeLevels == null || Data.upgradeLevels.Length != UpgradeCatalog.Count)
                    {
                        var old = Data.upgradeLevels;
                        Data.upgradeLevels = new int[UpgradeCatalog.Count];
                        if (old != null)
                            Array.Copy(old, Data.upgradeLevels, Math.Min(old.Length, Data.upgradeLevels.Length));
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"SaveSystem: failed to read save ({e.Message}); starting fresh.");
            Data = new SaveData();
        }
    }

    public static void Save()
    {
        PlayerPrefs.SetString(PrefKey, JsonUtility.ToJson(Data));
        PlayerPrefs.Save();
        DataChanged?.Invoke();
    }

    public static int GetUpgradeLevel(int index) =>
        Data.upgradeLevels != null && index >= 0 && index < Data.upgradeLevels.Length ? Data.upgradeLevels[index] : 0;

    public static void SetUpgradeLevel(int index, int level)
    {
        if (index < 0 || index >= Data.upgradeLevels.Length) return;
        Data.upgradeLevels[index] = level;
        Save();
    }

    public static void AddCoins(int amount)
    {
        Data.coins = Mathf.Max(0, Data.coins + amount);
        Save();
    }

    public static bool TrySpend(int amount)
    {
        if (Data.coins < amount) return false;
        Data.coins -= amount;
        Save();
        return true;
    }

    public static void RecordRun(int score)
    {
        Data.totalRuns++;
        if (score > Data.bestScore) Data.bestScore = score;
        Save();
    }
}
