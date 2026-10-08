using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DCM.Core.Input;

// One frame of device state plus the previous frame, captured once by DCMGame
// so every screen sees the same edges. Mouse coordinates are already mapped
// into the logical canvas.
public readonly struct InputSnapshot
{
    public KeyboardState Keyboard { get; }
    public KeyboardState PrevKeyboard { get; }
    public MouseState Mouse { get; }
    public MouseState PrevMouse { get; }
    public GamePadState Pad { get; }
    public GamePadState PrevPad { get; }

    public InputSnapshot(KeyboardState keyboard, KeyboardState prevKeyboard,
        MouseState mouse, MouseState prevMouse, GamePadState pad, GamePadState prevPad)
    {
        Keyboard     = keyboard;
        PrevKeyboard = prevKeyboard;
        Mouse        = mouse;
        PrevMouse    = prevMouse;
        Pad          = pad;
        PrevPad      = prevPad;
    }

    public Point MousePosition => Mouse.Position;

    public bool KeyDown(Keys key) => Keyboard.IsKeyDown(key);

    public bool KeyJustPressed(Keys key) => Keyboard.IsKeyDown(key) && !PrevKeyboard.IsKeyDown(key);

    public bool PadJustPressed(Buttons button) => Pad.IsButtonDown(button) && !PrevPad.IsButtonDown(button);

    public bool LeftJustPressed =>
        Mouse.LeftButton == ButtonState.Pressed && PrevMouse.LeftButton != ButtonState.Pressed;

    public bool LeftReleased =>
        Mouse.LeftButton == ButtonState.Released && PrevMouse.LeftButton == ButtonState.Pressed;

    public bool LeftReleasedIn(Rectangle bounds) => LeftReleased && bounds.Contains(Mouse.Position);
}
