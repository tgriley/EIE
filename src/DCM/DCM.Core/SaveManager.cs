#nullable enable
using System;
using System.IO;
using System.Text.Json;

namespace DCM.Core;

public static class SaveManager
{
    private static readonly string SavePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FindTheExit", "save.json");

    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    public static void Load()
    {
        try
        {
            if (!File.Exists(SavePath)) return;
            var data = JsonSerializer.Deserialize<SaveData>(File.ReadAllText(SavePath));
            if (data == null) return;
            LevelProgress.ApplySaveData(data);
            GameSettings.ApplySaveData(data);
        }
        catch { }
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath)!);
            var data = new SaveData
            {
                UnlockedLevels = LevelProgress.GetUnlockedLevels(),
                BestTimes      = LevelProgress.GetBestTimes(),
                MuteSound      = GameSettings.MuteSound,
                IsFullscreen   = GameSettings.IsFullscreen,
            };
            File.WriteAllText(SavePath, JsonSerializer.Serialize(data, WriteOptions));
        }
        catch { }
    }
}
