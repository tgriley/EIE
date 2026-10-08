namespace DCM.Core.Configuration;

public sealed record DisplayConfiguration(int LogicalWidth, int LogicalHeight, int WindowWidth, int WindowHeight)
{
    public static DisplayConfiguration Default { get; } = new(1280, 720, 1920, 1080);
}

public sealed record GameplayConfiguration(
    float StickDeadZone,
    float TriggerThreshold,
    int ControllerLookSensitivity,
    float CameraRaiseTime,
    float WeaponBobSpeed)
{
    public static GameplayConfiguration Default { get; } = new(0.20f, 0.50f, 50, 0.4f, 7f);
}

public sealed record GameConfiguration(
    DisplayConfiguration Display,
    GameplayConfiguration Gameplay)
{
    public static GameConfiguration Default { get; } = new(
        DisplayConfiguration.Default,
        GameplayConfiguration.Default);
}
