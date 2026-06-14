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
            _unlocked.Add(levelIndex);
    }

    public static float GetBestTime(int levelIndex) => _bestTimes[levelIndex];
    public static bool HasBestTime(int levelIndex) => _bestTimes[levelIndex] < float.MaxValue;

    public static void RecordTime(int levelIndex, float seconds)
    {
        if (levelIndex >= 0 && levelIndex < _bestTimes.Length && seconds < _bestTimes[levelIndex])
            _bestTimes[levelIndex] = seconds;
    }

    public static string FormatTime(float seconds)
    {
        if (seconds >= float.MaxValue) return "--:--.--";
        var m = (int)seconds / 60;
        var s = (int)seconds % 60;
        var t = (int)(seconds % 1f * 10f);
        return $"{m}:{s:D2}.{t}";
    }
}
