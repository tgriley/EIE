using DCM.Core.Persistence;
using Microsoft.Xna.Framework.Audio;

namespace DCM.Core.Services;

public sealed class GameSettings
{
    public bool MuteSound { get; private set; }
    public bool IsFullscreen { get; private set; }

    public void ToggleMute()
    {
        MuteSound = !MuteSound;
        ApplyAudioVolume();
    }

    public void ToggleFullscreen() => IsFullscreen = !IsFullscreen;

    public void ApplySaveData(SaveData data)
    {
        MuteSound = data.MuteSound;
        IsFullscreen = data.IsFullscreen;
        ApplyAudioVolume();
    }

    private void ApplyAudioVolume() => SoundEffect.MasterVolume = MuteSound ? 0f : 1f;
}
