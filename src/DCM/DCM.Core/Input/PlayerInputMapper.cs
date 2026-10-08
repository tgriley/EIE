using DCM.Core.Configuration;
using Microsoft.Xna.Framework.Input;
using System;

namespace DCM.Core.Input;

// Key and pad bindings for gameplay: turns raw device state into actions.
public static class PlayerInputMapper
{
    public static PlayerInput Movement(InputSnapshot input, GameplayConfiguration config,
        int mouseDeltaX, bool cameraRaising)
    {
        var sticks = input.Pad.ThumbSticks;
        var leftX  = DeadZone(sticks.Left.X, config.StickDeadZone);
        var leftY  = DeadZone(sticks.Left.Y, config.StickDeadZone);
        var rightX = DeadZone(sticks.Right.X, config.StickDeadZone);

        return new PlayerInput(
            input.KeyDown(Keys.W) || input.KeyDown(Keys.Up)   || leftY > 0,
            input.KeyDown(Keys.S) || input.KeyDown(Keys.Down) || leftY < 0,
            input.KeyDown(Keys.A)                             || leftX < 0,
            input.KeyDown(Keys.D)                             || leftX > 0,
            input.KeyDown(Keys.Left),
            input.KeyDown(Keys.Right),
            input.KeyDown(Keys.LeftShift) || input.KeyDown(Keys.RightShift) ||
                input.Pad.Triggers.Left > config.TriggerThreshold,
            mouseDeltaX + (int)(rightX * config.ControllerLookSensitivity),
            cameraRaising);
    }

    public static bool CameraRaiseHeld(InputSnapshot input, GameplayConfiguration config) =>
        input.Mouse.RightButton == ButtonState.Pressed ||
        input.Pad.Triggers.Right > config.TriggerThreshold;

    public static bool CameraShootPressed(InputSnapshot input) =>
        input.LeftJustPressed || input.PadJustPressed(Buttons.RightShoulder);

    public static bool PausePressed(InputSnapshot input) =>
        input.KeyJustPressed(Keys.Escape) || input.PadJustPressed(Buttons.Start);

    private static float DeadZone(float value, float deadZone) => Math.Abs(value) > deadZone ? value : 0f;
}
