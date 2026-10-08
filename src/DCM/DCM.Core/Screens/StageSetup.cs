using DCM.Core.Entities;
using DCM.Core.World;

namespace DCM.Core.Screens;

// Everything that varies between stages of a run.
public sealed record StageSetup(int Stage, Map Map, LevelAssets Assets, int StartHealth, PlayerBuffs Buffs);
