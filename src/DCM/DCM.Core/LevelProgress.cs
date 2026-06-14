using System.Collections.Generic;

namespace DCM.Core;

public static class LevelProgress
{
    private static readonly HashSet<int> _unlocked = new() { 0 };
    private static readonly float[] _bestTimes;

    static LevelProgress()
    {
        _bestTimes = new float[World.Map.LevelCount];
        System.Array.Fill(_bestTimes, float.MaxValue);
    }

    public static bool IsUnlocked(int levelIndex) => _unlocked.Contains(levelIndex);

    public static void Unlock(int levelIndex)
    {
        if (levelIndex >= 0 && levelIndex < World.Map.LevelCount)
        {
            _unlocked.Add(levelIndex);
            SaveManager.Save();
        }
    }

    public static float GetBestTime(int levelIndex) => _bestTimes[levelIndex];
    public static bool HasBestTime(int levelIndex) => _bestTimes[levelIndex] < float.MaxValue;

    public static void RecordTime(int levelIndex, float seconds)
    {
        if (levelIndex >= 0 && levelIndex < _bestTimes.Length && seconds < _bestTimes[levelIndex])
        {
            _bestTimes[levelIndex] = seconds;
            SaveManager.Save();
        }
    }

    public static string FormatTime(float seconds)
    {
        if (seconds >= float.MaxValue) return "--:--.--";
        var m = (int)seconds / 60;
        var s = (int)seconds % 60;
        var t = (int)(seconds % 1f * 10f);
        return $"{m}:{s:D2}.{t}";
    }

    public static void Reset()
    {
        _unlocked.Clear();
        _unlocked.Add(0);
        System.Array.Fill(_bestTimes, float.MaxValue);
        SaveManager.Save();
    }

    public static List<int> GetUnlockedLevels() => new(_unlocked);
    public static float[] GetBestTimes() => (float[])_bestTimes.Clone();

    public static void ApplySaveData(SaveData data)
    {
        _unlocked.Clear();
        _unlocked.Add(0);
        foreach (var l in data.UnlockedLevels)
            if (l >= 0 && l < World.Map.LevelCount)
                _unlocked.Add(l);

        for (var i = 0; i < _bestTimes.Length && i < data.BestTimes.Length; i++)
            _bestTimes[i] = data.BestTimes[i];
    }
}
