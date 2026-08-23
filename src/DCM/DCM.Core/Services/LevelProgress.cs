using System;
using System.Collections.Generic;
using DCM.Core.World;

namespace DCM.Core.Services;

public sealed class LevelProgress
{
    private readonly HashSet<int> _unlocked = new() { 0 };
    private readonly float[] _bestTimes = new float[Map.LevelCount];

    public LevelProgress() => Array.Fill(_bestTimes, float.MaxValue);

    public int BestEndlessStage { get; private set; }
    public bool IsUnlocked(int levelIndex) => _unlocked.Contains(levelIndex);
    public int MaxUnlockedLevel() => _unlocked.Count == 0 ? 0 : Math.Max(0, Max(_unlocked));
    public float GetBestTime(int levelIndex) => _bestTimes[levelIndex];
    public bool HasBestTime(int levelIndex) => GetBestTime(levelIndex) < float.MaxValue;
    public List<int> GetUnlockedLevels() => new(_unlocked);
    public float[] GetBestTimes() => (float[])_bestTimes.Clone();

    public bool RecordEndlessStage(int stagesCompleted)
    {
        if (stagesCompleted <= BestEndlessStage) return false;
        BestEndlessStage = stagesCompleted;
        return true;
    }

    public bool Unlock(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= Map.LevelCount) return false;
        return _unlocked.Add(levelIndex);
    }

    public bool RecordTime(int levelIndex, float seconds)
    {
        if (levelIndex < 0 || levelIndex >= _bestTimes.Length || seconds >= _bestTimes[levelIndex]) return false;
        _bestTimes[levelIndex] = seconds;
        return true;
    }

    public void Reset()
    {
        _unlocked.Clear();
        _unlocked.Add(0);
        Array.Fill(_bestTimes, float.MaxValue);
        BestEndlessStage = 0;
    }

    public void ApplySaveData(SaveData data)
    {
        Reset();
        foreach (var level in data.UnlockedLevels)
            if (level >= 0 && level < Map.LevelCount) _unlocked.Add(level);
        for (var i = 0; i < _bestTimes.Length && i < data.BestTimes.Length; i++)
            _bestTimes[i] = data.BestTimes[i];
        BestEndlessStage = Math.Max(0, data.BestEndlessStage);
    }

    public static string FormatTime(float seconds)
    {
        if (seconds >= float.MaxValue) return "--:--.--";
        var minutes = (int)seconds / 60;
        var wholeSeconds = (int)seconds % 60;
        var tenths = (int)(seconds % 1f * 10f);
        return $"{minutes}:{wholeSeconds:D2}.{tenths}";
    }

    private static int Max(IEnumerable<int> values)
    {
        var max = 0;
        foreach (var value in values) max = Math.Max(max, value);
        return max;
    }
}
