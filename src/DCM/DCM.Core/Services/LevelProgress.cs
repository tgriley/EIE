using DCM.Core.Persistence;
using System;

namespace DCM.Core.Services;

public sealed class LevelProgress
{
    public int BestEndlessStage { get; private set; }

    public bool RecordEndlessStage(int stagesCompleted)
    {
        if (stagesCompleted <= BestEndlessStage) return false;
        BestEndlessStage = stagesCompleted;
        return true;
    }

    public void Reset() => BestEndlessStage = 0;

    public void ApplySaveData(SaveData data) => BestEndlessStage = Math.Max(0, data.BestEndlessStage);
}
