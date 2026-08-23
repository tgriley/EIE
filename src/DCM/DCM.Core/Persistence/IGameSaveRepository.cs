#nullable enable
using System;

namespace DCM.Core.Persistence;

public interface IGameSaveRepository
{
    SaveData? Load();
    bool Save(SaveData data);
}

public sealed class JsonGameSaveRepository : IGameSaveRepository
{
    private readonly string _path;
    private readonly Action<Exception>? _onError;

    public JsonGameSaveRepository(string path, Action<Exception>? onError = null)
    {
        _path = path;
        _onError = onError;
    }

    public SaveData? Load()
    {
        try
        {
            if (!System.IO.File.Exists(_path)) return null;
            return System.Text.Json.JsonSerializer.Deserialize<SaveData>(
                System.IO.File.ReadAllText(_path));
        }
        catch (Exception ex) when (ex is System.IO.IOException ||
                                   ex is UnauthorizedAccessException ||
                                   ex is System.Text.Json.JsonException)
        {
            _onError?.Invoke(ex);
            return null;
        }
    }

    public bool Save(SaveData data)
    {
        try
        {
            var directory = System.IO.Path.GetDirectoryName(_path);
            if (string.IsNullOrWhiteSpace(directory))
                throw new InvalidOperationException("The save path has no directory.");

            System.IO.Directory.CreateDirectory(directory);
            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            System.IO.File.WriteAllText(_path,
                System.Text.Json.JsonSerializer.Serialize(data, options));
            return true;
        }
        catch (Exception ex) when (ex is System.IO.IOException ||
                                   ex is UnauthorizedAccessException ||
                                   ex is InvalidOperationException ||
                                   ex is System.Text.Json.JsonException)
        {
            _onError?.Invoke(ex);
            return false;
        }
    }
}
