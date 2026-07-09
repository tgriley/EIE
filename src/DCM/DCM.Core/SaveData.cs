using System.Collections.Generic;

namespace DCM.Core;

public class SaveData
{
    public List<int> UnlockedLevels { get; set; } = new() { 0 };
    public float[] BestTimes { get; set; } = System.Array.Empty<float>();
    public int BestEndlessStage { get; set; }
    public bool MuteSound { get; set; }
    public bool IsFullscreen { get; set; }
}
