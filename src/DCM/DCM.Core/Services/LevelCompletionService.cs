namespace DCM.Core.Services;

public sealed class LevelCompletionService
{
    private readonly LevelProgress _progress;
    private readonly System.Action _save;

    public LevelCompletionService(LevelProgress progress, System.Action save)
    {
        _progress = progress;
        _save = save;
    }

    public bool Complete(int levelIndex, float elapsed, bool endless, bool hasNextLevel)
    {
        if (endless)
        {
            if (_progress.RecordEndlessStage(levelIndex + 1)) _save();
            return false;
        }

        var previousBest = _progress.GetBestTime(levelIndex);
        var recorded = _progress.RecordTime(levelIndex, elapsed);
        var unlocked = hasNextLevel && _progress.Unlock(levelIndex + 1);
        if (recorded || unlocked) _save();
        return elapsed < previousBest || previousBest >= float.MaxValue;
    }
}
