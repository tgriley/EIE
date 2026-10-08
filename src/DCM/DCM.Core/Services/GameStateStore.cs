using DCM.Core.Persistence;
using System;

namespace DCM.Core.Services;

public sealed class GameStateStore
{
    private readonly IGameSaveRepository _repository;
    public GameSettings Settings { get; }
    public LevelProgress Progress { get; }

    public GameStateStore(IGameSaveRepository repository, GameSettings settings, LevelProgress progress)
    {
        _repository = repository;
        Settings = settings;
        Progress = progress;
    }

    public void Load()
    {
        var data = _repository.Load();
        if (data == null) return;
        Progress.ApplySaveData(data);
        Settings.ApplySaveData(data);
    }

    public bool Save() => _repository.Save(new SaveData
    {
        BestEndlessStage = Progress.BestEndlessStage,
        MuteSound = Settings.MuteSound,
        IsFullscreen = Settings.IsFullscreen
    });

    // Apply a change to settings or progress and persist it immediately.
    public void Update(Action<GameStateStore> change)
    {
        change(this);
        Save();
    }
}
