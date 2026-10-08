using Microsoft.Xna.Framework.Audio;
using System;

namespace DCM.Core.Audio;

public sealed record PlaySounds(
    SoundEffect PlayerOuch,
    SoundEffect PlayerDeath,
    SoundEffect Pickup,
    SoundEffect Win,
    SoundEffect CameraShutter) : IDisposable
{
    public void Dispose()
    {
        PlayerOuch.Dispose();
        PlayerDeath.Dispose();
        Pickup.Dispose();
        Win.Dispose();
        CameraShutter.Dispose();
    }
}
