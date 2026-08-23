using DCM.Core.Persistence;

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
        UnlockedLevels = Progress.GetUnlockedLevels(),
        BestTimes = Progress.GetBestTimes(),
        BestEndlessStage = Progress.BestEndlessStage,
        MuteSound = Settings.MuteSound,
        IsFullscreen = Settings.IsFullscreen
    });
}
