using UnityEngine;

// Qualification is saved separately from the current run's star count.
public static class LevelStarProgress
{
    private const string KeyPrefix = "Adventure.StarProgress.v1.";

    public static int RequiredStars(int totalStars)
    {
        return totalStars > 0 ? (int)((totalStars * 4L + 4L) / 5L) : 0;
    }

    public static bool HasEnoughStars(int collectedStars, int totalStars)
    {
        return totalStars > 0 && collectedStars >= RequiredStars(totalStars);
    }

    public static string Prerequisite(string sceneName)
    {
        if (sceneName == "Kitchen_Adventure") return "Beach";
        if (sceneName == "Bedroom") return "Kitchen_Adventure";
        return null;
    }

    public static bool IsUnlocked(string sceneName)
    {
        string prerequisite = Prerequisite(sceneName);
        return prerequisite == null || PlayerPrefs.GetInt(KeyPrefix + prerequisite, 0) == 1;
    }

    public static void RecordQualification(string sceneName, int collectedStars, int totalStars)
    {
        if (sceneName != "Beach" && sceneName != "Kitchen_Adventure" && sceneName != "Bedroom") return;
        if (!HasEnoughStars(collectedStars, totalStars)) return;
        string key = KeyPrefix + sceneName;
        if (PlayerPrefs.GetInt(key, 0) == 1) return;
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
    }
}