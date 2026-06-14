using Microsoft.Xna.Framework.Audio;

namespace DCM.Core;

public static class GameSettings
{
    public static bool MuteSound { get; private set; } = false;
    public static bool IsFullscreen { get; private set; } = false;

    public static void ToggleMute()
    {
        MuteSound = !MuteSound;
        SoundEffect.MasterVolume = MuteSound ? 0f : 1f;
        SaveManager.Save();
    }

    public static void ToggleFullscreen()
    {
        IsFullscreen = !IsFullscreen;
        SaveManager.Save();
    }

    public static void ApplySaveData(SaveData data)
    {
        MuteSound    = data.MuteSound;
        IsFullscreen = data.IsFullscreen;
        SoundEffect.MasterVolume = MuteSound ? 0f : 1f;
    }
}
