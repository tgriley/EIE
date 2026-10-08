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

    public void Complete(int stage)
    {
        if (_progress.RecordEndlessStage(stage + 1)) _save();
    }
}
