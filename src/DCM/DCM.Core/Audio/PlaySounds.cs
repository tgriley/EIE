using Microsoft.Xna.Framework.Audio;
using System;

namespace DCM.Core.Audio;

public sealed record PlaySounds(
    SoundEffect Gunshot,
    SoundEffect PlayerOuch,
    SoundEffect PlayerDeath,
    SoundEffect EnemyOuch,
    SoundEffect EnemyDeath,
    SoundEffect Win) : IDisposable
{
    public void Dispose()
    {
        Gunshot.Dispose();
        PlayerOuch.Dispose();
        PlayerDeath.Dispose();
        EnemyOuch.Dispose();
        EnemyDeath.Dispose();
        Win.Dispose();
    }
}
